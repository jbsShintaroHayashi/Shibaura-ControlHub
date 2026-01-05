using Shibaura_ControlHub.Models;
using Shibaura_ControlHub.Services;
using Shibaura_ControlHub.Utils;
using Shibaura_ControlHub.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Application = System.Windows.Application;

namespace Shibaura_ControlHub.ViewModels
{
    public class CameraViewModel : BaseViewModel
    {
        private Dictionary<int, ModeSettingsData> _allModeSettings = new Dictionary<int, ModeSettingsData>();
        private ModeSettingsData? _modeSettingsData;
        private EquipmentStatus? _selectedCamera;
        private int _selectedPresetNumber = 0;
        private int _selectedAiStillNumber = 0;
        private bool _isTrackingOn = false;
        private bool _isCamera1Selected = false;
        private readonly SonyPtzCameraClient _cameraClient;

        public ObservableCollection<EquipmentStatus> CameraList { get; set; } = null!;
        
        public EquipmentStatus? Camera1 => CameraList != null && CameraList.Count > 0 ? CameraList[0] : null;
        public EquipmentStatus? Camera2 => CameraList != null && CameraList.Count > 1 ? CameraList[1] : null;
        public EquipmentStatus? Camera3 => CameraList != null && CameraList.Count > 2 ? CameraList[2] : null;
        public bool Camera1Enabled => Camera1 != null;
        public bool Camera2Enabled => Camera2 != null;
        public bool Camera3Enabled => Camera3 != null;
        public ObservableCollection<CameraPresetItem> CameraPresets { get; set; } = new ObservableCollection<CameraPresetItem>();

        public EquipmentStatus? SelectedCamera
        {
            get => _selectedCamera;
            set
            {
                _selectedCamera = value;
                UpdateIsCamera1Selected();
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsCamera1Selected));
                LoadCameraPresetsForCurrentMode();
            }
        }

        public bool IsCamera1Selected
        {
            get => _isCamera1Selected;
            private set
            {
                if (_isCamera1Selected != value)
                {
                    _isCamera1Selected = value;
                    OnPropertyChanged();
                }
            }
        }

        public int SelectedPresetNumber
        {
            get => _selectedPresetNumber;
            set
            {
                _selectedPresetNumber = value;
                OnPropertyChanged();
            }
        }

        private int _calledPresetNumber = 0;
        public int CalledPresetNumber
        {
            get => _calledPresetNumber;
            set
            {
                _calledPresetNumber = value;
                OnPropertyChanged();
            }
        }

        public int SelectedAiStillNumber
        {
            get => _selectedAiStillNumber;
            set
            {
                _selectedAiStillNumber = value;
                OnPropertyChanged();
            }
        }

        private int _calledAiStillNumber = 0;
        public int CalledAiStillNumber
        {
            get => _calledAiStillNumber;
            set
            {
                _calledAiStillNumber = value;
                OnPropertyChanged();
            }
        }

        public bool IsTrackingOn
        {
            get => _isTrackingOn;
            set
            {
                _isTrackingOn = value;
                OnPropertyChanged();
            }
        }

        public ICommand SelectCameraCommand { get; private set; } = null!;
        public ICommand SelectPresetCommand { get; private set; } = null!;
        public ICommand CallPresetCommand { get; private set; } = null!;
        public ICommand RegisterPresetCommand { get; private set; } = null!;
        public ICommand SelectAiStillCommand { get; private set; } = null!;
        public ICommand CallAiStillCommand { get; private set; } = null!;
        public ICommand ToggleAiTrackingCommand { get; private set; } = null!;
        public ICommand MoveCameraCommand { get; private set; } = null!;
        public ICommand ZoomCameraCommand { get; private set; } = null!;

        public CameraViewModel(string mode, ObservableCollection<EquipmentStatus> cameraList)
        {
            CameraList = cameraList;
            _cameraClient = new SonyPtzCameraClient();
            SetCurrentModeFromName(mode);
            
            LoadModeSettings();
            InitializePresets();
            InitializeCommands();
            
            if (CameraList != null && CameraList.Count > 0)
            {
                SelectedCamera = CameraList[0];
            }
            
            if (CameraList != null)
            {
                CameraList.CollectionChanged += (s, e) =>
                {
                    OnPropertyChanged(nameof(Camera1));
                    OnPropertyChanged(nameof(Camera2));
                    OnPropertyChanged(nameof(Camera3));
                    OnPropertyChanged(nameof(Camera1Enabled));
                    OnPropertyChanged(nameof(Camera2Enabled));
                    OnPropertyChanged(nameof(Camera3Enabled));
                };
            }
        }

        protected override void OnModeChanged()
        {
            base.OnModeChanged();
            
            int modeNumber = CurrentMode;
            if (modeNumber == 0) return;
            
            if (!_allModeSettings.ContainsKey(modeNumber))
            {
                LoadModeSettings(ModeSettingsManager.GetModeName(CurrentMode));
            }
            _modeSettingsData = _allModeSettings[modeNumber];
            
            LoadCameraPresetsForCurrentMode();
            
            OnPropertyChanged(nameof(CameraPresets));
            OnPropertyChanged(nameof(SelectedCamera));
        }

        private void UpdateIsCamera1Selected()
        {
            IsCamera1Selected = Camera1 != null && _selectedCamera != null && _selectedCamera == Camera1;
        }

        private void InitializeCommands()
        {
            SelectCameraCommand = new RelayCommand<EquipmentStatus>(SelectCamera);
            SelectPresetCommand = new RelayCommand<int>(SelectPreset);
            CallPresetCommand = new RelayCommand(CallSelectedPreset);
            RegisterPresetCommand = new RelayCommand(RegisterSelectedPreset);
            SelectAiStillCommand = new RelayCommand<int>(SelectAiStill);
            CallAiStillCommand = new RelayCommand(CallSelectedAiStill);
            ToggleAiTrackingCommand = new RelayCommand<string>(ToggleAiTracking);
            MoveCameraCommand = new RelayCommand<string>(StartMoveCamera);
            ZoomCameraCommand = new RelayCommand<string>(ZoomCamera);
        }

        private void LoadModeSettings()
        {
            LoadModeSettings(ModeSettingsManager.GetModeName(CurrentMode));
        }

        private void LoadModeSettings(string mode)
        {
            int modeNumber = ModeSettingsManager.GetModeNumber(mode);
            LoadModeSettingsForModeNumber(modeNumber);
        }

        internal void LoadModeSettingsForModeNumber(int modeNumber)
        {
            if (modeNumber == 0) return;
            
            if (!_allModeSettings.ContainsKey(modeNumber))
            {
                var settings = ModeSettingsManager.LoadModeSettings(modeNumber);
                
                _allModeSettings[modeNumber] = settings;
            }
            
            if (modeNumber == CurrentMode)
            {
                _modeSettingsData = _allModeSettings[modeNumber];
            }
        }

        private void InitializePresets()
        {
            for (int i = 1; i <= 8; i++)
            {
                int presetNumber = i;
                CameraPresets.Add(new CameraPresetItem
                {
                    Number = i,
                    IsRegistered = false,
                    CallCommand = new RelayCommand(() => CallPresetItem(presetNumber)),
                    RegisterCommand = new RelayCommand(() => RegisterPresetItem(presetNumber))
                });
            }
        }

        private void SelectCamera(EquipmentStatus? camera)
        {
            string cameraName = camera != null ? camera.Name : "なし";
            ActionLogger.LogAction("カメラ選択", $"カメラ: {cameraName}");
            SelectedCamera = camera;
        }

        private void SelectPreset(int presetNumber)
        {
            ActionLogger.LogAction("プリセット選択", $"プリセット番号: {presetNumber}");
            SelectedPresetNumber = presetNumber;
        }

        private void CallSelectedPreset()
        {
            if (SelectedPresetNumber == 0)
            {
                ActionLogger.LogError("プリセット呼出", "プリセットが選択されていません");
                CustomDialog.Show("プリセットが選択されていません。", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = CustomDialog.Show(
                $"プリセット{SelectedPresetNumber}を呼び出しますか？",
                "プリセット呼出確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            CallPresetItem(SelectedPresetNumber);
            CalledPresetNumber = SelectedPresetNumber;
        }

        private void RegisterSelectedPreset()
        {
            if (SelectedPresetNumber == 0)
            {
                ActionLogger.LogError("プリセット登録", "プリセットが選択されていません");
                CustomDialog.Show("プリセットが選択されていません。", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ActionLogger.LogAction("プリセット登録", $"プリセット番号: {SelectedPresetNumber}");
            RegisterPresetItem(SelectedPresetNumber);
        }

        private void SelectAiStill(int stillNumber)
        {
            ActionLogger.LogAction("AIスチル選択", $"スチル番号: {stillNumber}");
            SelectedAiStillNumber = stillNumber;
        }

        private void CallSelectedAiStill()
        {
            if (SelectedAiStillNumber == 0)
            {
                ActionLogger.LogError("AIスチル呼出", "スチルが選択されていません");
                CustomDialog.Show("スチルが選択されていません。", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (SelectedCamera == null)
            {
                ActionLogger.LogError("AIスチル呼出", "カメラが選択されていません");
                CustomDialog.Show("カメラが選択されていません。", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            CalledAiStillNumber = SelectedAiStillNumber;
            
            CustomDialog.Show(
                $"スチル{SelectedAiStillNumber}を呼び出しました\n" +
                $"カメラ: {SelectedCamera.Name}",
                "スチル呼び出し",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ToggleAiTracking(string? state)
        {
            if (string.IsNullOrEmpty(state))
            {
                ActionLogger.LogError("トラッキング切り替え", "状態が指定されていません");
                return;
            }

            IsTrackingOn = state == "On";
        }

        private void RegisterPresetItem(int presetNumber)
        {
            if (SelectedCamera == null)
            {
                ActionLogger.LogError("プリセット登録", "カメラが選択されていません");
                CustomDialog.Show("カメラが選択されていません。", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = CustomDialog.Show(
                $"プリセット{presetNumber}を登録しますか？",
                "プリセット登録確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            int cameraNumber = GetCameraNumber(SelectedCamera);
            
            // カメラAPIに登録
            if (SelectedCamera != null && !string.IsNullOrEmpty(SelectedCamera.IpAddress))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _cameraClient.StorePresetAsync(SelectedCamera.IpAddress, presetNumber, $"Preset{presetNumber}", "off", CancellationToken.None);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var presetItem = CameraPresets.FirstOrDefault(p => p.Number == presetNumber);
                            if (presetItem != null)
                            {
                                presetItem.IsRegistered = true;
                            }
                            // 登録後、呼び出し済み状態に変更
                            CalledPresetNumber = presetNumber;
                            ActionLogger.LogResult("プリセット登録成功", $"カメラ{cameraNumber}のプリセット{presetNumber}を登録しました");
                        });
                    }
                    catch (Exception ex)
                    {
                        ActionLogger.LogError("プリセット登録エラー", $"カメラ{cameraNumber}のプリセット{presetNumber}登録中にエラー: {ex.Message}");
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            CustomDialog.Show($"プリセット{presetNumber}の登録に失敗しました。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                    }
                });
            }
        }

        private void CallPresetItem(int presetNumber)
        {
            if (SelectedCamera == null)
            {
                ActionLogger.LogError("プリセット呼出", "カメラが選択されていません");
                CustomDialog.Show("カメラが選択されていません。", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int cameraNumber = 0;
            if (CameraList != null)
            {
                for (int i = 0; i < CameraList.Count; i++)
                {
                    if (CameraList[i] == SelectedCamera)
                    {
                        cameraNumber = i + 1;
                        break;
                    }
                }
            }

            if (cameraNumber == 0)
            {
                ActionLogger.LogError("プリセット呼出", "カメラ番号を取得できませんでした");
                CustomDialog.Show("カメラ番号を取得できませんでした。", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // SonyPtzCameraClientを使用してプリセット呼び出し
            if (SelectedCamera != null && !string.IsNullOrEmpty(SelectedCamera.IpAddress))
            {
                try
                {
                    ActionLogger.LogProcessing("カメラプリセット呼出", $"カメラ{cameraNumber} ({SelectedCamera.IpAddress}) のプリセット{presetNumber}を呼び出し");
                    _ = Task.Run(async () => await _cameraClient.RecallPresetAsync(SelectedCamera.IpAddress, presetNumber, CancellationToken.None));
                    ActionLogger.LogResult("カメラプリセット呼出成功", $"カメラ{cameraNumber}のプリセット{presetNumber}を呼び出しました");
                }
                catch (Exception ex)
                {
                    ActionLogger.LogError("カメラプリセット呼出エラー", $"カメラ{cameraNumber}のプリセット{presetNumber}呼び出し中にエラー: {ex.Message}");
                }
            }
        }

        private void LoadCameraPresetsForCurrentMode()
        {
            if (CurrentMode == 0 || SelectedCamera == null)
            {
                return;
            }

            foreach (var preset in CameraPresets)
            {
                preset.IsRegistered = false;
            }
        }

        /// <summary>
        /// 選択状態のみをクリア（呼び出し状態は保持）
        /// </summary>
        public void ClearPresetSelectionOnly()
        {
            SelectedPresetNumber = 0;
        }

        private string? _currentMoveDirection = null;
        private CancellationTokenSource? _moveCancellationTokenSource = null;
        private readonly object _moveLock = new object();
        private bool _isStoppingMove = false;
        private bool _isMoving = false;
        private DateTime _lastStopTime = DateTime.MinValue;
        private readonly TimeSpan _stopDebounceTime = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// カメラ移動を開始（9方向対応）
        /// </summary>
        public void StartMoveCamera(string? direction)
        {
            if (SelectedCamera == null || string.IsNullOrEmpty(direction))
            {
                return;
            }

            // プリセット選択状態と呼び出し済み状態をクリア
            SelectedPresetNumber = 0;
            CalledPresetNumber = 0;

            if (string.IsNullOrWhiteSpace(SelectedCamera.IpAddress))
            {
                ActionLogger.LogError("カメラ移動", "カメラのIPアドレスが設定されていません");
                return;
            }

            int cameraNumber = GetCameraNumber(SelectedCamera);
            ActionLogger.LogAction("カメラ移動開始", $"カメラ: {SelectedCamera.Name} (カメラ{cameraNumber}), 方向: {direction}");
            
            var ip = SelectedCamera.IpAddress;
            
            // 既存の移動タスクをキャンセル
            lock (_moveLock)
            {
                // 停止処理直後は開始を無視（デバウンス処理）
                var timeSinceLastStop = DateTime.Now - _lastStopTime;
                if (timeSinceLastStop < _stopDebounceTime)
                {
                    return;
                }
                
                // 既に同じ方向で移動中の場合は無視（重複呼び出し防止）
                if (_isMoving && _currentMoveDirection == direction)
                {
                    return;
                }
                
                _moveCancellationTokenSource?.Cancel();
                _moveCancellationTokenSource?.Dispose();
                _moveCancellationTokenSource = new CancellationTokenSource();
                _currentMoveDirection = direction;
                _isMoving = true;
            }

            // 非同期処理で実行
            _ = Task.Run(async () =>
            {
                CancellationToken cancellationToken;
                string currentDirection;
                
                lock (_moveLock)
                {
                    cancellationToken = _moveCancellationTokenSource?.Token ?? CancellationToken.None;
                    currentDirection = _currentMoveDirection ?? "";
                }

                try
                {
                    // 中央ボタンの場合はホームポジションを呼び出す
                    if (direction == "Center")
                    {
                        // キャンセルチェック
                        cancellationToken.ThrowIfCancellationRequested();
                        await _cameraClient.RecallHomePositionAsync(ip, cancellationToken);
                        return;
                    }

                    // 9方向をAPI仕様に合わせて変換
                    string moveDirection = "";
                    switch (direction)
                       {
                           case "Up":
                               moveDirection = "up";
                               break;
                           case "Down":
                               moveDirection = "down";
                               break;
                           case "Left":
                               moveDirection = "left";
                               break;
                           case "Right":
                               moveDirection = "right";
                               break;
                           case "UpLeft":
                               moveDirection = "up-left";
                               break;
                           case "UpRight":
                               moveDirection = "up-right";
                               break;
                           case "DownLeft":
                               moveDirection = "down-left";
                               break;
                           case "DownRight":
                               moveDirection = "down-right";
                               break;
                           default:
                               // 無効な方向は無視
                               return;
                       }

                       if (!string.IsNullOrEmpty(moveDirection))
                       {
                           // キャンセルチェック
                           cancellationToken.ThrowIfCancellationRequested();
                           
                           // 速度0で移動開始（0を設定するとズーム位置によって速度が変わる）
                           await _cameraClient.MoveAsync(ip, moveDirection, speed: 0, cancellationToken);
                       }
                }
                catch (OperationCanceledException)
                {
                    // キャンセル時は無視
                }
                catch (Exception ex)
                {
                    int cameraNumber = GetCameraNumber(SelectedCamera);
                    ActionLogger.LogError("カメラ移動エラー", 
                        $"カメラ{cameraNumber}, 方向: {direction}\n" +
                        $"エラー: {ex.GetType().Name} - {ex.Message}\n" +
                        $"スタックトレース: {ex.StackTrace}");
                }
                finally
                {
                    lock (_moveLock)
                    {
                        // 移動が完了またはキャンセルされた場合のみフラグをリセット
                        if (_currentMoveDirection == direction)
                        {
                            _isMoving = false;
                        }
                    }
                }
            });
        }

        /// <summary>
        /// カメラ移動を停止
        /// </summary>
        public void StopMoveCamera()
        {
            // 既に停止処理中の場合は無視（重複呼び出し防止）
            lock (_moveLock)
            {
                if (_isStoppingMove)
                {
                    return;
                }
                _isStoppingMove = true;
            }
            
            if (SelectedCamera == null)
            {
                lock (_moveLock)
                {
                    _isStoppingMove = false;
                }
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedCamera.IpAddress))
            {
                lock (_moveLock)
                {
                    _isStoppingMove = false;
                }
                ActionLogger.LogError("カメラ移動停止", "カメラのIPアドレスが設定されていません");
                return;
            }

            int cameraNumber = GetCameraNumber(SelectedCamera);
            ActionLogger.LogAction("カメラ移動停止", $"カメラ: {SelectedCamera.Name} (カメラ{cameraNumber}), 方向: {_currentMoveDirection ?? "不明"}");

            var ip = SelectedCamera.IpAddress;
            
            // キャンセルトークンでタスクをキャンセル
            lock (_moveLock)
            {
                _moveCancellationTokenSource?.Cancel();
                _moveCancellationTokenSource?.Dispose();
                _moveCancellationTokenSource = null;
                _currentMoveDirection = null;
                _isMoving = false;
                _lastStopTime = DateTime.Now;
            }
            
            // 停止コマンドを非同期送信
            _ = Task.Run(async () =>
            {
                try
                {
                    int cameraNumber = GetCameraNumber(SelectedCamera);
                    await _cameraClient.MoveStopAsync(ip, CancellationToken.None);
                    ActionLogger.LogResult("カメラ移動停止成功", $"カメラ{cameraNumber}の移動を停止しました");
                }
                catch (Exception ex)
                {
                    ActionLogger.LogError("カメラ停止エラー",
                        $"エラー: {ex.GetType().Name} - {ex.Message}\nスタックトレース: {ex.StackTrace}");
                }
                finally
                {
                    lock (_moveLock)
                    {
                        _isStoppingMove = false;
                    }
                }
            });
        }

        private string? _currentZoomDirection = null;
        private CancellationTokenSource? _zoomCancellationTokenSource = null;
        private bool _isStoppingZoom = false;

        /// <summary>
        /// カメラズーム（In/Out）
        /// </summary>
        private void ZoomCamera(string? direction)
        {
            // Command経由の場合は開始のみ（イベントハンドラーで制御）
            StartZoom(direction);
        }

        /// <summary>
        /// ズーム開始
        /// </summary>
        public void StartZoom(string? direction)
        {
            if (SelectedCamera == null || string.IsNullOrEmpty(direction))
            {
                return;
            }

            // プリセット選択状態と呼び出し済み状態をクリア
            SelectedPresetNumber = 0;
            CalledPresetNumber = 0;

            StopZoom();

            if (SelectedCamera == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(SelectedCamera.IpAddress))
            {
                var ip = SelectedCamera.IpAddress;
                
                // 既存のズームタスクをキャンセル
                _zoomCancellationTokenSource?.Cancel();
                _zoomCancellationTokenSource?.Dispose();
                _zoomCancellationTokenSource = new CancellationTokenSource();
                _currentZoomDirection = direction;

                // 非同期処理で実行
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (direction == "In")
                        {
                            // ズームイン開始
                            await _cameraClient.ZoomAsync(ip, "tele", speed: 0, _zoomCancellationTokenSource.Token);
                        }
                        else if (direction == "Out")
                        {
                            // ズームアウト開始
                            await _cameraClient.ZoomAsync(ip, "wide", speed: 0, _zoomCancellationTokenSource.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // キャンセル時は無視
                    }
                    catch (Exception ex)
                    {
                        ActionLogger.LogError("カメラズームエラー", 
                            $"エラー: {ex.GetType().Name} - {ex.Message}\nスタックトレース: {ex.StackTrace}");
                    }
                }, _zoomCancellationTokenSource.Token);
            }
        }

        /// <summary>
        /// ズーム停止
        /// </summary>
        public void StopZoom()
        {
            // 既に停止処理中の場合は無視（重複呼び出し防止）
            if (_isStoppingZoom)
            {
                return;
            }
            _isStoppingZoom = true;
            
            if (_currentZoomDirection == null || SelectedCamera == null)
            {
                _isStoppingZoom = false;
                return;
            }

            // キャンセルトークンでタスクをキャンセル
            _zoomCancellationTokenSource?.Cancel();
            _zoomCancellationTokenSource?.Dispose();
            _zoomCancellationTokenSource = null;
            _currentZoomDirection = null;

            if (!string.IsNullOrWhiteSpace(SelectedCamera.IpAddress))
            {
                var ip = SelectedCamera.IpAddress;
                
                // 停止コマンドを非同期送信
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _cameraClient.ZoomStopAsync(ip, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        ActionLogger.LogError("カメラズーム停止エラー", 
                            $"エラー: {ex.GetType().Name} - {ex.Message}\nスタックトレース: {ex.StackTrace}");
                    }
                    finally
                    {
                        _isStoppingZoom = false;
                    }
                });
            }
            else
            {
                _isStoppingZoom = false;
            }
        }

        /// <summary>
        /// 選択されたカメラの番号を取得
        /// </summary>
        private int GetCameraNumber(EquipmentStatus camera)
        {
            if (CameraList == null || camera == null)
            {
                return 0;
            }

            for (int i = 0; i < CameraList.Count; i++)
            {
                if (CameraList[i] == camera)
                {
                    return i + 1;
                }
            }

            return 0;
        }
    }
}

