using Shibaura_ControlHub.Services;
using Shibaura_ControlHub.Models;
using Shibaura_ControlHub.Services;
using Shibaura_ControlHub.Utils;
using Shibaura_ControlHub.ViewModels;
using Shibaura_ControlHub.Views;
using Shibaura_Lib.Udp;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Shibaura_ControlHub
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MenuViewModel? _menuViewModel;
        private PowerOnViewModel _powerOnViewModel;
        private ProgressViewModel _progressViewModel;
        private DispatcherTimer? _progressTimer;
        private ModeControlViewModel _modeControlViewModel;
        private ObservableCollection<EquipmentStatus> _microphoneList = new ObservableCollection<EquipmentStatus>();
        private ObservableCollection<EquipmentStatus> _cameraList = new ObservableCollection<EquipmentStatus>();
        private readonly JsonFileService _jsonFileService = new JsonFileService();
        private ObservableCollection<EquipmentStatus> _switcherList = new ObservableCollection<EquipmentStatus>();
        private ObservableCollection<EquipmentStatus> _monitoringEquipmentList = new ObservableCollection<EquipmentStatus>();
        private UdpMessageReceiver? _receiver;
        private readonly CommandCommunicationService _commandCommunicationService;
        private bool _isModeViewActive;
        private string? _currentModeName;

        public MainWindow()
        {
            InitializeComponent();
            
            _commandCommunicationService = new CommandCommunicationService(Dispatcher, HandleRemoteModeStart);
            
            Init();
            // JSONファイルから機器データを読み込む
            LoadEquipmentFromJson();
            
            ShowPowerOnView();
        }

        private void Init()
        {
            try
            {
                // 受信クラスのインスタンス作成
                _receiver = new UdpMessageReceiver();

                // メッセージ受信イベントの登録
                _receiver.MessageReceived += OnMessageReceived;

                // 受信開始
                _receiver.Start(9000);
            }
            catch (Exception ex)
            {
                CustomDialog.Show($"初期化中にエラーが発生しました:\n{ex.Message}",
                    "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// JSONファイルから機器データを読み込む
        /// </summary>
        private void LoadEquipmentFromJson()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                
                // 操作用機器リスト（デバイスタイプごとに分類）
                _microphoneList.Clear();
                _cameraList.Clear();
                _switcherList.Clear();
                
                // 監視用機器リストの読み込み（Configフォルダから）
                var monitoringJsonPath = Path.Combine(baseDir, "Config", "monitoring_equipment.json");
                var equipmentDatatMonitoring = _jsonFileService.ReadFromFile<EquipmentData>(monitoringJsonPath);

                if (equipmentDatatMonitoring != null && equipmentDatatMonitoring.Equipment != null)
                {
                    _monitoringEquipmentList = new ObservableCollection<EquipmentStatus>(equipmentDatatMonitoring.Equipment);
                    
                    // カメラリスト：読み込んだカメラの1個目から3つ目までを追加
                    var cameras = equipmentDatatMonitoring.Equipment
                        .Where(e => e.DeviceType == (int)EquipmentType.Camera)
                        .Take(3)
                        .ToList();
                    
                    foreach (var camera in cameras)
                    {
                        _cameraList.Add(camera);
                    }
                    
                    // スイッチャーリスト：読み込んだスイッチャーを追加
                    var switchers = equipmentDatatMonitoring.Equipment
                        .Where(e => e.DeviceType == (int)EquipmentType.Switcher)
                        .ToList();
                    
                    foreach (var switcher in switchers)
                    {
                        _switcherList.Add(switcher);
                    }
                }
            }
            catch (Exception ex)
            {
                CustomDialog.Show($"JSONファイルの読み込み中にエラーが発生しました:\n{ex.Message}", 
                    "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowPowerOnView()
        {
            _powerOnViewModel = new PowerOnViewModel();
            var powerView = new PowerOnView
            {
                DataContext = _powerOnViewModel
            };

            _powerOnViewModel.PowerOnRequested += () =>
            {
                ShowProgressView("システムの電源をオンにしています...", () =>
                {
                    ShowMenuView();
                });
            };

            CurrentView.Content = powerView;
        }

        private void OnMessageReceived(object? sender, UdpMessageReceivedEventArgs e)
        {
            _commandCommunicationService.HandleMessage(e);
        }
        /// <summary>
        /// メニュー画面を表示
        /// </summary>
        private void ShowMenuView()
        {
            _menuViewModel = new MenuViewModel(_monitoringEquipmentList);
            var menuView = new MenuView();
            menuView.DataContext = _menuViewModel;
            
            // モード選択時のイベントハンドラーを設定
            _menuViewModel.ModeSelected += StartModeWithProgress;

            CurrentView.Content = menuView;

            _menuViewModel.PowerOffRequested += () =>
            {
                ShowProgressView("システムの電源をオフにしています...", () =>
                {
                    ShowPowerOnView();
                });
            };
        }

        private void ShowProgressView(string message, Action? onCompleted = null)
        {
            // 既存のタイマーを停止
            if (_progressTimer != null)
            {
                _progressTimer.Stop();
                _progressTimer = null;
            }

            _progressViewModel = new ProgressViewModel
            {
                Message = message,
                IsIndeterminate = false,
                ProgressValue = 0
            };
            CurrentView.Content = new ProgressView { DataContext = _progressViewModel };

            _progressTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(20) // 20msごとに更新（2秒で100%）
            };

            double currentProgress = 0;
            _progressTimer.Tick += (_, _) =>
            {
                currentProgress += 1.0; // 1%ずつ増やす
                if (_progressViewModel != null)
                {
                    _progressViewModel.ProgressValue = Math.Min(currentProgress, 100.0);
                }

                if (currentProgress >= 100.0)
                {
                    if (_progressTimer != null)
                    {
                        _progressTimer.Stop();
                    }
                    _progressTimer = null;

                    // プログレスバーが100%に到達したらコールバックを実行
                    if (onCompleted != null)
                    {
                        onCompleted.Invoke();
                    }
                }
            };

            _progressTimer.Start();
        }

        /// <summary>
        /// モード制御画面を表示
        /// </summary>
        private void StartModeWithProgress(string mode)
        {
            if (ShouldIgnoreModeRequest(mode))
            {
                return;
            }

            ShowProgressView($"モード「{mode}」を設定しています...", () =>
            {
                ShowModeControlView(mode);
            });
        }

        private void ShowModeControlView(string mode)
        {
            // モード切替時にはJSONファイルから設定を読み込む
            // ModeControlViewModelのコンストラクタで既にLoadAllModeSettingsが呼ばれているが、
            // 明示的に選択されたモードの設定を読み込む
            int modeNumber = ModeSettingsManager.GetModeNumber(mode);
            if (modeNumber > 0)
            {
                // 設定ファイルから読み込む（ModeControlViewModelのコンストラクタで既に読み込まれている）
                var settings = ModeSettingsManager.LoadModeSettings(modeNumber);
            }

            _modeControlViewModel = new ModeControlViewModel(mode, _microphoneList, _cameraList, _switcherList, _monitoringEquipmentList);
            var modeControlView = new ModeControlView();
            modeControlView.DataContext = _modeControlViewModel;
            _currentModeName = mode;
            _isModeViewActive = true;

            // メニューに戻るイベントハンドラーを設定
            _modeControlViewModel.ReturnToMenu += () =>
            {
                _isModeViewActive = false;
                _currentModeName = null;
                ShowMenuView();
            };

            CurrentView.Content = modeControlView;
        }

        private void HandleRemoteModeStart(string modeName)
        {
            StartModeWithProgress(modeName);
        }

        private bool ShouldIgnoreModeRequest(string modeName)
        {
            if (!_isModeViewActive)
            {
                return false;
            }

            if (string.IsNullOrEmpty(_currentModeName))
            {
                return false;
            }

            return string.Equals(_currentModeName, modeName, StringComparison.Ordinal);
        }
    }
}
