using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Shibaura_ControlHub.Models;
using Shibaura_ControlHub;
using Shibaura_ControlHub.Utils;
using Shibaura_ControlHub.Views;

namespace Shibaura_ControlHub.ViewModels
{
    /// <summary>
    /// Esports制御のViewModel
    /// </summary>
    public class EsportsViewModel : BaseViewModel
    {
        private readonly Dictionary<int, ModeSettingsData> _allModeSettings = new Dictionary<int, ModeSettingsData>();
        private ModeSettingsData? _modeSettingsData;
        
        
        private int _selectedEsportsPresetNumber = 0;

        /// <summary>
        /// esportsマトリクスボタンリスト（7行×18列 = 126個）
        /// </summary>
        public ObservableCollection<EsportsMatrixButton> EsportsMatrixButtons { get; set; } = new ObservableCollection<EsportsMatrixButton>();

        /// <summary>
        /// esportsマトリクスの行ラベル（7行分）
        /// </summary>
        public ObservableCollection<string> EsportsMatrixRowLabels { get; set; } = new ObservableCollection<string>();

        /// <summary>
        /// esportsマトリクスの列ラベル（18列分）
        /// </summary>
        public ObservableCollection<string> EsportsMatrixColumnLabels { get; set; } = new ObservableCollection<string>();

        /// <summary>
        /// 選択中のesportsプリセット番号
        /// </summary>
        public int SelectedEsportsPresetNumber
        {
            get => _selectedEsportsPresetNumber;
            set
            {
                _selectedEsportsPresetNumber = value;
                OnPropertyChanged();
            }
        }

        private int _calledEsportsPresetNumber = 0;

        /// <summary>
        /// 呼び出し済みのesportsプリセット番号
        /// </summary>
        public int CalledEsportsPresetNumber
        {
            get => _calledEsportsPresetNumber;
            set
            {
                _calledEsportsPresetNumber = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand SelectEsportsPresetCommand { get; private set; } = null!;
        public ICommand CallEsportsPresetCommand { get; private set; } = null!;
        public ICommand RegisterEsportsPresetCommand { get; private set; } = null!;
        public ICommand SelectEsportsMatrixCommand { get; private set; } = null!;

        public EsportsViewModel(string mode)
        {
            SetCurrentModeFromName(mode);
            
            LoadModeSettings();
            InitializeEsportsMatrix();
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
            
            // プリセット選択状態を読み込み
            if (_modeSettingsData != null)
            {
                SelectedEsportsPresetNumber = _modeSettingsData.SelectedEsportsPresetNumber;
            }
            
            LoadEsportsSelectionForCurrentMode();
            
            OnPropertyChanged(nameof(EsportsMatrixButtons));
            OnPropertyChanged(nameof(EsportsMatrixRowLabels));
            OnPropertyChanged(nameof(EsportsMatrixColumnLabels));
        }

        /// <summary>
        /// コマンドの初期化
        /// </summary>
        private void InitializeCommands()
        {
            SelectEsportsPresetCommand = new RelayCommand<int>(SelectEsportsPreset);
            CallEsportsPresetCommand = new RelayCommand(CallEsportsPreset);
            RegisterEsportsPresetCommand = new RelayCommand<int>(RegisterEsportsPreset);
            SelectEsportsMatrixCommand = new RelayCommand<EsportsMatrixButton>(SelectEsportsMatrix);
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
                EnsureEsportsPresetData(settings);
                
                if (settings.EsportsSelections == null)
                {
                    settings.EsportsSelections = new Dictionary<int, int>();
                }
                
                _allModeSettings[modeNumber] = settings;
            }
            
            if (modeNumber == CurrentMode)
            {
                _modeSettingsData = _allModeSettings[modeNumber];
            }
        }

        /// <summary>
        /// esportsマトリクスを初期化
        /// </summary>
        private void InitializeEsportsMatrix()
        {
            string modeName = CurrentModeName;
            ActionLogger.LogProcessingStart("Esportsマトリクス初期化", $"モード: {modeName}");
            
            EsportsMatrixButtons.Clear();
            EsportsMatrixRowLabels.Clear();
            EsportsMatrixColumnLabels.Clear();
            
            // 行ラベルを初期化（画像に基づく）
            EsportsMatrixRowLabels.Add("32型モニタ1");
            EsportsMatrixRowLabels.Add("32型モニタ2");
            EsportsMatrixRowLabels.Add("32型モニタ3");
            EsportsMatrixRowLabels.Add("PGM");
            EsportsMatrixRowLabels.Add("CLN");
            EsportsMatrixRowLabels.Add("リプレイ装置1");
            EsportsMatrixRowLabels.Add("リプレイ装置2");
            
            // 列ラベルを初期化（画像に基づく）
            EsportsMatrixColumnLabels.Add("Cam 1");
            EsportsMatrixColumnLabels.Add("Cam 2");
            EsportsMatrixColumnLabels.Add("Cam 3");
            EsportsMatrixColumnLabels.Add("HCam 1");
            EsportsMatrixColumnLabels.Add("HCam 2");
            EsportsMatrixColumnLabels.Add("PC 1");
            EsportsMatrixColumnLabels.Add("PC 2");
            EsportsMatrixColumnLabels.Add("PC 3");
            EsportsMatrixColumnLabels.Add("GPC 1");
            EsportsMatrixColumnLabels.Add("GPC 2");
            EsportsMatrixColumnLabels.Add("GPC 3");
            EsportsMatrixColumnLabels.Add("GPC 4");
            EsportsMatrixColumnLabels.Add("再 1");
            EsportsMatrixColumnLabels.Add("再 2");
            EsportsMatrixColumnLabels.Add("再 3");
            EsportsMatrixColumnLabels.Add("リプ 1");
            EsportsMatrixColumnLabels.Add("リプ 2");
            EsportsMatrixColumnLabels.Add("web2");

            // 7行×18列のマトリクスを生成
            ActionLogger.LogProcessing("マトリクス生成", "7行×18列のマトリクスを生成中");
            for (int row = 1; row <= 7; row++)
            {
                for (int column = 1; column <= 18; column++)
                {
                    EsportsMatrixButtons.Add(new EsportsMatrixButton
                    {
                        Row = row,
                        Column = column,
                        DisplayText = $"{row}-{column}",
                        IsSelected = false
                    });
                }
            }
            
            // 保存された選択状態を読み込み
            ActionLogger.LogProcessing("保存された選択状態の読み込み", "選択状態を読み込み中");
            LoadEsportsSelectionForCurrentMode();
            
            ActionLogger.LogResult("Esportsマトリクス初期化完了", $"行数: 7, 列数: 19");
            ActionLogger.LogProcessingComplete("Esportsマトリクス初期化");
        }

        /// <summary>
        /// esportsプリセットを選択
        /// </summary>
        private void SelectEsportsPreset(int presetNumber)
        {
            ActionLogger.LogAction("Esportsプリセット選択", $"プリセット番号: {presetNumber}");
            SelectedEsportsPresetNumber = presetNumber;
            
            // プリセット選択状態を保存
            if (_modeSettingsData != null)
            {
                _modeSettingsData.SelectedEsportsPresetNumber = presetNumber;
                
                // JSONファイルに即座に保存
                ModeSettingsManager.SaveModeSettings(CurrentMode, _modeSettingsData);
            }
        }

        /// <summary>
        /// esportsプリセットを登録
        /// </summary>
        private void RegisterEsportsPreset(int presetNumber)
        {
            if (presetNumber <= 0 || presetNumber > 8)
            {
                ActionLogger.LogError("Esportsプリセット登録", $"無効なプリセット番号: {presetNumber}");
                return;
            }

            var result = CustomDialog.Show(
                $"プリセット{presetNumber}を登録しますか？\n現在のマトリクス選択状態を保存します。",
                "プリセット登録確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            ActionLogger.LogAction("Esportsプリセット登録", $"プリセット番号: {presetNumber}");

            if (_modeSettingsData == null)
            {
                ActionLogger.LogError("Esportsプリセット登録", "設定データが無効です");
                return;
            }

            // 現在のマトリクス選択状態を取得
            var currentSelection = EsportsMatrixButtons
                .Where(b => b.IsSelected)
                .ToDictionary(b => b.Row, b => b.Column);

            if (currentSelection.Count == 0)
            {
                ActionLogger.LogError("Esportsプリセット登録", "マトリクスが選択されていません");
                CustomDialog.Show("マトリクスが選択されていません。", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // EsportsPresetsを初期化
            EnsureEsportsPresetData(_modeSettingsData);

            // プリセットに現在の選択状態を保存
            _modeSettingsData.EsportsPresets[presetNumber] = new Dictionary<int, int>(currentSelection);

            // JSONファイルに即座に保存
            ModeSettingsManager.SaveModeSettings(CurrentMode, _modeSettingsData);

            // 登録後、呼び出し済み状態に変更
            CalledEsportsPresetNumber = presetNumber;
            SelectedEsportsPresetNumber = presetNumber;

            var selectionList = currentSelection.OrderBy(s => s.Key).Select(s => $"行{s.Key}→列{s.Value}").ToList();
            var detail = string.Join(", ", selectionList);
            ActionLogger.LogResult("Esportsプリセット登録成功", $"プリセット{presetNumber}を登録しました: {detail}");
        }

        /// <summary>
        /// 選択中のesportsプリセットを呼び出し
        /// </summary>
        private void CallEsportsPreset()
        {
            if (SelectedEsportsPresetNumber == 0)
            {
                ActionLogger.LogError("Esportsプリセット呼出", "プリセットが選択されていません");
                CustomDialog.Show("プリセットが選択されていません。", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 確認ダイアログを表示
            var result = CustomDialog.Show(
                $"プリセット{SelectedEsportsPresetNumber}を呼び出しますか？",
                "プリセット呼出確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                ActionLogger.LogProcessing("ユーザー確認", "プリセット呼出がキャンセルされました");
                return;
            }

            ActionLogger.LogAction("Esportsプリセット呼出", $"プリセット番号: {SelectedEsportsPresetNumber}");
            ActionLogger.LogProcessingStart("Esportsプリセット処理", $"プリセット{SelectedEsportsPresetNumber}を呼び出し");

            ActionLogger.LogProcessing("プリセットデータの取得", $"プリセット{SelectedEsportsPresetNumber}のデータを取得中");
            var presetSelection = GetEsportsPresetSelection(SelectedEsportsPresetNumber);
            
            if (presetSelection == null || presetSelection.Count == 0)
            {
                ActionLogger.LogError("Esportsプリセット呼出", $"プリセット{SelectedEsportsPresetNumber}のデータが定義されていません");
                CustomDialog.Show($"プリセット{SelectedEsportsPresetNumber}のデータが定義されていません。", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ActionLogger.LogProcessing("マトリクスボタンのリセット", "すべてのボタンを未選択に");
            // 全てのボタンを未選択にする
            foreach (var button in EsportsMatrixButtons)
            {
                button.IsSelected = false;
            }
            
            ActionLogger.LogProcessing("プリセット選択状態の適用", $"選択数: {presetSelection.Count}");
            // プリセットに含まれるボタンを選択状態にする
            foreach (var (row, column) in presetSelection)
            {
                var button = EsportsMatrixButtons.FirstOrDefault(b => b.Row == row && b.Column == column);
                if (button != null)
                {
                    button.IsSelected = true;
                }
            }

            // 現在の選択状態を保存
            ActionLogger.LogProcessing("選択状態の保存", "選択状態を保存中");
            SaveEsportsCurrentSelection();
            
            // UDP送信機能は削除されました
            
            // 呼び出し済みプリセット番号を設定（濃い青に変更）
            CalledEsportsPresetNumber = SelectedEsportsPresetNumber;
            
            LogEsportsPresetSelection("Esportsプリセット呼出完了", presetSelection);
            ActionLogger.LogProcessingComplete("Esportsプリセット処理");
        }

        /// <summary>
        /// esportsマトリクスのセルを選択/選択解除
        /// </summary>
        private void SelectEsportsMatrix(EsportsMatrixButton? button)
        {
            if (button == null)
            {
                ActionLogger.LogError("Esportsマトリクス選択", "ボタンがnullです");
                return;
            }

            ActionLogger.LogAction("Esportsマトリクス選択", $"行: {button.Row}, 列: {button.Column}");
            
            ActionLogger.LogProcessingStart("マトリクス選択処理", $"行{button.Row}の他のボタンを未選択に");

            // 同じ行の他のボタンを全て未選択にする
            foreach (var b in EsportsMatrixButtons.Where(b => b.Row == button.Row && b != button))
            {
                b.IsSelected = false;
            }

            // 選択状態にする（トグルしない）
            button.IsSelected = true;
            
            ActionLogger.LogProcessing("マトリクス選択状態更新", $"行: {button.Row}, 列: {button.Column}, 選択状態: {button.IsSelected}");

            // 現在の選択状態を保存
            ActionLogger.LogProcessing("マトリクス選択状態の保存", "選択状態を保存中");
            SaveEsportsCurrentSelection();
            
            // UDP送信機能は削除されました
            
            // マトリクスを変更したので、オレンジ状態（仮選択）のプリセットを濃い青（呼び出し済み）に戻す
            if (SelectedEsportsPresetNumber > 0 && 
                CalledEsportsPresetNumber != SelectedEsportsPresetNumber)
            {
                CalledEsportsPresetNumber = SelectedEsportsPresetNumber;
            }
            
            ActionLogger.LogResult("マトリクス選択完了", $"行: {button.Row}, 列: {button.Column}, 選択状態: {button.IsSelected}");
            ActionLogger.LogProcessingComplete("マトリクス選択処理");
        }

        /// <summary>
        /// 現在のesports選択状態を保存（行ごとの選択形式）
        /// </summary>
        public void SaveEsportsCurrentSelection()
        {
            if (_modeSettingsData == null || CurrentMode == 0)
            {
                ActionLogger.LogError("Esportsマトリクス保存", "設定データまたはモードが無効です");
                return;
            }

            // 行ごとの選択を保存（行 -> 列）
            _modeSettingsData.EsportsSelections.Clear();
            var selectedButtons = EsportsMatrixButtons
                .Where(b => b.IsSelected)
                .ToList();

            foreach (var button in selectedButtons)
            {
                _modeSettingsData.EsportsSelections[button.Row] = button.Column;
            }


            string modeName = CurrentModeName;
            ActionLogger.LogProcessing("Esportsマトリクス選択状態の保存", $"モード: {modeName}, 選択行数: {_modeSettingsData.EsportsSelections.Count}");

            // JSONファイルに即座に保存
            ModeSettingsManager.SaveModeSettings(CurrentMode, _modeSettingsData);
        }

        /// <summary>
        /// 現在のモードのesports選択状態を読み込む（行ごとの選択形式、メモリ上のデータを使用）
        /// </summary>
        private void LoadEsportsSelectionForCurrentMode()
        {
            // メモリ上のデータを使用（JSONから読み込まない）
            if (_modeSettingsData == null || 
                _modeSettingsData.EsportsSelections == null || 
                CurrentMode == 0)
            {
                ActionLogger.LogProcessing("Esportsマトリクス選択状態の読み込み", "保存された選択状態がありません");
                return;
            }

            string modeName = CurrentModeName;
            ActionLogger.LogProcessingStart("Esportsマトリクス選択状態の読み込み", $"モード: {modeName}");

            // 全てのボタンを未選択にする
            foreach (var button in EsportsMatrixButtons)
            {
                button.IsSelected = false;
            }

            ActionLogger.LogProcessing("選択状態の復元", $"復元する行数: {_modeSettingsData.EsportsSelections.Count}");
            
            // 保存されている選択状態を反映（行ごとの選択）
            foreach (var (row, column) in _modeSettingsData.EsportsSelections)
            {
                var button = EsportsMatrixButtons.FirstOrDefault(b => b.Row == row && b.Column == column);
                if (button != null)
                {
                    button.IsSelected = true;
                }
            }
            
            ActionLogger.LogResult("Esportsマトリクス選択状態の読み込み完了", $"選択行数: {_modeSettingsData.EsportsSelections.Count}");
            ActionLogger.LogProcessingComplete("Esportsマトリクス選択状態の読み込み");
        }

        private void EnsureEsportsPresetData(ModeSettingsData settings)
        {
            if (settings.EsportsPresets == null)
            {
                settings.EsportsPresets = new Dictionary<int, Dictionary<int, int>>();
            }

            const int presetCount = 8;
            const int rowCount = 7;
            const int columnCount = 19;

            for (int preset = 1; preset <= presetCount; preset++)
            {
                if (!settings.EsportsPresets.ContainsKey(preset))
                {
                    settings.EsportsPresets[preset] = GenerateDefaultPreset(rowCount, columnCount, preset);
                }
            }
        }

        private Dictionary<int, int> GenerateDefaultPreset(int rowCount, int columnCount, int presetNumber)
        {
            var preset = new Dictionary<int, int>();
            for (int row = 1; row <= rowCount; row++)
            {
                int column = ((row + presetNumber - 2) % columnCount) + 1;
                preset[row] = column;
            }
            return preset;
        }

        private void ApplyEsportsPresetSelection(int presetNumber)
        {
            if (presetNumber <= 0)
            {
                return;
            }

            var presetSelection = GetEsportsPresetSelection(presetNumber);
            if (presetSelection == null)
            {
                return;
            }

            foreach (var button in EsportsMatrixButtons)
            {
                button.IsSelected = false;
            }

            foreach (var (row, column) in presetSelection)
            {
                var targetButton = EsportsMatrixButtons.FirstOrDefault(b => b.Row == row && b.Column == column);
                if (targetButton != null)
        {
                    targetButton.IsSelected = true;
                }
            }

            if (_modeSettingsData != null)
            {
                _modeSettingsData.EsportsSelections = presetSelection.ToDictionary(p => p.row, p => p.column);
            }
        }

        private List<(int row, int column)>? GetEsportsPresetSelection(int presetNumber)
        {
            if (_modeSettingsData == null || 
                _modeSettingsData.EsportsPresets == null)
            {
                return null;
            }

            if (!_modeSettingsData.EsportsPresets.TryGetValue(presetNumber, out var preset))
            {
                return null;
            }

            return preset
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => (kvp.Key, kvp.Value))
                .ToList();
        }

        private void LogEsportsPresetSelection(string title, List<(int row, int column)> selection)
        {
            if (selection.Count == 0)
            {
                ActionLogger.LogResult(title, "選択されているルーティングはありません。");
                return;
            }

            var detail = string.Join(", ", selection.Select(s => $"行{s.row}→列{s.column}"));
            ActionLogger.LogResult(title, detail);
        }

        /// <summary>
        /// 選択状態のみをクリア（呼び出し状態は保持）
        /// </summary>
        public void ClearEsportsPresetSelectionOnly()
        {
            SelectedEsportsPresetNumber = 0;

            if (_modeSettingsData != null)
            {
                _modeSettingsData.SelectedEsportsPresetNumber = 0;
                ModeSettingsManager.SaveModeSettings(CurrentMode, _modeSettingsData);
            }
        }
    }
}

