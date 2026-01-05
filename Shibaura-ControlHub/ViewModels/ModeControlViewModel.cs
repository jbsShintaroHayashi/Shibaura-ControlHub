using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Shibaura_ControlHub.Models;
using Shibaura_ControlHub.Services;
using Shibaura_ControlHub.Utils;
using Shibaura_ControlHub.Views;

namespace Shibaura_ControlHub.ViewModels
{
    /// <summary>
    /// モード制御画面のViewModel（各機材ViewModelを管理する親クラス）
    /// </summary>
    public class ModeControlViewModel : INotifyPropertyChanged
    {
    private readonly EquipmentControlService _controlService;
    private int _currentMode = 0;
    private string _selectedEquipmentCategory = "";
    private EquipmentStatus? _selectedEquipment;
    private readonly DispatcherTimer _monitoringTimer;
    private readonly ListCollectionView _monitoringView;

    /// <summary>
    /// 各機材ViewModelのインスタンス
    /// </summary>
    public MicrophoneViewModel MicrophoneViewModel { get; private set; } = null!;
    public CameraViewModel CameraViewModel { get; private set; } = null!;
    public SwitcherViewModel SwitcherViewModel { get; private set; } = null!;
    public EsportsViewModel EsportsViewModel { get; private set; } = null!;
    public RecordingViewModel RecordingViewModel { get; private set; } = null!;
    public LightingViewModel LightingViewModel { get; private set; } = null!;
    public CaptureViewModel CaptureViewModel { get; private set; } = null!;

    public ObservableCollection<EquipmentStatus> MicrophoneList { get; set; }
    public ObservableCollection<EquipmentStatus> CameraList { get; set; }
    public ObservableCollection<EquipmentStatus> SwitcherList { get; set; }
    public ObservableCollection<EquipmentStatus> MonitoringList { get; set; }
    public ObservableCollection<string> OperationHistory { get; set; }
    
    public ICollectionView FilteredMonitoringView => _monitoringView;
    
    // 後方互換性のため、各ViewModelのプロパティを公開
    /// <summary>
    /// マイクフェーダー値（4個分）
    /// </summary>
    public ObservableCollection<double> MicrophoneFaderValues
    {
        get => MicrophoneViewModel.MicrophoneFaderValues;
        set => MicrophoneViewModel.MicrophoneFaderValues = value;
    }
    
    /// <summary>
    /// 出力フェーダー値（2個分：S1、S2）
    /// </summary>
    public ObservableCollection<double> OutputFaderValues
    {
        get => MicrophoneViewModel.OutputFaderValues;
        set => MicrophoneViewModel.OutputFaderValues = value;
    }
    
    /// <summary>
    /// カメラプリセットリスト
    /// </summary>
    public ObservableCollection<CameraPresetItem> CameraPresets
    {
        get => CameraViewModel.CameraPresets;
        set => CameraViewModel.CameraPresets = value;
    }

    /// <summary>
    /// esportsマトリクスボタンリスト
    /// </summary>
    public ObservableCollection<EsportsMatrixButton> EsportsMatrixButtons
    {
        get => EsportsViewModel.EsportsMatrixButtons;
        set => EsportsViewModel.EsportsMatrixButtons = value;
    }

    /// <summary>
    /// esportsマトリクスの行ラベル
    /// </summary>
    public ObservableCollection<string> EsportsMatrixRowLabels
    {
        get => EsportsViewModel.EsportsMatrixRowLabels;
        set => EsportsViewModel.EsportsMatrixRowLabels = value;
    }

    /// <summary>
    /// esportsマトリクスの列ラベル
    /// </summary>
    public ObservableCollection<string> EsportsMatrixColumnLabels
    {
        get => EsportsViewModel.EsportsMatrixColumnLabels;
        set => EsportsViewModel.EsportsMatrixColumnLabels = value;
    }

    /// <summary>
    /// 録画マトリクスボタンリスト
    /// </summary>
    public ObservableCollection<EsportsMatrixButton> RecordingMatrixButtons
    {
        get => RecordingViewModel.RecordingMatrixButtons;
        set => RecordingViewModel.RecordingMatrixButtons = value;
    }

    /// <summary>
    /// 録画マトリクスの行ラベル
    /// </summary>
    public ObservableCollection<string> RecordingMatrixRowLabels
    {
        get => RecordingViewModel.RecordingMatrixRowLabels;
        set => RecordingViewModel.RecordingMatrixRowLabels = value;
    }

    /// <summary>
    /// 録画マトリクスの列ラベル
    /// </summary>
    public ObservableCollection<string> RecordingMatrixColumnLabels
    {
        get => RecordingViewModel.RecordingMatrixColumnLabels;
        set => RecordingViewModel.RecordingMatrixColumnLabels = value;
    }

    /// <summary>
    /// スイッチャーマトリクスボタンリスト
    /// </summary>
    public ObservableCollection<EsportsMatrixButton> SwitcherMatrixButtons
    {
        get => SwitcherViewModel.SwitcherMatrixButtons;
        set => SwitcherViewModel.SwitcherMatrixButtons = value;
    }

    /// <summary>
    /// スイッチャーマトリクスの行ラベル
    /// </summary>
    public ObservableCollection<string> SwitcherMatrixRowLabels
    {
        get => SwitcherViewModel.SwitcherMatrixRowLabels;
        set => SwitcherViewModel.SwitcherMatrixRowLabels = value;
    }

    /// <summary>
    /// スイッチャーマトリクスの列ラベル
    /// </summary>
    public ObservableCollection<string> SwitcherMatrixColumnLabels
    {
        get => SwitcherViewModel.SwitcherMatrixColumnLabels;
        set => SwitcherViewModel.SwitcherMatrixColumnLabels = value;
    }

    /// <summary>
    /// スイッチャーマトリクスの列数（XAMLバインディング用）
    /// </summary>
    public int SwitcherMatrixColumnCount => SwitcherViewModel.SwitcherMatrixColumnCount;

    /// <summary>
    /// スイッチャーの現在のモード（XAMLバインディング用）
    /// </summary>
    public int SwitcherCurrentMode => SwitcherViewModel.CurrentMode;

    /// <summary>
    /// 指定された行と列のボタンを取得（XAMLバインディング用）
    /// </summary>
    public EsportsMatrixButton? GetSwitcherMatrixButton(int row, int column)
    {
        if (SwitcherViewModel != null)
        {
            return SwitcherViewModel.GetMatrixButton(row, column);
        }
        return null;
    }

    /// <summary>
    /// 選択中のesportsプリセット番号
    /// </summary>
    public int SelectedEsportsPresetNumber
    {
        get => EsportsViewModel.SelectedEsportsPresetNumber;
        set
            {
                EsportsViewModel.SelectedEsportsPresetNumber = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 選択中の照明プリセット番号
    /// </summary>
    public int SelectedLightingPresetNumber
    {
        get => LightingViewModel.SelectedLightingPresetNumber;
        set
            {
                LightingViewModel.SelectedLightingPresetNumber = value;
            OnPropertyChanged();
        }
    }

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

    /// <summary>
    /// 現在のモード名（画面表示用）
    /// </summary>
    public string CurrentModeName => ModeSettingsManager.GetModeName(CurrentMode);

    /// <summary>
    /// 現在のモードでマイクが利用可能か
    /// </summary>
    public bool MicrophoneAvailable => CurrentMode switch
    {
        1 => App.ModeConfig.Mode1MicrophoneAvailable,
        2 => App.ModeConfig.Mode2MicrophoneAvailable,
        3 => App.ModeConfig.Mode3MicrophoneAvailable,
        _ => false
    };

    /// <summary>
    /// 現在のモードでカメラが利用可能か
    /// </summary>
    public bool CameraAvailable => CurrentMode switch
    {
        1 => App.ModeConfig.Mode1CameraAvailable,
        2 => App.ModeConfig.Mode2CameraAvailable,
        3 => App.ModeConfig.Mode3CameraAvailable,
        _ => false
    };

    /// <summary>
    /// 現在のモードでスイッチャーが利用可能か
    /// </summary>
    public bool SwitcherAvailable => CurrentMode switch
    {
        1 => App.ModeConfig.Mode1SwitcherAvailable,
        2 => App.ModeConfig.Mode2SwitcherAvailable,
        3 => App.ModeConfig.Mode3SwitcherAvailable,
        _ => false
    };

    /// <summary>
    /// 現在のモードでeスポーツ映像選択を表示するか（モード3限定）
    /// </summary>
    public bool EsportsAvailable => CurrentMode == 3;

    /// <summary>
    /// 現在のモードで映像録画を表示するか（モード3限定）
    /// </summary>
    public bool RecordingAvailable => CurrentMode == 3;

    /// <summary>
    /// 現在のモードで照明制御を表示するか（モード2限定）
    /// </summary>
    public bool LightingAvailable => CurrentMode == 2;

    /// <summary>
    /// 現在のモードでキャプチャを表示するか（モード2限定）
    /// </summary>
    public bool CaptureAvailable => CurrentMode == 2;

    /// <summary>
    /// 現在のモードで使用するコンテンツ（マイク・カメラなど）
    /// </summary>
    public object? CurrentModeContent
    {
        get
        {
            return CurrentMode switch
            {
                1 => new object(), // マイク用
                2 => SelectedCamera ?? new object(), // カメラ用
                _ => SelectedEquipment
            };
        }
    }

        /// <summary>
        /// モードに応じた操作用機器リスト（マイクとカメラを統合）
        /// </summary>
        public ObservableCollection<EquipmentStatus> EquipmentList
        {
            get
            {
                var list = new ObservableCollection<EquipmentStatus>();
                
                // マイクリストを追加
                if (MicrophoneList != null)
                {
                    foreach (var item in MicrophoneList)
                    {
                        list.Add(item);
                    }
                }
                
                // カメラリストを追加
                if (CameraList != null)
                {
                    foreach (var item in CameraList)
                    {
                        list.Add(item);
                    }
                }
                
                return list;
            }
        }

        public EquipmentStatus? SelectedEquipment
        {
            get => _selectedEquipment;
            set
            {
                _selectedEquipment = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ExecuteCommand));
            }
        }

    public int CurrentMode
    {
        get => _currentMode;
        set 
        { 
            // モードが変更された場合
            if (_currentMode != value && _currentMode != 0)
            {
            int currentModeNumber = _currentMode;
            int newModeNumber = value;
            string currentModeName = ModeSettingsManager.GetModeName(currentModeNumber);
            string newModeName = ModeSettingsManager.GetModeName(newModeNumber);
            
            ActionLogger.LogAction("モード変更", $"モード: {currentModeName} → {newModeName}");
            
            ActionLogger.LogProcessingStart("モード変更前の設定保存", $"現在のモード: {currentModeName}");
                
                // 変更前に現在のモードの設定を保存
                ActionLogger.LogProcessing("マイクフェーダー値の保存", "マイクフェーダー値を保存中");
                if (MicrophoneViewModel != null)
                {
                    MicrophoneViewModel.SaveCurrentFaderValuesToEquipmentSettings();
                    MicrophoneViewModel.SaveCurrentOutputFaderValuesToEquipmentSettings();
                    MicrophoneViewModel.SaveCurrentMuteStatesToEquipmentSettings();
                }
                
                
                ActionLogger.LogProcessing("スイッチャーマトリクスの保存", "スイッチャーマトリクス選択を保存中");
                if (SwitcherViewModel != null)
                {
                    SwitcherViewModel.SaveSwitcherMatrixSelection();
                }
                
                ActionLogger.LogProcessing("Esportsマトリクスの保存", "Esportsマトリクス選択を保存中");
                if (EsportsViewModel != null)
                {
                    EsportsViewModel.SaveEsportsCurrentSelection();
                }
                
                // オレンジ状態（選択されているが呼び出されていない）のプリセットを濃い青（呼び出し済み）に戻す
                ActionLogger.LogProcessing("プリセット状態の更新", "選択中のプリセットを呼び出し済み状態に更新");
                ResetPresetSelectionStates();
                
                ActionLogger.LogProcessingComplete("モード変更前の設定保存");
            }
            
            bool modeChanged = _currentMode != value;
            _currentMode = value; 
            
            if (modeChanged)
            {
                string modeName = ModeSettingsManager.GetModeName(value);
                ActionLogger.LogProcessingStart("モード変更処理", $"新しいモード: {modeName}");
                
                ActionLogger.LogProcessing("各ViewModelのモード更新", "すべてのViewModelのモードを更新中");
                // 各ViewModelのモードを更新（モード番号で設定）
                if (MicrophoneViewModel != null) MicrophoneViewModel.CurrentMode = value;
                if (CameraViewModel != null) CameraViewModel.CurrentMode = value;
                if (SwitcherViewModel != null) SwitcherViewModel.CurrentMode = value;
                if (EsportsViewModel != null) EsportsViewModel.CurrentMode = value;
                if (RecordingViewModel != null) RecordingViewModel.CurrentMode = value;
                if (LightingViewModel != null) LightingViewModel.CurrentMode = value;
                
                ActionLogger.LogProcessing("UI更新", "プロパティ変更を通知");
            OnPropertyChanged();
            OnPropertyChanged(nameof(EquipmentList));
            OnPropertyChanged(nameof(CurrentModeContent));
            OnPropertyChanged(nameof(EsportsAvailable));
            OnPropertyChanged(nameof(RecordingAvailable));
            OnPropertyChanged(nameof(LightingAvailable));
            OnPropertyChanged(nameof(MicrophoneAvailable));
            OnPropertyChanged(nameof(CameraAvailable));
            OnPropertyChanged(nameof(SwitcherAvailable));
            
                UpdateMonitoringFilter();
            
                // 各ViewModelのプロパティ変更を通知
                OnPropertyChanged(nameof(MicrophoneFaderValues));
                OnPropertyChanged(nameof(OutputFaderValues));
                OnPropertyChanged(nameof(CameraPresets));
                OnPropertyChanged(nameof(EsportsMatrixButtons));
                OnPropertyChanged(nameof(EsportsMatrixRowLabels));
                OnPropertyChanged(nameof(EsportsMatrixColumnLabels));
                OnPropertyChanged(nameof(RecordingMatrixButtons));
                OnPropertyChanged(nameof(RecordingMatrixRowLabels));
                OnPropertyChanged(nameof(RecordingMatrixColumnLabels));
                OnPropertyChanged(nameof(SwitcherMatrixButtons));
                OnPropertyChanged(nameof(SwitcherMatrixRowLabels));
                OnPropertyChanged(nameof(SwitcherMatrixColumnLabels));
                
                ActionLogger.LogResult("モード変更完了", $"モードが {modeName} に変更されました");
                ActionLogger.LogProcessingComplete("モード変更処理");
            }
        }
    }


    /// <summary>
    /// 選択されたカメラ
    /// </summary>
    public EquipmentStatus? SelectedCamera
    {
        get
        {
            if (CameraViewModel != null)
            {
                return CameraViewModel.SelectedCamera;
            }
            return null;
        }
        set
        {
            if (CameraViewModel != null)
            {
                CameraViewModel.SelectedCamera = value;
            }
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentModeContent));
            OnPropertyChanged(nameof(IsCamera1Selected));
        }
    }

    /// <summary>
    /// カメラ1が選択されているかどうか（カメラ1のみAI対応）
    /// </summary>
    public bool IsCamera1Selected
    {
        get => CameraViewModel.IsCamera1Selected;
    }

    /// <summary>
    /// 選択中のAIスチル番号
    /// </summary>
    public int SelectedAiStillNumber
    {
        get => CameraViewModel.SelectedAiStillNumber;
        set
            {
                CameraViewModel.SelectedAiStillNumber = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// トラッキングがオンかどうか
    /// </summary>
    public bool IsTrackingOn
    {
        get => CameraViewModel.IsTrackingOn;
        set
            {
                CameraViewModel.IsTrackingOn = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 選択中のスイッチャープリセット番号
    /// </summary>
    public int SelectedSwitcherPresetNumber
    {
        get => SwitcherViewModel.SelectedSwitcherPresetNumber;
        set
            {
                SwitcherViewModel.SelectedSwitcherPresetNumber = value;
            OnPropertyChanged();
        }
    }

    public ICommand ExecuteCommand { get; private set; } = null!;
    public ICommand ReturnToMenuCommand { get; private set; } = null!;
    public ICommand SelectEquipmentCategoryCommand { get; private set; } = null!;
    
    public event Action? ReturnToMenu;

    /// <summary>
    /// 選択中の機器カテゴリ
    /// </summary>
    public string SelectedEquipmentCategory
    {
        get => _selectedEquipmentCategory;
        set
        {
            _selectedEquipmentCategory = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 選択中のプリセット番号
    /// </summary>
    public int SelectedPresetNumber
    {
        get => CameraViewModel.SelectedPresetNumber;
        set
            {
                CameraViewModel.SelectedPresetNumber = value;
            OnPropertyChanged();
        }
    }

        public ModeControlViewModel(string mode, 
            ObservableCollection<EquipmentStatus> microphoneList,
            ObservableCollection<EquipmentStatus> cameraList,
            ObservableCollection<EquipmentStatus> switcherList,
            ObservableCollection<EquipmentStatus> monitoringList)
        {
            // 1. サービスの初期化
            _controlService = new EquipmentControlService();

            // 2. 機器リストの設定
            MicrophoneList = microphoneList;
            CameraList = cameraList;
            SwitcherList = switcherList;
            MonitoringList = monitoringList;

            _monitoringView = new ListCollectionView(MonitoringList);
            UpdateMonitoringFilter();

            // 3. 各機材ViewModelのインスタンスを作成（モード名からモード番号に変換）
            int modeNumber = ModeSettingsManager.GetModeNumber(mode);
            MicrophoneViewModel = new MicrophoneViewModel(mode, microphoneList);
            CameraViewModel = new CameraViewModel(mode, cameraList);
            SwitcherViewModel = new SwitcherViewModel(mode, switcherList);
            EsportsViewModel = new EsportsViewModel(mode);
            RecordingViewModel = new RecordingViewModel(mode);
            LightingViewModel = new LightingViewModel(mode);
            CaptureViewModel = new CaptureViewModel(mode);
            
            // 各ViewModelのモードをモード番号で設定
            if (modeNumber > 0)
            {
                MicrophoneViewModel.SetCurrentModeFromName(mode);
                CameraViewModel.SetCurrentModeFromName(mode);
                SwitcherViewModel.SetCurrentModeFromName(mode);
                EsportsViewModel.SetCurrentModeFromName(mode);
                RecordingViewModel.SetCurrentModeFromName(mode);
                LightingViewModel.SetCurrentModeFromName(mode);
                CaptureViewModel.SetCurrentModeFromName(mode);
            }

            // 4. 起動時に全モードの設定を読み込む（モードごとに値を保持するため）
            LoadAllModeSettings();

            // 5. モードの設定（モード名からモード番号に変換して設定）
            CurrentMode = ModeSettingsManager.GetModeNumber(mode);
            
            // 現在のモードのプリセット選択状態を復元
            RestorePresetSelectionForCurrentMode();
            
            UpdateMonitoringFilter();

            // 6. UI初期状態の設定
            OperationHistory = new ObservableCollection<string>();

            // 7. コマンドの初期化
            InitializeCommands();

            _monitoringTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            _monitoringTimer.Tick += MonitoringTimer_Tick;
            _monitoringTimer.Start();
            _ = UpdateMonitoringStatusAsync();

        }

        /// <summary>
        /// 起動時に全モードの設定を読み込む（モードごとに値を保持するため）
        /// </summary>
        private void LoadAllModeSettings()
        {
            ActionLogger.LogProcessingStart("全モード設定の読み込み", "起動時に全モードの設定を読み込み中");
            
            var allModes = new[] { App.ModeConfig.Mode1Name, App.ModeConfig.Mode2Name, App.ModeConfig.Mode3Name };
            
            foreach (var modeName in allModes)
            {
                if (string.IsNullOrEmpty(modeName)) continue;
                
                ActionLogger.LogProcessing("モード設定の読み込み", $"モード: {modeName}");
                
                int modeNumber = ModeSettingsManager.GetModeNumber(modeName);
                if (modeNumber == 0) continue;
                
                if (MicrophoneViewModel != null)
                {
                    MicrophoneViewModel.LoadModeSettingsForModeNumber(modeNumber);
                }
                if (CameraViewModel != null)
                {
                    CameraViewModel.LoadModeSettingsForModeNumber(modeNumber);
                }
                if (SwitcherViewModel != null)
                {
                    SwitcherViewModel.LoadModeSettingsForModeNumber(modeNumber);
                }
                if (EsportsViewModel != null)
                {
                    EsportsViewModel.LoadModeSettingsForModeNumber(modeNumber);
                }
                if (CaptureViewModel != null)
                {
                    CaptureViewModel.LoadModeSettingsForModeNumber(modeNumber);
                }
            }
            
            ActionLogger.LogResult("全モード設定の読み込み完了", $"全{allModes.Length}モードの設定を読み込みました");
            ActionLogger.LogProcessingComplete("全モード設定の読み込み");
        }

        /// <summary>
        /// 現在のモードのプリセット選択状態を復元
        /// </summary>
        private void RestorePresetSelectionForCurrentMode()
        {
            if (CurrentMode == 0) return;

            try
            {
                var settings = ModeSettingsManager.LoadModeSettings(CurrentMode);
                
                // 各ViewModelのプリセット選択状態を復元
                if (SwitcherViewModel != null)
                {
                    SwitcherViewModel.SelectedSwitcherPresetNumber = settings.SelectedSwitcherPresetNumber;
                }
                if (LightingViewModel != null)
                {
                    LightingViewModel.SelectedLightingPresetNumber = settings.SelectedLightingPresetNumber;
                }
                if (EsportsViewModel != null)
                {
                    EsportsViewModel.SelectedEsportsPresetNumber = settings.SelectedEsportsPresetNumber;
                }
            }
            catch (Exception ex)
            {
                ActionLogger.LogError("プリセット選択状態復元", $"プリセット選択状態の復元に失敗しました: {ex.Message}");
            }
        }

        private async void MonitoringTimer_Tick(object? sender, EventArgs e)
        {
            await UpdateMonitoringStatusAsync();
        }

        private async Task UpdateMonitoringStatusAsync()
        {
            if (MonitoringList == null || MonitoringList.Count == 0)
            {
                return;
            }

            var targets = _monitoringView.Cast<EquipmentStatus>().Where(FilterByModeDisplayFlag).ToList();
            if (targets.Count == 0)
            {
                return;
            }

            var updates = await _controlService.CheckEquipmentStatusAsync(targets);
            foreach (var update in updates)
            {
                update.Apply();
            }
        }

        private void UpdateMonitoringFilter()
        {
            _monitoringView.Filter = item =>
            {
                if (item is not EquipmentStatus equipment)
                {
                    return false;
                }

                return FilterByModeDisplayFlag(equipment);
            };

            _monitoringView.Refresh();
            OnPropertyChanged(nameof(FilteredMonitoringView));
        }

        private bool FilterByModeDisplayFlag(EquipmentStatus equipment)
        {
            return equipment.ModeDisplayFlag switch
            {
                0 => true,
                1 => CurrentMode == 3,
                _ => true
            };
        }

        /// <summary>
        /// コマンドの初期化
        /// </summary>
        private void InitializeCommands()
        {
            ExecuteCommand = new RelayCommand(ExecuteControl, () => SelectedEquipment != null);
            ReturnToMenuCommand = new RelayCommand(SaveAndReturnToMenu);
            SelectEquipmentCategoryCommand = new RelayCommand<string>(SelectEquipmentCategory);
        }

        /// <summary>
        /// メニューに戻る前に設定を保存
        /// </summary>
        private void SaveAndReturnToMenu()
        {
            // 確認ダイアログを表示
            var result = CustomDialog.Show(
                "メニューに戻りますか？\n現在の設定は保存されます。",
                "メニューに戻る確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                ActionLogger.LogProcessing("ユーザー確認", "メニューに戻る操作がキャンセルされました");
                return;
            }

            string modeName = CurrentModeName;
            ActionLogger.LogAction("メニューに戻る", $"現在のモード: {modeName}");
            
            ActionLogger.LogProcessingStart("設定保存", "すべての設定を保存中");
            
            ActionLogger.LogProcessing("マイクフェーダー値の保存", "マイクフェーダー値を保存中");
            if (MicrophoneViewModel != null)
            {
                MicrophoneViewModel.SaveCurrentFaderValuesToEquipmentSettings();
                MicrophoneViewModel.SaveCurrentOutputFaderValuesToEquipmentSettings();
                MicrophoneViewModel.SaveCurrentMuteStatesToEquipmentSettings();
            }
            
            
            ActionLogger.LogProcessing("スイッチャーマトリクスの保存", "スイッチャーマトリクス選択を保存中");
            if (SwitcherViewModel != null)
            {
                SwitcherViewModel.SaveSwitcherMatrixSelection();
            }
            
            ActionLogger.LogProcessing("Esportsマトリクスの保存", "Esportsマトリクス選択を保存中");
            if (EsportsViewModel != null)
            {
                EsportsViewModel.SaveEsportsCurrentSelection();
            }
            
            // オレンジ状態（選択されているが呼び出されていない）のプリセットを濃い青（呼び出し済み）に戻す
            ActionLogger.LogProcessing("プリセット状態の更新", "選択中のプリセットを呼び出し済み状態に更新");
            ResetPresetSelectionStates();
            
            // 現在のモードの設定をJSONファイルに保存
            ActionLogger.LogProcessing("設定ファイルの保存", $"モード「{modeName}」の設定をJSONファイルに保存中");
            SaveCurrentModeSettingsToJson();
            
            ActionLogger.LogResult("設定保存完了", "すべての設定を保存しました");
            ActionLogger.LogProcessingComplete("設定保存");
            
            // メニューに戻るイベントを呼び出す
            ReturnToMenu?.Invoke();
        }

        /// <summary>
        /// 現在のモードの設定をJSONファイルに保存
        /// </summary>
        private void SaveCurrentModeSettingsToJson()
        {
            if (CurrentMode == 0) return;

            try
            {
                // MicrophoneViewModelから設定データを取得
                ModeSettingsData? settings = null;
                if (MicrophoneViewModel != null)
                {
                    settings = MicrophoneViewModel.GetModeSettingsData(CurrentMode);
                }
                
                if (settings == null)
                {
                    // 設定データが存在しない場合は新規作成
                    settings = new ModeSettingsData();
                    // マイクと出力のフェーダー値とミュート状態を現在の値で初期化
                    // マイクは4個のみなので、最初の4個だけを保存
                    if (MicrophoneViewModel != null)
                    {
                        // マイクフェーダー値（4個のみ）
                        if (MicrophoneViewModel.MicrophoneFaderValues.Count >= 4)
                        {
                            settings.MicrophoneFaderValues = new double[4]
                            {
                                MicrophoneViewModel.MicrophoneFaderValues[0],
                                MicrophoneViewModel.MicrophoneFaderValues[1],
                                MicrophoneViewModel.MicrophoneFaderValues[2],
                                MicrophoneViewModel.MicrophoneFaderValues[3]
                            };
                        }
                        else
                        {
                            var values = new double[4];
                            for (int i = 0; i < 4; i++)
                            {
                                values[i] = i < MicrophoneViewModel.MicrophoneFaderValues.Count 
                                    ? MicrophoneViewModel.MicrophoneFaderValues[i] : 50.0;
                            }
                            settings.MicrophoneFaderValues = values;
                        }
                        
                        // 出力フェーダー値（2個のみ）
                        if (MicrophoneViewModel.OutputFaderValues.Count >= 2)
                        {
                            settings.OutputFaderValues = new double[2]
                            {
                                MicrophoneViewModel.OutputFaderValues[0],
                                MicrophoneViewModel.OutputFaderValues[1]
                            };
                        }
                        else
                        {
                            var values = new double[2];
                            for (int i = 0; i < 2; i++)
                            {
                                values[i] = i < MicrophoneViewModel.OutputFaderValues.Count 
                                    ? MicrophoneViewModel.OutputFaderValues[i] : 50.0;
                            }
                            settings.OutputFaderValues = values;
                        }
                        
                        // 出力ミュート状態（2個のみ）
                        if (MicrophoneViewModel.OutputMuteStates.Count >= 2)
                        {
                            settings.OutputMuteStates = new bool[2]
                            {
                                MicrophoneViewModel.OutputMuteStates[0],
                                MicrophoneViewModel.OutputMuteStates[1]
                            };
                        }
                        else
                        {
                            settings.OutputMuteStates = new bool[2] { false, false };
                        }
                        
                        // マイクミュート状態（4個のみ）
                        if (MicrophoneViewModel.MicrophoneMuteStates.Count >= 4)
                        {
                            settings.MicrophoneMuteStates = new bool[4]
                            {
                                MicrophoneViewModel.MicrophoneMuteStates[0],
                                MicrophoneViewModel.MicrophoneMuteStates[1],
                                MicrophoneViewModel.MicrophoneMuteStates[2],
                                MicrophoneViewModel.MicrophoneMuteStates[3]
                            };
                        }
                        else
                        {
                            settings.MicrophoneMuteStates = new bool[4] { false, false, false, false };
                        }
                    }
                }
                else
                {
                    // 設定データが存在する場合でも、最新のフェーダー値とミュート状態を反映
                    // マイクは4個のみ、出力は2個のみなので、最初の数個だけを保存
                    if (MicrophoneViewModel != null)
                    {
                        // マイクフェーダー値（4個のみ）
                        if (MicrophoneViewModel.MicrophoneFaderValues.Count >= 4)
                        {
                            settings.MicrophoneFaderValues = new double[4]
                            {
                                MicrophoneViewModel.MicrophoneFaderValues[0],
                                MicrophoneViewModel.MicrophoneFaderValues[1],
                                MicrophoneViewModel.MicrophoneFaderValues[2],
                                MicrophoneViewModel.MicrophoneFaderValues[3]
                            };
                        }
                        else
                        {
                            var values = new double[4];
                            for (int i = 0; i < 4; i++)
                            {
                                values[i] = i < MicrophoneViewModel.MicrophoneFaderValues.Count 
                                    ? MicrophoneViewModel.MicrophoneFaderValues[i] : 50.0;
                            }
                            settings.MicrophoneFaderValues = values;
                        }
                        
                        // 出力フェーダー値（2個のみ）
                        if (MicrophoneViewModel.OutputFaderValues.Count >= 2)
                        {
                            settings.OutputFaderValues = new double[2]
                            {
                                MicrophoneViewModel.OutputFaderValues[0],
                                MicrophoneViewModel.OutputFaderValues[1]
                            };
                        }
                        else
                        {
                            var values = new double[2];
                            for (int i = 0; i < 2; i++)
                            {
                                values[i] = i < MicrophoneViewModel.OutputFaderValues.Count 
                                    ? MicrophoneViewModel.OutputFaderValues[i] : 50.0;
                            }
                            settings.OutputFaderValues = values;
                        }
                        
                        // 出力ミュート状態（2個のみ）
                        if (MicrophoneViewModel.OutputMuteStates.Count >= 2)
                        {
                            settings.OutputMuteStates = new bool[2]
                            {
                                MicrophoneViewModel.OutputMuteStates[0],
                                MicrophoneViewModel.OutputMuteStates[1]
                            };
                        }
                        else
                        {
                            settings.OutputMuteStates = new bool[2] { false, false };
                        }
                        
                        // マイクミュート状態（4個のみ）
                        if (MicrophoneViewModel.MicrophoneMuteStates.Count >= 4)
                        {
                            settings.MicrophoneMuteStates = new bool[4]
                            {
                                MicrophoneViewModel.MicrophoneMuteStates[0],
                                MicrophoneViewModel.MicrophoneMuteStates[1],
                                MicrophoneViewModel.MicrophoneMuteStates[2],
                                MicrophoneViewModel.MicrophoneMuteStates[3]
                            };
                        }
                        else
                        {
                            settings.MicrophoneMuteStates = new bool[4] { false, false, false, false };
                        }
                    }
                }

                // プリセット選択状態を保存
                if (SwitcherViewModel != null)
                {
                    settings.SelectedSwitcherPresetNumber = SwitcherViewModel.SelectedSwitcherPresetNumber;
                }
                if (LightingViewModel != null)
                {
                    settings.SelectedLightingPresetNumber = LightingViewModel.SelectedLightingPresetNumber;
                }
                if (EsportsViewModel != null)
                {
                    settings.SelectedEsportsPresetNumber = EsportsViewModel.SelectedEsportsPresetNumber;
                }

                ModeSettingsManager.SaveModeSettings(CurrentMode, settings);
            }
            catch (Exception ex)
            {
                ActionLogger.LogError("設定ファイル保存", $"設定ファイルの保存に失敗しました: {ex.Message}");
            }
        }


        private void ExecuteControl()
        {
            if (SelectedEquipment == null)
            {
                ActionLogger.LogError("機材制御実行", "機器が選択されていません");
                return;
            }

            string modeName = ModeSettingsManager.GetModeName(CurrentMode);
            ActionLogger.LogAction("機材制御実行", $"モード: {modeName}, 対象機器: {SelectedEquipment.Name}");

            var result = CustomDialog.Show(
                $"以下の設定で機材制御を実行しますか？\n\n" +
                $"モード: {modeName}\n" +
                $"対象機器: {SelectedEquipment.Name}\n" +
                $"続行しますか？",
                "実行確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                ActionLogger.LogProcessing("ユーザー確認", "実行がキャンセルされました");
                return;
            }

            ActionLogger.LogProcessingStart("機材制御処理", $"モード: {modeName}, 機器: {SelectedEquipment.Name}");
            ActionLogger.LogProcessing("機材制御サービスの実行", "ExecuteModeを呼び出し");
            
            _controlService.ExecuteMode(CurrentMode);

            var timestamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            var historyMessage = $"[{timestamp}] {modeName} - {SelectedEquipment!.Name}を実行";

            OperationHistory.Insert(0, historyMessage);
            
            if (OperationHistory.Count > 100)
            {
                OperationHistory.RemoveAt(OperationHistory.Count - 1);
            }

            ActionLogger.LogResult("機材制御実行完了", $"モード: {modeName}, 機器: {SelectedEquipment.Name}");
            ActionLogger.LogProcessingComplete("機材制御処理");

            CustomDialog.Show(
                $"機材制御を実行しました。\n\n" +
                $"モード: {modeName}\n" +
                $"対象機器: {SelectedEquipment.Name}\n" +
                $"実行時刻: {timestamp}",
                "制御実行完了",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }


        /// <summary>
        /// 機器カテゴリを選択
        /// </summary>
        private void SelectEquipmentCategory(string? category)
        {
            if (string.IsNullOrEmpty(category))
            {
                ActionLogger.LogError("機器カテゴリ選択", "カテゴリが指定されていません");
                return;
            }

            // オレンジ状態（選択されているが呼び出されていない）のプリセットを濃い青（呼び出し済み）に戻す
            ResetPresetSelectionStates();

            var previousCategory = SelectedEquipmentCategory;
            ActionLogger.LogAction("機器カテゴリ選択", $"カテゴリ: {previousCategory} → {category}");
            
            SelectedEquipmentCategory = category;
            
            ActionLogger.LogResult("機器カテゴリ選択完了", $"カテゴリが {category} に変更されました");
        }





        /// <summary>
        /// 仮選択状態（選択されているが呼び出されていない）のプリセットをクリア
        /// </summary>
        private void ResetPresetSelectionStates()
        {
            // スイッチャープリセット：選択状態をクリア（呼び出し状態は保持）
            if (SwitcherViewModel != null && 
                SwitcherViewModel.SelectedSwitcherPresetNumber > 0 && 
                SwitcherViewModel.CalledSwitcherPresetNumber != SwitcherViewModel.SelectedSwitcherPresetNumber)
            {
                SwitcherViewModel.ClearSwitcherPresetSelectionOnly();
            }

            // カメラプリセット：選択状態をクリア（呼び出し状態は保持）
            if (CameraViewModel != null && 
                CameraViewModel.SelectedPresetNumber > 0 && 
                CameraViewModel.CalledPresetNumber != CameraViewModel.SelectedPresetNumber)
            {
                CameraViewModel.ClearPresetSelectionOnly();
            }

            // Esportsプリセット：選択状態をクリア（呼び出し状態は保持）
            if (EsportsViewModel != null && 
                EsportsViewModel.SelectedEsportsPresetNumber > 0 && 
                EsportsViewModel.CalledEsportsPresetNumber != EsportsViewModel.SelectedEsportsPresetNumber)
            {
                EsportsViewModel.ClearEsportsPresetSelectionOnly();
            }

            // 照明プリセット：選択状態をクリア（呼び出し状態は保持）
            if (LightingViewModel != null && 
                LightingViewModel.SelectedLightingPresetNumber > 0 && 
                LightingViewModel.CalledLightingPresetNumber != LightingViewModel.SelectedLightingPresetNumber)
            {
                LightingViewModel.ClearLightingPresetSelectionOnly();
            }
        }

        public void Cleanup()
        {
            // 現在のモードの設定を保存
            if (_currentMode != 0)
            {
                if (MicrophoneViewModel != null)
                {
                    MicrophoneViewModel.SaveCurrentFaderValuesToEquipmentSettings();
                    MicrophoneViewModel.SaveCurrentOutputFaderValuesToEquipmentSettings();
                }
                if (EsportsViewModel != null)
                {
                    EsportsViewModel.SaveEsportsCurrentSelection();
                }
                if (SwitcherViewModel != null)
                {
                    SwitcherViewModel.SaveSwitcherMatrixSelection();
                }
            }
            
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

