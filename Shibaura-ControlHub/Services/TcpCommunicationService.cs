using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Shibaura_ControlHub.Models;
using Shibaura_ControlHub.Utils;

namespace Shibaura_ControlHub.Services
{
    /// <summary>
    /// TCP通信を担当するサービス
    /// </summary>
    public class TcpCommunicationService
    {
        /// <summary>
        /// TCP接続をテスト
        /// </summary>
        public async Task<bool> TestConnectionAsync(string ipAddress, int port)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(ipAddress, port);
                return client.Connected;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// TCPでバイトデータを送信し、送信後に切断する（レスポンスは待たない）。
        /// モード切替コマンド（SS 1/2/3 + CR）など、送信のみでよい場合に使用する。
        /// </summary>
        /// <param name="host">接続先ホスト（IPアドレスまたはホスト名）</param>
        /// <param name="port">ポート番号</param>
        /// <param name="data">送信するバイト列</param>
        public async Task SendBytesAndCloseAsync(string host, int port, byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return;
            }

            using var client = new TcpClient();
            await client.ConnectAsync(host, port);
            using (var stream = client.GetStream())
            {
                await stream.WriteAsync(data, 0, data.Length);
            }
        }

        /// <summary>
        /// TCP経由でデータを送信
        /// </summary>
        public async Task<string> SendCommandAsync(string ipAddress, int port, string command)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(ipAddress, port);

                using var stream = client.GetStream();
                var data = Encoding.UTF8.GetBytes(command);
                await stream.WriteAsync(data, 0, data.Length);

                // レスポンスを読み取る
                var buffer = new byte[1024];
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                return Encoding.UTF8.GetString(buffer, 0, bytesRead);
            }
            catch (Exception ex)
            {
                throw new Exception($"TCP通信エラー: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// モード切替コマンドをDSP（monitoring_equipment.json の「デジタルシグナルプロセッサ」）へTCP送信する（接続→送信→切断）。
        /// 送信データ: SS + スペース + モード番号 + CR (Hex: 53 53 20 31/32/33 0D)
        /// </summary>
        /// <param name="modeNumber">モード番号（1=授業, 2=遠隔, 3=eスポーツ）</param>
        /// <param name="monitoringList">モニタリング機器リスト（DSPの ipAddress/port を参照）</param>
        public async Task SendModeSwitchCommandToDspAsync(int modeNumber, IEnumerable<EquipmentStatus>? monitoringList)
        {
            const string dspName = "デジタルシグナルプロセッサ";
            var dsp = monitoringList?.FirstOrDefault(e =>
                string.Equals(e.Name, dspName, StringComparison.OrdinalIgnoreCase));
            if (dsp == null || string.IsNullOrWhiteSpace(dsp.IpAddress))
            {
                return;
            }

            var host = dsp.IpAddress;
            var port = dsp.Port > 0 ? dsp.Port : 10055;
            var commandBytes = new byte[] { 0x53, 0x53, 0x20, (byte)('0' + modeNumber), 0x0D };

            try
            {
                ActionLogger.LogProcessing("モード切替TCP送信", $"{dspName} ({host}:{port}) へ SS {modeNumber} (CR) を送信");
                await SendBytesAndCloseAsync(host, port, commandBytes);
                ActionLogger.LogResult("モード切替TCP送信完了", $"{host}:{port} へ送信しました");
            }
            catch (Exception ex)
            {
                ActionLogger.LogError("モード切替TCP送信", $"{host}:{port} への送信に失敗しました: {ex.Message}");
            }
        }

        /// <summary>
        /// TCP経由でデータを受信
        /// </summary>
        public async Task<string> ReceiveDataAsync(string ipAddress, int port)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(ipAddress, port);

                using var stream = client.GetStream();
                var buffer = new byte[1024];
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                return Encoding.UTF8.GetString(buffer, 0, bytesRead);
            }
            catch (Exception ex)
            {
                throw new Exception($"TCP受信エラー: {ex.Message}", ex);
            }
        }
    }
}

