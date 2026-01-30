using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Shibaura_ControlHub.Models;
using Shibaura_ControlHub.Services;
using Shibaura_ControlHub.Utils;
using Shibaura_ControlHub.Views;

namespace Shibaura_ControlHub.ViewModels
{
    /// <summary>
    /// MainWindowのViewModel
    /// </summary>
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly EquipmentControlService _controlService;
        private DispatcherTimer? _autoRefreshTimer;
        private string _selectedMode = string.Empty;
        private string _currentMode = "未設定";
        private string _controlStatus = "待機中";
        private int _operationHistoryCount = 0;

        // 機材リスト
        public ObservableCollection<EquipmentStatus> EquipmentList { get; set; }

        // 操作履歴
        public ObservableCollection<string> OperationHistory { get; set; }

        public MainWindowViewModel()
        {
            _controlService = new EquipmentControlService();
            
            OperationHistory = new ObservableCollection<string>();

            // コマンドを初期化
            Mode1Command = new RelayCommand(SelectMode1);
            Mode2Command = new RelayCommand(SelectMode2);
            ExecuteCommand = new RelayCommand(ExecuteControl, CanExecute);

            // 自動更新タイマーを開始
            InitializeAutoRefreshTimer();
            UpdateHistoryCount();
        }

        // 現在のモード
        public string CurrentMode
        {
            get => _currentMode;
            set { _currentMode = value; OnPropertyChanged(); }
        }

        // 制御状態
        public string ControlStatus
        {
            get => _controlStatus;
            set { _controlStatus = value; OnPropertyChanged(); }
        }

        // 操作履歴数
        public int OperationHistoryCount
        {
            get => _operationHistoryCount;
            set { _operationHistoryCount = value; OnPropertyChanged(); }
        }

        // コマンド
        public ICommand Mode1Command { get; }
        public ICommand Mode2Command { get; }
        public ICommand ExecuteCommand { get; }
        public ICommand ClearHistoryCommand { get; }

        /// <summary>
        /// 自動更新タイマーを初期化
        /// </summary>
        private void InitializeAutoRefreshTimer()
        {
            _autoRefreshTimer = new DispatcherTimer();
            _autoRefreshTimer.Interval = TimeSpan.FromSeconds(10);
            _autoRefreshTimer.Tick += AutoRefreshTimer_Tick;
            _autoRefreshTimer.Start();

            _ = RefreshEquipmentStatusAsync();
        }

        /// <summary>
        /// モード1を選択
        /// </summary>
        private void SelectMode1()
        {
            _selectedMode = "モード1";
            CurrentMode = "モード1";
            OnPropertyChanged(nameof(Mode1Command));
            OnPropertyChanged(nameof(Mode2Command));
            OnPropertyChanged(nameof(ExecuteCommand));
        }

        /// <summary>
        /// モード2を選択
        /// </summary>
        private void SelectMode2()
        {
            _selectedMode = "モード2";
            CurrentMode = "モード2";
            OnPropertyChanged(nameof(Mode1Command));
            OnPropertyChanged(nameof(Mode2Command));
            OnPropertyChanged(nameof(ExecuteCommand));
        }

        /// <summary>
        /// 実行可能かどうか
        /// </summary>
        private bool CanExecute()
        {
            return !string.IsNullOrEmpty(_selectedMode);
        }

        /// <summary>
        /// 制御を実行
        /// </summary>
        private void ExecuteControl()
        {
            if (string.IsNullOrEmpty(_selectedMode))
            {
                CustomDialog.Show(
                    "モードを選択してください。",
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // 確認ダイアログを表示
            var result = CustomDialog.Show(
                $"以下の設定で機材制御を実行しますか？\n\n" +
                $"モード: {_selectedMode}\n" +
                $"続行しますか？",
                "実行確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                // 実際の制御処理を実行
                _controlService.ExecuteMode(_selectedMode);

                var timestamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                var historyMessage = $"[{timestamp}] {_selectedMode}実行 - 全機材に適用";

                OperationHistory.Insert(0, historyMessage);

                // 表示制限（最新100件）
                if (OperationHistory.Count > 100)
                {
                    OperationHistory.RemoveAt(OperationHistory.Count - 1);
                }

                UpdateHistoryCount();

                // 状態を更新
                ControlStatus = "実行中";

                CustomDialog.Show(
                    $"機材制御を実行しました。\n\n" +
                    $"モード: {_selectedMode}\n" +
                    $"実行時刻: {timestamp}",
                    "制御実行完了",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // 1秒後に状態を「実行完了」に更新
                var timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += (s, args) =>
                {
                    ControlStatus = "実行完了";
                    if (s is DispatcherTimer t)
                    {
                        t.Stop();
                    }
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                ActionLogger.LogError("機材制御実行", $"機材制御に失敗しました: {ex.Message}");
                CustomDialog.Show(
                    $"機材制御の実行中にエラーが発生しました。\n\n{ex.Message}",
                    "機材制御エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 自動更新タイマーのTickイベント
        /// </summary>
        private async void AutoRefreshTimer_Tick(object? sender, EventArgs e)
        {
            await RefreshEquipmentStatusAsync();
        }

        private async Task RefreshEquipmentStatusAsync()
        {
            if (EquipmentList == null || EquipmentList.Count == 0)
            {
                return;
            }

            var snapshot = EquipmentList.ToList();
            var updates = await _controlService.CheckEquipmentStatusAsync(snapshot);
            foreach (var update in updates)
            {
                update.Apply();
            }
        }

        /// <summary>
        /// 記録数を更新
        /// </summary>
        private void UpdateHistoryCount()
        {
            OperationHistoryCount = OperationHistory.Count;
        }

        /// <summary>
        /// クリーンアップ処理
        /// </summary>
        public void Cleanup()
        {
            if (_autoRefreshTimer != null)
            {
                _autoRefreshTimer.Stop();
            }
            _autoRefreshTimer = null;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

