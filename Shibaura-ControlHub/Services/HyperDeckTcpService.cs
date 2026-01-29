using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shibaura_ControlHub.Services
{
    /// <summary>
    /// HyperDeck（Ethernet Protocol / TCP）へコマンド送信するサービス。
    /// 必要なときだけ接続し、送信後に切断する。
    /// </summary>
    public sealed class HyperDeckTcpService
    {
        private const int DefaultPort = 9993;

        public Task RecordAsync(string ipAddress, int port = DefaultPort, CancellationToken cancellationToken = default) =>
            SendCommandAsync(ipAddress, port, "record", cancellationToken);

        public Task StopAsync(string ipAddress, int port = DefaultPort, CancellationToken cancellationToken = default) =>
            SendCommandAsync(ipAddress, port, "stop", cancellationToken);

        private static async Task SendCommandAsync(string ipAddress, int port, string command, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                throw new ArgumentException("IPアドレスが空です。", nameof(ipAddress));
            }

            if (port <= 0)
            {
                port = DefaultPort;
            }

            // HyperDeckはCRLF終端
            var payload = Encoding.ASCII.GetBytes($"{command}\r\n");

            using var client = new TcpClient();
            await client.ConnectAsync(ipAddress, port, cancellationToken).ConfigureAwait(false);

            using var stream = client.GetStream();
            await stream.WriteAsync(payload, 0, payload.Length, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}

