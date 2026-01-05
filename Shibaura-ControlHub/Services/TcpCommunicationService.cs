using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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

