using System;
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

