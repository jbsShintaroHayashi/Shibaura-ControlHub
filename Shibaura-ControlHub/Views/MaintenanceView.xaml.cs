using System;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace Shibaura_ControlHub.Views
{
    /// <summary>
    /// MaintenanceView.xaml の相互作用ロジック
    /// </summary>
    public partial class MaintenanceView : System.Windows.Controls.UserControl, IDisposable
    {
        private bool _isDisposed = false;
        private bool _isInitialized = false;

        public MaintenanceView()
        {
            InitializeComponent();
            // コンストラクタでは初期化しない（MainWindowから呼び出す）
        }

        /// <summary>
        /// WebView2を初期化（アプリ起動時に1回だけ呼び出す）
        /// </summary>
        public async System.Threading.Tasks.Task InitializeWebView2Async()
        {
            // 多重初期化を防止
            if (_isInitialized || _isDisposed)
            {
                return;
            }

            try
            {
                await WebView2Control.EnsureCoreWebView2Async();
                if (WebView2Control.CoreWebView2 != null && !_isDisposed)
                {
                    WebView2Control.CoreWebView2.Settings.AreDevToolsEnabled = false;
                    WebView2Control.CoreWebView2.Settings.IsWebMessageEnabled = true;
                    
                    // Basic認証の自動入力
                    WebView2Control.CoreWebView2.BasicAuthenticationRequested += OnBasicAuthenticationRequested;
                    
                    // ナビゲーション完了時に自動入力処理を実行（フォーム用）
                    WebView2Control.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
                    
                    // WebMessageReceivedイベント（将来の拡張用）
                    // WebView2Control.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
                    
                    _isInitialized = true;
                }
            }
            catch (Exception ex)
            {
                if (!_isDisposed)
                {
                    System.Windows.MessageBox.Show($"WebView2の初期化に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnBasicAuthenticationRequested(object? sender, CoreWebView2BasicAuthenticationRequestedEventArgs e)
        {
            var deferral = e.GetDeferral();
            try
            {
                if (DataContext is ViewModels.MaintenanceViewModel viewModel)
                {
                    // 認証情報を自動的に提供
                    string username = viewModel.Username ?? "admin";
                    string password = viewModel.Password ?? "Qweasd123";
                    
                    // Responseオブジェクトに認証情報を設定
                    e.Response.UserName = username;
                    e.Response.Password = password;
                }
            }
            finally
            {
                deferral.Complete();
            }
        }

        private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess && WebView2Control.CoreWebView2 != null && DataContext is ViewModels.MaintenanceViewModel viewModel)
            {
                // ページ読み込み完了後、少し待ってからJavaScriptを実行
                await System.Threading.Tasks.Task.Delay(500);
                
                try
                {
                    // ユーザー名とパスワードを自動入力するJavaScript
                    string username = (viewModel.Username ?? "admin").Replace("'", "\\'").Replace("\\", "\\\\");
                    string password = (viewModel.Password ?? "Qweasd123").Replace("'", "\\'").Replace("\\", "\\\\");
                    
                    // 一般的なフォームフィールド名を試す
                    string script = $@"
                        (function() {{
                            // ユーザー名フィールドを探して値を設定
                            var usernameFields = ['username', 'user', 'login', 'loginname', 'account', 'id', 'name'];
                            var passwordFields = ['password', 'pass', 'pwd', 'passwd'];
                            
                            for (var i = 0; i < usernameFields.length; i++) {{
                                var field = document.getElementById(usernameFields[i]) || 
                                           document.getElementsByName(usernameFields[i])[0] ||
                                           document.querySelector('input[type=""text""][name*=""user""]') ||
                                           document.querySelector('input[type=""text""][name*=""login""]');
                                if (field) {{
                                    field.value = '{username}';
                                    field.dispatchEvent(new Event('input', {{ bubbles: true }}));
                                    field.dispatchEvent(new Event('change', {{ bubbles: true }}));
                                    break;
                                }}
                            }}
                            
                            // パスワードフィールドを探して値を設定
                            for (var i = 0; i < passwordFields.length; i++) {{
                                var field = document.getElementById(passwordFields[i]) || 
                                           document.getElementsByName(passwordFields[i])[0] ||
                                           document.querySelector('input[type=""password""]');
                                if (field) {{
                                    field.value = '{password}';
                                    field.dispatchEvent(new Event('input', {{ bubbles: true }}));
                                    field.dispatchEvent(new Event('change', {{ bubbles: true }}));
                                    break;
                                }}
                            }}
                        }})();
                    ";
                    
                    await WebView2Control.CoreWebView2.ExecuteScriptAsync(script);
                }
                catch (Exception ex)
                {
                    // JavaScript実行エラーは無視（フォームフィールドが見つからない場合など）
                    System.Diagnostics.Debug.WriteLine($"自動入力スクリプト実行エラー: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// WebView2をDisposeしてプロセスを終了させる
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            try
            {
                // イベントハンドラーを解除（メモリリーク防止）
                if (WebView2Control?.CoreWebView2 != null)
                {
                    WebView2Control.CoreWebView2.BasicAuthenticationRequested -= OnBasicAuthenticationRequested;
                    WebView2Control.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
                    
                    // WebMessageReceivedイベントの解除（将来の拡張用）
                    // WebView2Control.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
                }

                // WebView2をDispose
                WebView2Control?.Dispose();
            }
            catch
            {
                // Disposeエラーは無視
            }
            finally
            {
                _isDisposed = true;
            }
        }
    }
}
