using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Shibaura_ControlHub.Models;
using Shibaura_ControlHub.Utils;

namespace Shibaura_ControlHub.ViewModels
{
    /// <summary>
    /// 録画制御のViewModel
    /// </summary>
    public class RecordingViewModel : BaseViewModel
    {
        private readonly Dictionary<int, ModeSettingsData> _allModeSettings = new Dictionary<int, ModeSettingsData>();
        private ModeSettingsData? _modeSettingsData;


        /// <summary>
        /// 録画マトリクスボタンリスト（2行×9列 = 18個）
        /// </summary>
        public ObservableCollection<EsportsMatrixButton> RecordingMatrixButtons { get; set; } = new ObservableCollection<EsportsMatrixButton>();

        /// <summary>
        /// 録画マトリクスの行ラベル（2行分）
        /// </summary>
        public ObservableCollection<string> RecordingMatrixRowLabels { get; set; } = new ObservableCollection<string>();

        /// <summary>
        /// 録画マトリクスの列ラベル（9列分）
        /// </summary>
        public ObservableCollection<string> RecordingMatrixColumnLabels { get; set; } = new ObservableCollection<string>();

        // Commands
        public ICommand SelectRecordingMatrixCommand { get; private set; } = null!;

        public RecordingViewModel(string mode)
        {
            SetCurrentModeFromName(mode);
            
            LoadModeSettings();
            InitializeRecordingMatrix();
            InitializeCommands();
        }

        protected override void OnModeChanged()
        {
            base.OnModeChanged();
            
            int modeNumber = CurrentMode;
            if (modeNumber == 0)
            {
                return;
            }

            if (!_allModeSettings.ContainsKey(modeNumber))
            {
                LoadModeSettings(ModeSettingsManager.GetModeName(CurrentMode));
            }

            _modeSettingsData = _allModeSettings[modeNumber];
            
            InitializeRecordingMatrix();
            
            // UI更新のためプロパティ変更通知を発行
            OnPropertyChanged(nameof(RecordingMatrixButtons));
            OnPropertyChanged(nameof(RecordingMatrixRowLabels));
            OnPropertyChanged(nameof(RecordingMatrixColumnLabels));
        }

        /// <summary>
        /// コマンドの初期化
        /// </summary>
        private void InitializeCommands()
        {
            SelectRecordingMatrixCommand = new RelayCommand<EsportsMatrixButton>(SelectRecordingMatrix);
        }

        /// <summary>
        /// 録画マトリクスを初期化
        /// </summary>
        private void InitializeRecordingMatrix()
        {
            string modeName = CurrentModeName;
            ActionLogger.LogProcessingStart("録画マトリクス初期化", $"モード: {modeName}");
            
            RecordingMatrixButtons.Clear();
            RecordingMatrixRowLabels.Clear();
            RecordingMatrixColumnLabels.Clear();
            
            // 行ラベルを初期化
            RecordingMatrixRowLabels.Add("録画1");
            RecordingMatrixRowLabels.Add("録画2");

            // 列ラベルを初期化
            RecordingMatrixColumnLabels.Add("PGM");
            RecordingMatrixColumnLabels.Add("CLN");
            RecordingMatrixColumnLabels.Add("Cam 1");
            RecordingMatrixColumnLabels.Add("Cam 2");
            RecordingMatrixColumnLabels.Add("Cam 3");
            RecordingMatrixColumnLabels.Add("HCam 1");
            RecordingMatrixColumnLabels.Add("HCam 2");
            RecordingMatrixColumnLabels.Add("PC 1");
            RecordingMatrixColumnLabels.Add("PC 2");
            RecordingMatrixColumnLabels.Add("PC 3");
            RecordingMatrixColumnLabels.Add("オフ");

            // 2行×9列のマトリクスを生成
            ActionLogger.LogProcessing("マトリクス生成", "2行×11列のマトリクスを生成中");
            for (int row = 1; row <= 2; row++)
            {
                for (int column = 1; column <= 11; column++)
                {
                    RecordingMatrixButtons.Add(new EsportsMatrixButton
                    {
                        Row = row,
                        Column = column,
                        DisplayText = $"{row}-{column}",
                        IsSelected = false
                    });
                }
            }
            
            ActionLogger.LogResult("録画マトリクス初期化完了", $"行数: 2, 列数: 11");
            ActionLogger.LogProcessingComplete("録画マトリクス初期化");

            LoadRecordingMatrixSelectionForCurrentMode();
        }

        /// <summary>
        /// 録画マトリクスのセルを選択/選択解除
        /// </summary>
        private void SelectRecordingMatrix(EsportsMatrixButton? button)
        {
            if (button == null)
            {
                ActionLogger.LogError("録画マトリクス選択", "ボタンがnullです");
                return;
            }

            ActionLogger.LogAction("録画マトリクス選択", $"行: {button.Row}, 列: {button.Column}");
            
            ActionLogger.LogProcessingStart("マトリクス選択処理", $"行{button.Row}の他のボタンを未選択に");

            // 同じ行の他のボタンを全て未選択にする
            foreach (var b in RecordingMatrixButtons.Where(b => b.Row == button.Row && b != button))
            {
                b.IsSelected = false;
            }

            // 選択状態にする（トグルしない）
            button.IsSelected = true;
            
            ActionLogger.LogProcessing("マトリクス選択状態更新", $"行: {button.Row}, 列: {button.Column}, 選択状態: {button.IsSelected}");
            
            ActionLogger.LogProcessing("録画マトリクス選択状態の保存", "選択状態を保存中");
            SaveRecordingMatrixSelection();
            
            // UDP送信機能は削除されました

            ActionLogger.LogResult("マトリクス選択完了", $"行: {button.Row}, 列: {button.Column}, 選択状態: {button.IsSelected}");
            ActionLogger.LogProcessingComplete("マトリクス選択処理");
        }

        /// <summary>
        /// モード設定を読み込む（起動時のみJSONから読み込む）
        /// </summary>
        private void LoadModeSettings()
        {
            LoadModeSettings(ModeSettingsManager.GetModeName(CurrentMode));
        }

        /// <summary>
        /// 指定されたモードの設定を読み込む
        /// </summary>
        private void LoadModeSettings(string mode)
        {
            int modeNumber = ModeSettingsManager.GetModeNumber(mode);
            if (modeNumber == 0)
            {
                return;
            }

            if (!_allModeSettings.ContainsKey(modeNumber))
            {
                var settings = ModeSettingsManager.LoadModeSettings(modeNumber);
                if (settings.RecordingSelections == null)
                    settings.RecordingSelections = new Dictionary<int, int>();
                _allModeSettings[modeNumber] = settings;
            }

            if (modeNumber == CurrentMode)
            {
                _modeSettingsData = _allModeSettings[modeNumber];
            }
        }

        /// <summary>
        /// 録画マトリクスの選択状態を保存（行ごとの選択形式）
        /// </summary>
        private void SaveRecordingMatrixSelection()
        {
            if (_modeSettingsData == null || CurrentMode == 0)
            {
                ActionLogger.LogError("録画マトリクス保存", "設定データまたはモードが無効です");
                return;
            }

            _modeSettingsData.RecordingSelections.Clear();

            foreach (var button in RecordingMatrixButtons.Where(b => b.IsSelected))
            {
                _modeSettingsData.RecordingSelections[button.Row] = button.Column;
            }
            
            // JSONファイルに即座に保存
            ModeSettingsManager.SaveModeSettings(CurrentMode, _modeSettingsData);
        }

        /// <summary>
        /// 録画マトリクスの選択状態を読み込み
        /// </summary>
        private void LoadRecordingMatrixSelectionForCurrentMode()
        {
            if (_modeSettingsData == null || 
                _modeSettingsData.RecordingSelections == null || 
                CurrentMode == 0)
            {
                ActionLogger.LogProcessing("録画マトリクス選択状態の読み込み", "保存された選択状態がありません");
                return;
            }

            foreach (var button in RecordingMatrixButtons)
            {
                button.IsSelected = false;
            }

            foreach (var (row, column) in _modeSettingsData.RecordingSelections)
            {
                var button = RecordingMatrixButtons.FirstOrDefault(b => b.Row == row && b.Column == column);
                if (button != null)
                {
                    button.IsSelected = true;
                }
            }
        }

    }
}

