using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Shibaura_ControlHub.Models;
using Shibaura_ControlHub;
using Shibaura_ControlHub.Views;
using WpfApplication = System.Windows.Application;

namespace Shibaura_ControlHub.ViewModels
{
    /// <summary>
    /// メニュー画面のViewModel
    /// </summary>
    public class MenuViewModel : INotifyPropertyChanged
    {
        private EquipmentStatus? _selectedEquipment;
        private ObservableCollection<EquipmentStatus> _equipmentList = new ObservableCollection<EquipmentStatus>();
        private int _hiddenButtonClickCount = 0;
        private DispatcherTimer? _hiddenButtonTimer;

        public ObservableCollection<EquipmentStatus> EquipmentList
        {
            get => _equipmentList;
            set
            {
                _equipmentList = value;
                OnPropertyChanged();
            }
        }

        public EquipmentStatus? SelectedEquipment
        {
            get => _selectedEquipment;
            set
            {
                _selectedEquipment = value;
                OnPropertyChanged();
            }
        }

        public ICommand Mode1Command { get; }
        public ICommand Mode2Command { get; }
        public ICommand Mode3Command { get; }
        public ICommand HiddenButtonCommand { get; }
        public ICommand PowerOffCommand { get; }

        /// <summary>
        /// モード1の名前（XAMLバインディング用）
        /// </summary>
        public string Mode1Name => App.ModeConfig.Mode1Name;

        /// <summary>
        /// モード2の名前（XAMLバインディング用）
        /// </summary>
        public string Mode2Name => App.ModeConfig.Mode2Name;

        /// <summary>
        /// モード3の名前（XAMLバインディング用）
        /// </summary>
        public string Mode3Name => App.ModeConfig.Mode3Name;

        public event Action<string>? ModeSelected;
        public event Action? PowerOffRequested;

        public MenuViewModel(ObservableCollection<EquipmentStatus> equipmentList)
        {
            EquipmentList = equipmentList;

            // 最初の機器を選択
            if (EquipmentList.Count > 0)
            {
                SelectedEquipment = EquipmentList[0];
            }

            // INIファイルから読み込んだモード名を使用
            Mode1Command = new RelayCommand(() => SelectModeWithConfirmation(App.ModeConfig.Mode1Name));
            Mode2Command = new RelayCommand(() => SelectModeWithConfirmation(App.ModeConfig.Mode2Name));
            Mode3Command = new RelayCommand(() => SelectModeWithConfirmation(App.ModeConfig.Mode3Name));
            HiddenButtonCommand = new RelayCommand<string>(obj => HandleHiddenButtonClick(obj ?? string.Empty));
            PowerOffCommand = new RelayCommand(() => RequestPowerOff());
        }

        private void RequestPowerOff()
        {
            var result = CustomDialog.Show("システムの電源をオフにしますか？", "電源オフ", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                PowerOffRequested?.Invoke();
            }
        }

        /// <summary>
        /// モード選択時の確認ダイアログを表示してからモードを選択
        /// </summary>
        private void SelectModeWithConfirmation(string modeName)
        {
            var result = CustomDialog.Show(
                $"「{modeName}」モードを開始しますか？",
                "モード選択確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ModeSelected?.Invoke(modeName);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 隠しボタンクリック時の処理
        /// </summary>
        private void HandleHiddenButtonClick(string buttonPosition)
        {
            // 左下を5回クリックで終了
            if (buttonPosition != "LeftBottom")
            {
                // 左下以外がクリックされた場合は何もしない
                return;
            }
            
            // 1回目のクリック時にタイマーを開始
            if (_hiddenButtonClickCount == 0)
            {
                // タイマーを開始（5秒後にカウントをリセット）
                _hiddenButtonTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(5)
                };
                _hiddenButtonTimer.Tick += (s, e) =>
                {
                    _hiddenButtonClickCount = 0;
                    _hiddenButtonTimer?.Stop();
                    _hiddenButtonTimer = null;
                };
                _hiddenButtonTimer.Start();
            }

            _hiddenButtonClickCount++;
            
            // 5回クリックされたらアプリケーション終了
            if (_hiddenButtonClickCount >= 5)
            {
                _hiddenButtonTimer?.Stop();
                _hiddenButtonTimer = null;
                WpfApplication.Current.Shutdown();
            }
        }

    }
}

