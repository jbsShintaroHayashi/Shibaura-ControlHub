using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Shibaura_ControlHub.Models;
using Shibaura_ControlHub;
using Shibaura_ControlHub.Utils;
using Shibaura_ControlHub.Views;

namespace Shibaura_ControlHub.ViewModels
{
    /// <summary>
    /// キャプチャ制御のViewModel
    /// </summary>
    public class CaptureViewModel : BaseViewModel
    {
        private readonly Dictionary<int, ModeSettingsData> _allModeSettings = new Dictionary<int, ModeSettingsData>();
        private ModeSettingsData? _modeSettingsData;

        /// <summary>
        /// キャプチャマトリクスボタンリスト（1行×11列 = 11個）
        /// </summary>
        public ObservableCollection<EsportsMatrixButton> CaptureMatrixButtons { get; set; } = new ObservableCollection<EsportsMatrixButton>();

        /// <summary>
        /// キャプチャマトリクスの行ラベル（1行分）
        /// </summary>
        public ObservableCollection<string> CaptureMatrixRowLabels { get; set; } = new ObservableCollection<string>();

        /// <summary>
        /// キャプチャマトリクスの列ラベル（11列分）
        /// </summary>
        public ObservableCollection<string> CaptureMatrixColumnLabels { get; set; } = new ObservableCollection<string>();

        // Commands
        public ICommand SelectCaptureMatrixCommand { get; private set; } = null!;

        public CaptureViewModel(string mode)
        {
            SetCurrentModeFromName(mode);
            
            LoadModeSettings();
            InitializeCaptureMatrix();
            InitializeCommands();
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
            
            LoadCaptureSelectionForCurrentMode();
            
            OnPropertyChanged(nameof(CaptureMatrixButtons));
            OnPropertyChanged(nameof(CaptureMatrixRowLabels));
            OnPropertyChanged(nameof(CaptureMatrixColumnLabels));
        }

        /// <summary>
        /// コマンドの初期化
        /// </summary>
        private void InitializeCommands()
        {
            SelectCaptureMatrixCommand = new RelayCommand<EsportsMatrixButton>(SelectCaptureMatrix);
        }

        /// <summary>
        /// モード設定を読み込む（起動時のみJSONから読み込む）
        /// </summary>
        private void LoadModeSettings()
        {
            LoadModeSettings(ModeSettingsManager.GetModeName(CurrentMode));
        }

        /// <summary>
        /// 指定されたモードの設定を読み込む（起動時のみJSONから読み込む）
        /// </summary>
        /// <param name="mode">モード名（"授業"、"遠隔"、"e-sports"など）</param>
        private void LoadModeSettings(string mode)
        {
            int modeNumber = ModeSettingsManager.GetModeNumber(mode);
            LoadModeSettingsForModeNumber(modeNumber);
        }

        /// <summary>
        /// 指定されたモード番号の設定を読み込む（起動時のみJSONから読み込む）
        /// </summary>
        /// <param name="modeNumber">モード番号（1、2、3）</param>
        internal void LoadModeSettingsForModeNumber(int modeNumber)
        {
            if (modeNumber == 0) return;
            
            if (!_allModeSettings.ContainsKey(modeNumber))
            {
                var settings = ModeSettingsManager.LoadModeSettings(modeNumber);
                
                if (settings.CaptureSelections == null)
                {
                    settings.CaptureSelections = new Dictionary<int, int>();
                }
                
                _allModeSettings[modeNumber] = settings;
            }
            
            if (modeNumber == CurrentMode)
            {
                _modeSettingsData = _allModeSettings[modeNumber];
            }
        }

        /// <summary>
        /// キャプチャマトリクスを初期化
        /// </summary>
        private void InitializeCaptureMatrix()
        {
            string modeName = CurrentModeName;
            ActionLogger.LogProcessingStart("キャプチャマトリクス初期化", $"モード: {modeName}");
            
            CaptureMatrixButtons.Clear();
            CaptureMatrixRowLabels.Clear();
            CaptureMatrixColumnLabels.Clear();
            
            // 行ラベルを初期化（1行のみ）
            CaptureMatrixRowLabels.Add("キャプチャ");
            
            // 列ラベルを初期化（スイッチャーAの遠隔モードと同じ11列）
            CaptureMatrixColumnLabels.Add("PGM");
            CaptureMatrixColumnLabels.Add("CLN");
            CaptureMatrixColumnLabels.Add("Cam1");
            CaptureMatrixColumnLabels.Add("Cam2");
            CaptureMatrixColumnLabels.Add("Cam3");
            CaptureMatrixColumnLabels.Add("HCam1");
            CaptureMatrixColumnLabels.Add("HCam2");
            CaptureMatrixColumnLabels.Add("講師");
            CaptureMatrixColumnLabels.Add("PcA");
            CaptureMatrixColumnLabels.Add("PcB");
            CaptureMatrixColumnLabels.Add("オフ");

            // 1行×11列のマトリクスを生成
            ActionLogger.LogProcessing("マトリクス生成", "1行×11列のマトリクスを生成中");
            for (int row = 1; row <= 1; row++)
            {
                for (int column = 1; column <= 11; column++)
                {
                    CaptureMatrixButtons.Add(new EsportsMatrixButton
                    {
                        Row = row,
                        Column = column,
                        DisplayText = "",
                        IsSelected = false
                    });
                }
            }
            
            // 保存された選択状態を読み込み
            ActionLogger.LogProcessing("保存された選択状態の読み込み", "選択状態を読み込み中");
            LoadCaptureSelectionForCurrentMode();
            
            ActionLogger.LogResult("キャプチャマトリクス初期化完了", $"行数: 1, 列数: 11");
            ActionLogger.LogProcessingComplete("キャプチャマトリクス初期化");
        }

        /// <summary>
        /// キャプチャマトリクスのセルを選択/選択解除
        /// </summary>
        private void SelectCaptureMatrix(EsportsMatrixButton? button)
        {
            if (button == null)
            {
                ActionLogger.LogError("キャプチャマトリクス選択", "ボタンがnullです");
                return;
            }

            ActionLogger.LogAction("キャプチャマトリクス選択", $"行: {button.Row}, 列: {button.Column}");
            
            ActionLogger.LogProcessingStart("マトリクス選択処理", $"行{button.Row}の他のボタンを未選択に");

            // 同じ行の他のボタンを全て未選択にする
            foreach (var b in CaptureMatrixButtons.Where(b => b.Row == button.Row && b != button))
            {
                b.IsSelected = false;
            }

            // 選択状態にする（トグルしない）
            button.IsSelected = true;
            
            ActionLogger.LogProcessing("マトリクス選択状態更新", $"行: {button.Row}, 列: {button.Column}, 選択状態: {button.IsSelected}");

            // 現在の選択状態を保存
            ActionLogger.LogProcessing("マトリクス選択状態の保存", "選択状態を保存中");
            SaveCaptureCurrentSelection();
            
            ActionLogger.LogResult("マトリクス選択完了", $"行: {button.Row}, 列: {button.Column}, 選択状態: {button.IsSelected}");
            ActionLogger.LogProcessingComplete("マトリクス選択処理");
        }

        /// <summary>
        /// 現在のキャプチャ選択状態を保存（行ごとの選択形式）
        /// </summary>
        public void SaveCaptureCurrentSelection()
        {
            if (_modeSettingsData == null || CurrentMode == 0)
            {
                ActionLogger.LogError("キャプチャマトリクス保存", "設定データまたはモードが無効です");
                return;
            }

            // 行ごとの選択を保存（行 -> 列）
            _modeSettingsData.CaptureSelections.Clear();
            var selectedButtons = CaptureMatrixButtons
                .Where(b => b.IsSelected)
                .ToList();

            foreach (var button in selectedButtons)
            {
                _modeSettingsData.CaptureSelections[button.Row] = button.Column;
            }

            string modeName = CurrentModeName;
            ActionLogger.LogProcessing("キャプチャマトリクス選択状態の保存", $"モード: {modeName}, 選択行数: {_modeSettingsData.CaptureSelections.Count}");

            // JSONファイルに即座に保存
            ModeSettingsManager.SaveModeSettings(CurrentMode, _modeSettingsData);
        }

        /// <summary>
        /// 現在のモードのキャプチャ選択状態を読み込む（行ごとの選択形式、メモリ上のデータを使用）
        /// </summary>
        private void LoadCaptureSelectionForCurrentMode()
        {
            // メモリ上のデータを使用（JSONから読み込まない）
            if (_modeSettingsData == null || 
                _modeSettingsData.CaptureSelections == null || 
                CurrentMode == 0)
            {
                ActionLogger.LogProcessing("キャプチャマトリクス選択状態の読み込み", "保存された選択状態がありません");
                return;
            }

            string modeName = CurrentModeName;
            ActionLogger.LogProcessingStart("キャプチャマトリクス選択状態の読み込み", $"モード: {modeName}");

            // 全てのボタンを未選択にする
            foreach (var button in CaptureMatrixButtons)
            {
                button.IsSelected = false;
            }

            ActionLogger.LogProcessing("選択状態の復元", $"復元する行数: {_modeSettingsData.CaptureSelections.Count}");
            
            // 保存されている選択状態を反映（行ごとの選択）
            foreach (var (row, column) in _modeSettingsData.CaptureSelections)
            {
                var button = CaptureMatrixButtons.FirstOrDefault(b => b.Row == row && b.Column == column);
                if (button != null)
                {
                    button.IsSelected = true;
                }
            }
            
            ActionLogger.LogResult("キャプチャマトリクス選択状態の読み込み完了", $"選択行数: {_modeSettingsData.CaptureSelections.Count}");
            ActionLogger.LogProcessingComplete("キャプチャマトリクス選択状態の読み込み");
        }

    }
}

