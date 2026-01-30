using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Threading;
using Shibaura_ControlHub.Utils;
using Shibaura_Lib.Udp;

namespace Shibaura_ControlHub.Services
{
    /// <summary>
    /// UDPコマンド通信を処理するサービス
    /// </summary>
    public class CommandCommunicationService
    {
        private readonly Dispatcher _dispatcher;
        private readonly Action<string> _modeStartRequested;

        public CommandCommunicationService(Dispatcher dispatcher, Action<string> modeStartRequested)
        {
            _dispatcher = dispatcher;
            _modeStartRequested = modeStartRequested;
        }

        /// <summary>
        /// モード切替コマンドをUDPで送信する（コマンド受信で使うのと同じ形式を指定先に送る）。
        /// メッセージ形式: Command(2byte), EquipmentId(2byte), Mode(2byte) の順・リトルエンディアン。
        /// </summary>
        /// <param name="host">送信先IPアドレス（例: 192.168.0.21）</param>
        /// <param name="port">送信先ポート（例: 9000）</param>
        /// <param name="modeNumber">モード番号（1=授業, 2=遠隔, 3=eスポーツ）</param>
        public static async Task SendModeCommandToAsync(string host, int port, int modeNumber)
        {
            if (string.IsNullOrEmpty(host) || port <= 0)
            {
                return;
            }

            ushort modeId = modeNumber switch
            {
                1 => (ushort)UdpMessageCatalog.ModeId.Class,
                2 => (ushort)UdpMessageCatalog.ModeId.Remote,
                3 => (ushort)UdpMessageCatalog.ModeId.Esports,
                _ => 0
            };
            if (modeId == 0)
            {
                return;
            }

            ushort command = (ushort)UdpMessageCatalog.CommandId.Start;
            ushort equipmentId = (ushort)UdpMessageCatalog.EquipmentId.All;

            byte[] payload = new byte[6];
            var cmdBytes = BitConverter.GetBytes(command);
            var eqBytes = BitConverter.GetBytes(equipmentId);
            var modeBytes = BitConverter.GetBytes(modeId);
            Buffer.BlockCopy(cmdBytes, 0, payload, 0, 2);
            Buffer.BlockCopy(eqBytes, 0, payload, 2, 2);
            Buffer.BlockCopy(modeBytes, 0, payload, 4, 2);

            try
            {
                using var udp = new UdpClient();
                var endPoint = new IPEndPoint(IPAddress.Parse(host), port);
                await udp.SendAsync(payload, payload.Length, endPoint).ConfigureAwait(false);
                ActionLogger.LogInfo("モード切替UDP送信", $"{host}:{port} へモード {modeNumber} を送信しました");
            }
            catch (Exception ex)
            {
                ActionLogger.LogError("モード切替UDP送信", $"{host}:{port} への送信に失敗: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 受信したメッセージを処理
        /// </summary>
        public void HandleMessage(UdpMessageReceivedEventArgs e)
        {
            _dispatcher.Invoke(() =>
            {
                var message = e.Message;
                var remoteAddress = e.RemoteAddress;
                var remotePort = e.RemotePort;

                if (message.Command == (ushort)UdpMessageCatalog.CommandId.Start)
                {
                    if (message.EquipmentId == (ushort)UdpMessageCatalog.EquipmentId.All)
                    {
                        string modeName = string.Empty;

                        if (message.Mode == (ushort)UdpMessageCatalog.ModeId.Class)
                        {
                            modeName = ModeSettingsManager.GetModeName(1);
                        }
                        else if (message.Mode == (ushort)UdpMessageCatalog.ModeId.Remote)
                        {
                            modeName = ModeSettingsManager.GetModeName(2);
                        }
                        else if (message.Mode == (ushort)UdpMessageCatalog.ModeId.Esports)
                        {
                            modeName = ModeSettingsManager.GetModeName(3);
                        }

                        if (!string.IsNullOrEmpty(modeName))
                        {
                            _modeStartRequested(modeName);
                        }
                    }
                }
            });
        }
    }
}

