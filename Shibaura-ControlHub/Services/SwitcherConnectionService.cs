using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using Shibaura_ControlHub.Models;
using Shibaura_ControlHub.Services;
using Shibaura_ControlHub.Utils;

namespace Shibaura_ControlHub.Services
{
    /// <summary>
    /// スイッチャーの接続処理を担当するサービス
    /// </summary>
    public class SwitcherConnectionService
    {
        /// <summary>
        /// スイッチャーに接続する（非同期）
        /// </summary>
        /// <param name="switcherList">スイッチャーリスト</param>
        /// <param name="preferredSwitcherName">優先するスイッチャー名（nullの場合は最初のスイッチャーを使用）</param>
        /// <param name="onConnected">接続成功時のコールバック（ATEMクライアントを引数に取る）</param>
        /// <param name="dispatcher">UIスレッド用のDispatcher</param>
        /// <param name="timeoutSeconds">接続タイムアウト（秒、デフォルト: 30秒）</param>
        public void ConnectToSwitcherAsync(
            ObservableCollection<EquipmentStatus> switcherList,
            string? preferredSwitcherName,
            Action<IAtemSwitcherClient> onConnected,
            Dispatcher dispatcher,
            int timeoutSeconds = 30)
        {
            ActionLogger.LogProcessingStart("スイッチャー接続", "スイッチャーへの接続を開始します");

            try
            {
                // スイッチャーリストの確認
                if (switcherList == null || switcherList.Count == 0)
                {
                    ActionLogger.LogError("スイッチャー接続", "スイッチャーリストが空です");
                    return;
                }

                ActionLogger.LogProcessing("スイッチャーリスト確認", $"スイッチャー数: {switcherList.Count}");

                // 使用するスイッチャーを決定
                EquipmentStatus? targetSwitcher = null;

                // 優先スイッチャー名が指定されている場合は、そのスイッチャーを探す
                if (!string.IsNullOrWhiteSpace(preferredSwitcherName))
                {
                    targetSwitcher = switcherList.FirstOrDefault(s => 
                        s.Name.Contains(preferredSwitcherName, StringComparison.OrdinalIgnoreCase));
                    
                    if (targetSwitcher != null)
                    {
                        ActionLogger.LogProcessing("優先スイッチャー発見", 
                            $"名前: {targetSwitcher.Name}, IP: {targetSwitcher.IpAddress ?? "未設定"}, Port: {targetSwitcher.Port}");
                    }
                }

                // 優先スイッチャーが見つからない場合は、最初のスイッチャーを使用
                if (targetSwitcher == null)
                {
                    targetSwitcher = switcherList.FirstOrDefault();
                    
                    if (targetSwitcher != null)
                    {
                        ActionLogger.LogProcessing("デフォルトスイッチャー使用", 
                            $"名前: {targetSwitcher.Name}, IP: {targetSwitcher.IpAddress ?? "未設定"}, Port: {targetSwitcher.Port}");
                    }
                }

                if (targetSwitcher == null)
                {
                    ActionLogger.LogError("スイッチャー接続", "使用可能なスイッチャーが見つかりません");
                    return;
                }

                // IPアドレスの確認
                if (string.IsNullOrWhiteSpace(targetSwitcher.IpAddress))
                {
                    ActionLogger.LogError("スイッチャー接続", $"スイッチャー '{targetSwitcher.Name}' のIPアドレスが設定されていません");
                    return;
                }

                var ipAddress = targetSwitcher.IpAddress;
                var port = targetSwitcher.Port > 0 ? targetSwitcher.Port : 9910; // ATEM標準ポート

                ActionLogger.LogProcessing("ATEM接続開始", $"IP: {ipAddress}, Port: {port}");

                // ATEMクライアントの作成と接続（非同期）
                var client = new AtemSwitcherClient();
                
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // 接続タイムアウトを設定
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

                        // ATEMスイッチャーに接続
                        await client.ConnectAsync(ipAddress, cts.Token).ConfigureAwait(false);

                        ActionLogger.LogResult("ATEM接続成功", 
                            $"ATEMスイッチャーに接続しました: {ipAddress}:{port} (スイッチャー名: {targetSwitcher.Name})");
                        ActionLogger.LogProcessingComplete("スイッチャー接続");

                        // UIスレッドでコールバックを実行
                        dispatcher.Invoke(() =>
                        {
                            try
                            {
                                onConnected(client);
                                ActionLogger.LogAction("ATEMクライアント設定", "SwitcherViewModelにATEMクライアントを設定しました");
                            }
                            catch (Exception ex)
                            {
                                ActionLogger.LogError("ATEMクライアント設定エラー", 
                                    $"コールバック実行中にエラーが発生しました: {ex.Message}");
                            }
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        ActionLogger.LogError("ATEM接続失敗", 
                            $"接続タイムアウト: {ipAddress} への接続が{timeoutSeconds}秒以内に完了しませんでした");
                    }
                }).ContinueWith(task =>
                {
                    if (task.IsFaulted && task.Exception != null)
                    {
                        var exceptionMessage = string.Join("; ", 
                            task.Exception.Flatten().InnerExceptions.Select(e => e.Message));
                        ActionLogger.LogError("ATEM接続タスク失敗", $"タスク例外: {exceptionMessage}");
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
            catch (Exception ex)
            {
                ActionLogger.LogError("スイッチャー接続サービスエラー", 
                    $"例外: {ex.GetType().Name} - {ex.Message}\nスタックトレース: {ex.StackTrace}");
            }
        }
    }
}

