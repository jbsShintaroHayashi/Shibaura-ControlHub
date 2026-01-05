using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Shibaura_ControlHub.Utils;

namespace Shibaura_ControlHub.Services
{
    /// <summary>
    /// SONY PTZカメラ SRG-X120 のHTTP APIクライアント
    /// </summary>
    public sealed class SonyPtzCameraClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _username;
        private readonly string _password;
        private bool _disposed;

        public SonyPtzCameraClient(string username = "admin", string password = "Qweasd123")
        {
            _username = username;
            _password = password;
            
            // HttpClientHandlerに認証情報を設定（自動的にBasic/Digest認証を処理）
            var handler = new HttpClientHandler
            {
                Credentials = new NetworkCredential(username, password),
                PreAuthenticate = true
            };

            // タイムアウトは3秒に設定
            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(3)
            };
        }

        /// <summary>
        /// 共通ヘッダーを取得
        /// </summary>
        private void SetCommonHeaders(HttpRequestMessage request, string ipAddress)
        {
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Headers.Add("Accept", "*/*");
            request.Headers.Add("Referer", $"http://{ipAddress}/index.html?lang=ja");
        }

        /// <summary>
        /// プリセット呼び出し
        /// </summary>
        /// <param name="ipAddress">カメラのIPアドレス</param>
        /// <param name="presetNumber">プリセット番号（1-16）</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        public async Task RecallPresetAsync(string ipAddress, int presetNumber, CancellationToken cancellationToken = default)
        {
            if (presetNumber < 1) presetNumber = 1;
            if (presetNumber > 16) presetNumber = 16;

            var uri = $"http://{ipAddress}/command/presetposition.cgi?PresetCall={presetNumber}";
            await SendGetRequestAsync(uri, ipAddress, cancellationToken).ConfigureAwait(false);
            
            ActionLogger.LogAction("プリセット呼出", $"カメラ {ipAddress} のプリセット{presetNumber}を呼び出しました");
        }

        /// <summary>
        /// プリセット登録（現在位置を記録）
        /// </summary>
        /// <param name="ipAddress">カメラのIPアドレス</param>
        /// <param name="presetNumber">プリセット番号（1-256）</param>
        /// <param name="presetName">プリセット名（32文字以内）</param>
        /// <param name="thumbnail">サムネイル使用 on/off</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        public async Task StorePresetAsync(string ipAddress, int presetNumber, string? presetName = null, string thumbnail = "off", CancellationToken cancellationToken = default)
        {
            if (presetNumber < 1) presetNumber = 1;
            if (presetNumber > 256) presetNumber = 256;

            var name = string.IsNullOrWhiteSpace(presetName) ? $"Preset{presetNumber}" : presetName.Trim();
            if (name.Length > 32) name = name.Substring(0, 32);

            // PresetSet=<番号>,<名前>,<on|off>
            var uri = $"http://{ipAddress}/command/presetposition.cgi?PresetSet={presetNumber},{name},{thumbnail}";
            await SendGetRequestAsync(uri, ipAddress, cancellationToken).ConfigureAwait(false);
            
            ActionLogger.LogAction("プリセット登録", $"カメラ {ipAddress} のプリセット{presetNumber} ({name}, thumbnail:{thumbnail}) を登録しました");
        }

        /// <summary>
        /// プリセット削除
        /// </summary>
        /// <param name="ipAddress">カメラのIPアドレス</param>
        /// <param name="presetNumber">プリセット番号（1-16）</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        public async Task ClearPresetAsync(string ipAddress, int presetNumber, CancellationToken cancellationToken = default)
        {
            if (presetNumber < 1) presetNumber = 1;
            if (presetNumber > 16) presetNumber = 16;

            var uri = $"http://{ipAddress}/command/presetposition.cgi?PresetClear={presetNumber}";
            await SendGetRequestAsync(uri, ipAddress, cancellationToken).ConfigureAwait(false);
            
            ActionLogger.LogAction("プリセット削除", $"カメラ {ipAddress} のプリセット{presetNumber}を削除しました");
        }

        /// <summary>
        /// ホームポジション呼び出し
        /// </summary>
        /// <param name="ipAddress">カメラのIPアドレス</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        public async Task RecallHomePositionAsync(string ipAddress, CancellationToken cancellationToken = default)
        {
            // キャッシュバスターとしてタイムスタンプを追加
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var uri = $"http://{ipAddress}/command/presetposition.cgi?HomePos=recall&_={timestamp}";
            await SendGetRequestAsync(uri, ipAddress, cancellationToken).ConfigureAwait(false);
            
            ActionLogger.LogAction("ホームポジション呼出", $"カメラ {ipAddress} のホームポジションを呼び出しました");
        }

        /// <summary>
        /// パン・チルト移動
        /// </summary>
        /// <param name="ipAddress">カメラのIPアドレス</param>
        /// <param name="direction">方向（up, down, left, right, home）</param>
        /// <param name="speed">速度（0-15、0で停止、デフォルト: 1）</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        public async Task MoveAsync(string ipAddress, string direction, int speed = 1, CancellationToken cancellationToken = default)
        {
            speed = Math.Clamp(speed, 0, 15);
            var uri = $"http://{ipAddress}/command/ptzf.cgi?Move={direction},{speed}";
            await SendGetRequestAsync(uri, ipAddress, cancellationToken).ConfigureAwait(false);
            ActionLogger.LogAction("パン・チルト移動", $"カメラ {ipAddress} を {direction} 方向に移動（速度: {speed}）");
        }

        /// <summary>
        /// パン・チルト停止（すべての方向を停止）
        /// </summary>
        /// <param name="ipAddress">カメラのIPアドレス</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        public async Task MoveStopAsync(string ipAddress, CancellationToken cancellationToken = default)
        {
            var uri = $"http://{ipAddress}/command/ptzf.cgi?Move=stop,pantilt";
            await SendGetRequestAsync(uri, ipAddress, cancellationToken).ConfigureAwait(false);
            ActionLogger.LogAction("パン・チルト移動", $"カメラを停止");
        }

        /// <summary>
        /// パン・チルト停止（すべての方向を停止）
        /// </summary>
        /// <param name="ipAddress">カメラのIPアドレス</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        public async Task StopPanTiltAllAsync(string ipAddress, CancellationToken cancellationToken = default)
        {
            // すべての方向に対して速度0を送信して停止
            await MoveAsync(ipAddress, "up", speed: 0, cancellationToken).ConfigureAwait(false);
            await MoveAsync(ipAddress, "down", speed: 0, cancellationToken).ConfigureAwait(false);
            await MoveAsync(ipAddress, "left", speed: 0, cancellationToken).ConfigureAwait(false);
            await MoveAsync(ipAddress, "right", speed: 0, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// ズーム操作（Moveでtele/wideを指定）
        /// </summary>
        /// <param name="ipAddress">カメラのIPアドレス</param>
        /// <param name="direction">方向（tele=ズームイン, wide=ズームアウト）</param>
        /// <param name="speed">速度（0-7、0で停止、デフォルト: 1）</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        public async Task ZoomAsync(string ipAddress, string direction, int speed = 1, CancellationToken cancellationToken = default)
        {
            speed = Math.Clamp(speed, 0, 7);
            var uri = $"http://{ipAddress}/command/ptzf.cgi?Move={direction},{speed}";
            await SendGetRequestAsync(uri, ipAddress, cancellationToken).ConfigureAwait(false);
            
            if (speed == 0)
            {
                ActionLogger.LogAction("ズーム停止", $"カメラ {ipAddress} のズームを停止");
            }
            else
            {
                ActionLogger.LogAction("ズーム操作", $"カメラ {ipAddress} を {direction} 方向にズーム（速度: {speed}）");
            }
        }

        /// <summary>
        /// ズーム停止
        /// </summary>
        /// <param name="ipAddress">カメラのIPアドレス</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        public async Task ZoomStopAsync(string ipAddress, CancellationToken cancellationToken = default)
        {
            var uri = $"http://{ipAddress}/command/ptzf.cgi?Move=stop,zoom";
            await SendGetRequestAsync(uri, ipAddress, cancellationToken).ConfigureAwait(false);
            ActionLogger.LogAction("ズーム停止", $"カメラ {ipAddress} のズームを停止しました");
        }

        /// <summary>
        /// フォーカス操作
        /// </summary>
        /// <param name="ipAddress">カメラのIPアドレス</param>
        /// <param name="direction">方向（near, far, auto, manual）</param>
        /// <param name="speed">速度（0-7、デフォルト: 0）</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        public async Task FocusAsync(string ipAddress, string direction, int speed = 0, CancellationToken cancellationToken = default)
        {
            speed = Math.Clamp(speed, 0, 7);
            var uri = $"http://{ipAddress}/command/ptzf.cgi?Focus={direction},{speed}";
            await SendGetRequestAsync(uri, ipAddress, cancellationToken).ConfigureAwait(false);
            
            ActionLogger.LogAction("フォーカス操作", $"カメラ {ipAddress} のフォーカスを {direction} に設定（速度: {speed}）");
        }

        /// <summary>
        /// プリセット状態の問い合わせ
        /// </summary>
        /// <param name="ipAddress">カメラのIPアドレス</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <returns>レスポンス文字列</returns>
        public async Task<string> InquiryPresetAsync(string ipAddress, CancellationToken cancellationToken = default)
        {
            var uri = $"http://{ipAddress}/command/presetposition.cgi?Inquiry=Preset";
            return await SendGetRequestAsync(uri, ipAddress, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// PTZF状態取得
        /// </summary>
        /// <param name="ipAddress">カメラのIPアドレス</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <returns>レスポンス文字列</returns>
        public async Task<string> InquiryPositionAsync(string ipAddress, CancellationToken cancellationToken = default)
        {
            var uri = $"http://{ipAddress}/command/ptzf.cgi?Inquiry=Position";
            return await SendGetRequestAsync(uri, ipAddress, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// GETリクエストを送信（HttpClientHandlerが自動的にDigest認証を処理）
        /// </summary>
        private async Task<string> SendGetRequestAsync(string uri, string ipAddress, CancellationToken cancellationToken)
        {
            try
            {
                ActionLogger.LogProcessing("HTTPリクエスト送信", $"URI: {uri}, ユーザー名: {_username}");
                
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                SetCommonHeaders(request, ipAddress);

                var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                
                ActionLogger.LogProcessing("HTTPレスポンス受信", 
                    $"ステータス: {response.StatusCode} ({(int)response.StatusCode})\n" +
                    $"URI: {uri}");

                var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    ActionLogger.LogError("HTTPリクエスト失敗", 
                        $"URI: {uri}\n" +
                        $"ステータス: {response.StatusCode} ({(int)response.StatusCode})\n" +
                        $"レスポンス: {content}");
                    
                    response.EnsureSuccessStatusCode();
                }
                else
                {
                    ActionLogger.LogAction("HTTPリクエスト成功", 
                        $"URI: {uri}\n" +
                        $"ステータス: {response.StatusCode}");
                }
                
                return content;
            }
            catch (HttpRequestException ex)
            {
                ActionLogger.LogError("HTTPリクエストエラー", 
                    $"URI: {uri}\n" +
                    $"エラー: {ex.Message}");
                throw;
            }
            catch (TaskCanceledException ex)
            {
                ActionLogger.LogError("HTTPリクエストタイムアウト", 
                    $"URI: {uri}\n" +
                    $"エラー: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                ActionLogger.LogError("HTTPリクエスト例外", 
                    $"URI: {uri}\n" +
                    $"例外: {ex.GetType().Name}\n" +
                    $"メッセージ: {ex.Message}");
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}

