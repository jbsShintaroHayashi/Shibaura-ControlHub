using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shibaura_ControlHub.Models;
using Shibaura_ControlHub.Utils;

namespace Shibaura_ControlHub.Services
{
    /// <summary>
    /// デジタルミキサ（Bose ControlSpace DSP）制御クラス。
    /// DigitalMixerManager.cs と同様のコマンド形式で TCP 送信する（送信都度接続・切断）。
    /// Gain モジュールのボリューム・ミュートに対応。
    /// </summary>
    public class DigitalMixerManager
    {
        /// <summary>
        /// 案1：0dB（ユニティゲイン）基準方式。
        /// フェーダー 70（または75）＝ 0dB、それより上＝＋dB、下＝－dB。0％＝ミュート（-60dBで表現）。
        /// ブレークポイント: (0, -60), (50, -20), (70, 0), (85, +6), (100, +10)
        /// </summary>
        private const double UnityFaderPercent = 70.0;
        private const double DbAtZeroPercent = -60.0;
        private static readonly (double Percent, double Db)[] UnityGainCurve =
        {
            (0, -60.0),
            (50, -20.0),
            (70, 0.0),
            (85, 6.0),
            (100, 10.0)
        };

        private const string SetCmdFormatGainMute = "SA \"{0}\">2={1}\r";
        private const string SetCmdFormatGainVolume = "SA \"{0}\">1={1:0.0}\r";
        private const string GetCmdFormatGainVolume = "GA \"{0}\">1\r";
        private const string GetCmdFormatGainMute = "GA \"{0}\">2\r";

        /*
         * Bose ControlSpace GAIN モジュール コマンド例（CR = \r = 0x0D）
         * Mute: O=On（ミュート）, F=Off（ミュート解除）, T=Toggle
         *
         * 【設定(SA)】
         *   M1 を 0 dB:       SA "M1">1=0.0\r   → 53 41 20 22 4D 31 22 3E 31 3D 30 2E 30 0D
         *   M1 を -6 dB:      SA "M1">1=-6.0\r  → 53 41 20 22 4D 31 22 3E 31 3D 2D 36 2E 30 0D
         *   M1 ミュート:      SA "M1">2=O\r     → 53 41 20 22 4D 31 22 3E 32 3D 4F 0D
         *   M1 ミュート解除:  SA "M1">2=F\r     → 53 41 20 22 4D 31 22 3E 32 3D 46 0D
         *
         * 【取得(GA)】
         *   M1 ゲイン取得:    GA "M1">1\r       → 47 41 20 22 4D 31 22 3E 31 0D
         *   M1 ミュート取得:  GA "M1">2\r       → 47 41 20 22 4D 31 22 3E 32 0D
         *
         * 【複数取得（セミコロン 0x3B 区切り、末尾 CR 0x0D）】
         *   GA "M1">1;GA "M1">2;...;GA "S2">2\r
         *   → 47 41 20 22 4D 31 22 3E 31 3B 47 41 20 22 4D 31 22 3E 32 3B ... 0D
         */

        private readonly string _host;
        private readonly int _port;
        private readonly TcpCommunicationService _tcpService = new TcpCommunicationService();

        /// <summary>
        /// チャンネル番号（1～6）に対応する Gain モジュール名。M1～M4=マイク, S1/S2=出力。
        /// </summary>
        private static readonly string[] DefaultGainModuleNames = { "M1", "M2", "M3", "M4", "S1", "S2" };

        public DigitalMixerManager(string host, int port)
        {
            _host = host ?? string.Empty;
            _port = port > 0 ? port : 10055;
        }

        /// <summary>
        /// モニタリング機器リストから「デジタルシグナルプロセッサ」を取得して DigitalMixerManager を生成する。見つからない場合は null。
        /// </summary>
        public static DigitalMixerManager? CreateFromMonitoringList(IEnumerable<EquipmentStatus>? monitoringList)
        {
            const string dspName = "デジタルシグナルプロセッサ";
            var dsp = monitoringList?.FirstOrDefault(e =>
                string.Equals(e.Name, dspName, StringComparison.OrdinalIgnoreCase));
            if (dsp == null || string.IsNullOrWhiteSpace(dsp.IpAddress))
            {
                return null;
            }
            var port = dsp.Port > 0 ? dsp.Port : 10055;
            return new DigitalMixerManager(dsp.IpAddress, port);
        }

        /// <summary>
        /// % から dB に変換（0dB＝ユニティ基準、フェーダー70＝0dB）
        /// 0→-60dB, 50→-20dB, 70→0dB, 85→+6dB, 100→+10dB の線形補間。
        /// </summary>
        public static double PercentToDecibelForGain(double percent)
        {
            try
            {
                percent = Math.Clamp(percent, 0, 100);
                for (int i = 0; i < UnityGainCurve.Length - 1; i++)
                {
                    var (p0, d0) = UnityGainCurve[i];
                    var (p1, d1) = UnityGainCurve[i + 1];
                    if (percent >= p0 && percent <= p1)
                    {
                        if (Math.Abs(p1 - p0) < 1e-9) return d0;
                        return d0 + (d1 - d0) * (percent - p0) / (p1 - p0);
                    }
                }
                return UnityGainCurve[UnityGainCurve.Length - 1].Db;
            }
            catch
            {
                return 0.0;
            }
        }

        /// <summary>
        /// dB から % に逆変換（PercentToDecibelForGain の逆）。0dB＝ユニティ基準→70%。
        /// 状態取得(GA)で得た dB を UI フェーダー用 % に変換して表示する際に使用。
        /// </summary>
        public static double DecibelToPercentForGain(double dbDecibel)
        {
            try
            {
                for (int i = 0; i < UnityGainCurve.Length - 1; i++)
                {
                    var (p0, d0) = UnityGainCurve[i];
                    var (p1, d1) = UnityGainCurve[i + 1];
                    if (dbDecibel >= Math.Min(d0, d1) && dbDecibel <= Math.Max(d0, d1))
                    {
                        if (Math.Abs(d1 - d0) < 1e-9) return p0;
                        return p0 + (p1 - p0) * (dbDecibel - d0) / (d1 - d0);
                    }
                }
                if (dbDecibel <= UnityGainCurve[0].Db) return 0;
                return 100;
            }
            catch
            {
                return UnityFaderPercent;
            }
        }

        /// <summary>
        /// チャンネル番号（1～6）に対応する Gain モジュール名を取得
        /// </summary>
        public static string GetGainModuleName(int channel1Based)
        {
            var index = Math.Clamp(channel1Based, 1, DefaultGainModuleNames.Length) - 1;
            return DefaultGainModuleNames[index];
        }

        /// <summary>
        /// Gain モジュール ボリューム設定（dB 値で送信）
        /// </summary>
        public async Task<bool> SetGainVolumeAsync(string moduleName, double dbVol)
        {
            var cmd = string.Format(SetCmdFormatGainVolume, moduleName, dbVol);
            return await SendCmdAsync(cmd).ConfigureAwait(false);
        }

        /// <summary>
        /// Gain モジュール ミュート設定
        /// </summary>
        /// <param name="moduleName">モジュール名</param>
        /// <param name="isMute">true=ミュート, false=ミュート解除</param>
        public async Task<bool> SetGainMuteAsync(string moduleName, bool isMute)
        {
            // Bose: O=On（ミュート）, F=Off（ミュート解除）
            var value = isMute ? "O" : "F";
            var cmd = string.Format(SetCmdFormatGainMute, moduleName, value);
            return await SendCmdAsync(cmd).ConfigureAwait(false);
        }

        /// <summary>
        /// チャンネル番号（1～6）でボリュームを設定。UI の 0～100% を dB に変換して送信。
        /// </summary>
        public async Task<bool> SetGainVolumeByChannelAsync(int channel1Based, double percent0To100)
        {
            var moduleName = GetGainModuleName(channel1Based);
            var db = PercentToDecibelForGain(percent0To100);
            return await SetGainVolumeAsync(moduleName, db).ConfigureAwait(false);
        }

        /// <summary>
        /// チャンネル番号（1～6）でミュートを設定
        /// </summary>
        public async Task<bool> SetGainMuteByChannelAsync(int channel1Based, bool isMute)
        {
            var moduleName = GetGainModuleName(channel1Based);
            return await SetGainMuteAsync(moduleName, isMute).ConfigureAwait(false);
        }

        /// <summary>
        /// DSP から全チャンネル（M1～M4, S1, S2）のゲイン・ミュート状態を取得
        /// 複数コマンド一括送信は不可のため、GA を1本ずつ送信して応答を集め、解析する。
        /// </summary>
        public async Task<GainStateResult?> GetAllGainStateAsync()
        {
            if (string.IsNullOrEmpty(_host))
            {
                return null;
            }

            var responseBuilder = new StringBuilder();
            try
            {
                foreach (var name in DefaultGainModuleNames)
                {
                    var cmdVolume = string.Format(GetCmdFormatGainVolume, name);
                    var cmdMute = string.Format(GetCmdFormatGainMute, name);
                    var dataVolume = Encoding.UTF8.GetBytes(cmdVolume);
                    var dataMute = Encoding.UTF8.GetBytes(cmdMute);

                    ActionLogger.LogInfo("DSP状態取得 送信", $"{_host}:{_port} {cmdVolume.TrimEnd()}");
                    var r1 = await _tcpService.SendAndReceiveAsync(_host, _port, dataVolume, 2000).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(r1))
                    {
                        ActionLogger.LogInfo("DSP状態取得 受信", $"{_host}:{_port} {r1.TrimEnd()}");
                        responseBuilder.Append(r1.TrimEnd()).Append(';');
                    }

                    ActionLogger.LogInfo("DSP状態取得 送信", $"{_host}:{_port} {cmdMute.TrimEnd()}");
                    var r2 = await _tcpService.SendAndReceiveAsync(_host, _port, dataMute, 2000).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(r2))
                    {
                        ActionLogger.LogInfo("DSP状態取得 受信", $"{_host}:{_port} {r2.TrimEnd()}");
                        responseBuilder.Append(r2.TrimEnd()).Append(';');
                    }
                }

                var combined = responseBuilder.ToString();
                return string.IsNullOrEmpty(combined) ? null : ParseGainStateResponse(combined);
            }
            catch (Exception ex)
            {
                ActionLogger.LogError("DSP状態取得", $"{_host}:{_port} 送受信失敗: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// GA 問い合わせの応答を解析
        /// </summary>
        private static GainStateResult? ParseGainStateResponse(string response)
        {
            if (string.IsNullOrEmpty(response)) return null;

            var volumes = new double[6];
            var mutes = new bool[6];
            for (int i = 0; i < 6; i++)
            {
                volumes[i] = 0.0;
                mutes[i] = false;
            }

            var normalized = response.Replace("\r", " ").Replace("\n", " ");
            var segments = normalized.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var segment in segments)
            {
                var trimmed = segment.Trim();
                if (trimmed.Length < 5 || !trimmed.Contains("\"") || !trimmed.Contains(">")) continue;

                int quoteStart = trimmed.IndexOf('"');
                int quoteEnd = trimmed.IndexOf('"', quoteStart + 1);
                if (quoteStart < 0 || quoteEnd <= quoteStart) continue;

                string moduleName = trimmed.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                int channelIndex = Array.IndexOf(DefaultGainModuleNames, moduleName);
                if (channelIndex < 0) continue;

                string rest = trimmed.Substring(quoteEnd + 1).Trim();
                if (rest.StartsWith(">1="))
                {
                    string valuePart = rest.Substring(3).Trim();
                    // DSP が "2,5" のようにカンマ小数点で返す場合に対応（ドットに置換してパース）
                    string normalizedValue = valuePart.Replace(',', '.');
                    if (double.TryParse(normalizedValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double db))
                    {
                        volumes[channelIndex] = db;
                    }
                }
                else if (rest.StartsWith(">2="))
                {
                    string valuePart = rest.Substring(3).Trim();
                    // Bose: O=On（ミュート）, F=Off（ミュート解除）
                    mutes[channelIndex] = string.Equals(valuePart, "O", StringComparison.OrdinalIgnoreCase);
                }
            }

            return new GainStateResult(volumes, mutes);
        }

        /// <summary>
        /// DSP から取得したゲイン状態（6チャンネル分）
        /// </summary>
        public sealed class GainStateResult
        {
            public double[] VolumeDb { get; }
            public bool[] IsMuted { get; }

            public GainStateResult(double[] volumeDb, bool[] isMuted)
            {
                VolumeDb = volumeDb ?? new double[6];
                IsMuted = isMuted ?? new bool[6];
            }
        }

        /// <summary>
        /// DSP へコマンド送信。成功時はログに出さず、失敗時のみエラーに残す。
        /// </summary>
        private async Task<bool> SendCmdAsync(string strCmd)
        {
            if (string.IsNullOrEmpty(_host))
            {
                return false;
            }
            try
            {
                var data = Encoding.UTF8.GetBytes(strCmd);
                await _tcpService.SendBytesAndCloseAsync(_host, _port, data).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                ActionLogger.LogError("DSPコマンド送信", $"{_host}:{_port} 送信失敗: {ex.Message}");
                return false;
            }
        }
    }
}
