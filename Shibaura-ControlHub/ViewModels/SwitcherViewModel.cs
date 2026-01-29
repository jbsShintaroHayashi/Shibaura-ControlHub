using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Shibaura_ControlHub.Models;
using Shibaura_ControlHub;
using Shibaura_ControlHub.Services;
using Shibaura_ControlHub.Utils;
using Shibaura_ControlHub.Views;

namespace Shibaura_ControlHub.ViewModels
{
    /// <summary>
    /// スイッチャー制御のViewModel
    /// </summary>
    public class SwitcherViewModel : BaseViewModel
    {
        private readonly Dictionary<int, ModeSettingsData> _allModeSettings = new Dictionary<int, ModeSettingsData>();
        private ModeSettingsData? _modeSettingsData;
        private IAtemSwitcherClient? _atemClient;
        private readonly IAtemSwitcherClient _atemSwitcherClient;
        private string? _atemIpAddress;
        private bool _isAtemConnected = false;
        private SwitcherMatrixDef? _currentMatrixDef;

        private int _selectedSwitcherPresetNumber = 0;

        public ObservableCollection<EquipmentStatus> SwitcherList { get; set; }
        
        /// <summary>
        /// スイッチャーマトリクスボタンリスト
        /// </summary>
        public ObservableCollection<EsportsMatrixButton> SwitcherMatrixButtons { get; set; } = new ObservableCollection<EsportsMatrixButton>();

        /// <summary>
        /// スイッチャーマトリクスの行ラベル
        /// </summary>
        public ObservableCollection<string> SwitcherMatrixRowLabels { get; set; } = new ObservableCollection<string>();

        /// <summary>
        /// スイッチャーマトリクスの列ラベル
        /// </summary>
        public ObservableCollection<string> SwitcherMatrixColumnLabels { get; set; } = new ObservableCollection<string>();

        /// <summary>
        /// スイッチャーマトリクスの列数（XAMLバインディング用）
        /// </summary>
        public int SwitcherMatrixColumnCount => SwitcherMatrixColumnLabels.Count;

        /// <summary>
        /// 指定された行と列のボタンを取得（XAMLバインディング用）
        /// </summary>
        public EsportsMatrixButton? GetMatrixButton(int row, int column)
        {
            return SwitcherMatrixButtons.FirstOrDefault(b => b.Row == row && b.Column == column);
        }

        /// <summary>
        /// 選択中のスイッチャープリセット番号
        /// </summary>
        public int SelectedSwitcherPresetNumber
        {
            get => _selectedSwitcherPresetNumber;
            set
            {
                _selectedSwitcherPresetNumber = value;
                OnPropertyChanged();
            }
        }

        private int _calledSwitcherPresetNumber = 0;

        /// <summary>
        /// 呼び出し済みのスイッチャープリセット番号
        /// </summary>
        public int CalledSwitcherPresetNumber
        {
            get => _calledSwitcherPresetNumber;
            set
            {
                _calledSwitcherPresetNumber = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand SelectSwitcherPresetCommand { get; private set; } = null!;
        public ICommand CallSwitcherPresetCommand { get; private set; } = null!;
        public ICommand RegisterSwitcherPresetCommand { get; private set; } = null!;
        public ICommand SelectSwitcherMatrixCommand { get; private set; } = null!;

        public SwitcherViewModel(string mode, ObservableCollection<EquipmentStatus> switcherList)
        {
            SwitcherList = switcherList;
            _atemSwitcherClient = new AtemSwitcherClient();
            SetCurrentModeFromName(mode);
            
            LoadModeSettings();
            InitializeSwitcherMatrix();
            InitializeCommands();
            InitializeAtemConnection();
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
                SelectedSwitcherPresetNumber = _modeSettingsData.SelectedSwitcherPresetNumber;
            }
            
            InitializeSwitcherMatrix();
            
            OnPropertyChanged(nameof(SwitcherMatrixButtons));
            OnPropertyChanged(nameof(SwitcherMatrixRowLabels));
            OnPropertyChanged(nameof(SwitcherMatrixColumnLabels));
            OnPropertyChanged(nameof(SwitcherMatrixColumnCount));

            // モード変更に合わせて、復元された選択状態（出力/AUXルーティング）をATEMへ再送して「出力を呼び出す」
            if (_atemClient != null)
            {
                string modeName = ModeSettingsManager.GetModeName(CurrentMode);
                ActionLogger.LogProcessing("ATEMルーティング送信", $"モード変更に伴い現在の選択状態をATEMに適用します (モード: {modeName})");
                SendCurrentSelectionToAtem();
            }
        }

        /// <summary>
        /// コマンドの初期化
        /// </summary>
        private void InitializeCommands()
        {
            SelectSwitcherPresetCommand = new RelayCommand<int>(SelectSwitcherPreset);
            CallSwitcherPresetCommand = new RelayCommand(CallSwitcherPreset);
            RegisterSwitcherPresetCommand = new RelayCommand<int>(RegisterSwitcherPreset);
            SelectSwitcherMatrixCommand = new RelayCommand<EsportsMatrixButton>(SelectSwitcherMatrix);
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
                EnsureSwitcherPresetData(settings);
                
                if (settings.SwitcherSelections == null)
                {
                    settings.SwitcherSelections = new Dictionary<int, int>();
                }
                
                _allModeSettings[modeNumber] = settings;
            }
            
            if (modeNumber == CurrentMode)
            {
                _modeSettingsData = _allModeSettings[modeNumber];
            }
        }

        /// <summary>
        /// スイッチャーマトリクスを初期化（モードごとの定義を SwitcherDefinitions から取得）
        /// </summary>
        private void InitializeSwitcherMatrix()
        {
            string modeName = ModeSettingsManager.GetModeName(CurrentMode);
            ActionLogger.LogProcessingStart("スイッチャーマトリクス初期化", $"モード: {modeName}");

            var matrixId = CurrentMode == 3 ? SwitcherDefinitions.MatrixIdSwitcherEsports : SwitcherDefinitions.MatrixIdSwitcherRemote;
            var def = SwitcherDefinitions.GetMatrix(matrixId);
            if (def == null)
            {
                ActionLogger.LogError("スイッチャーマトリクス初期化", $"マトリクス定義が見つかりません: {matrixId}");
                return;
            }

            _currentMatrixDef = def;
            SwitcherMatrixButtons.Clear();
            SwitcherMatrixRowLabels.Clear();
            SwitcherMatrixColumnLabels.Clear();

            foreach (var o in def.Outputs)
                SwitcherMatrixRowLabels.Add(o.Label);
            foreach (var i in def.Inputs)
                SwitcherMatrixColumnLabels.Add(i.Label);

            int rowCount = def.RowCount;
            int columnCount = def.ColumnCount;
            ActionLogger.LogProcessing("マトリクス生成", $"{rowCount}行×{columnCount}列のマトリクスを生成中");
            for (int row = 1; row <= rowCount; row++)
            {
                for (int column = 1; column <= columnCount; column++)
                {
                    SwitcherMatrixButtons.Add(new EsportsMatrixButton
                    {
                        Row = row,
                        Column = column,
                        DisplayText = "",
                        IsSelected = false
                    });
                }
            }

            LoadSwitcherMatrixSelectionForCurrentMode();
            ActionLogger.LogResult("スイッチャーマトリクス初期化完了", $"行数: {rowCount}, 列数: {columnCount}");
            ActionLogger.LogProcessingComplete("スイッチャーマトリクス初期化");
        }

        /// <summary>
        /// スイッチャープリセットを選択
        /// </summary>
        private void SelectSwitcherPreset(int presetNumber)
        {
            ActionLogger.LogAction("スイッチャープリセット選択", $"プリセット番号: {presetNumber}");
            SelectedSwitcherPresetNumber = presetNumber;
            
            // プリセット選択状態を保存
            if (_modeSettingsData != null)
            {
                _modeSettingsData.SelectedSwitcherPresetNumber = presetNumber;
                
                // JSONファイルに即座に保存
                ModeSettingsManager.SaveModeSettings(CurrentMode, _modeSettingsData);
            }
        }

        /// <summary>
        /// スイッチャープリセットを登録
        /// </summary>
        private void RegisterSwitcherPreset(int presetNumber)
        {
            if (presetNumber <= 0 || presetNumber > 8)
            {
                ActionLogger.LogError("スイッチャープリセット登録", $"無効なプリセット番号: {presetNumber}");
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

            ActionLogger.LogAction("スイッチャープリセット登録", $"プリセット番号: {presetNumber}");

            if (_modeSettingsData == null)
            {
                ActionLogger.LogError("スイッチャープリセット登録", "設定データが無効です");
                return;
            }

            // 現在のマトリクス選択状態を取得
            var currentSelection = GetCurrentPresetSelection();

            if (currentSelection.Count == 0)
            {
                ActionLogger.LogError("スイッチャープリセット登録", "マトリクスが選択されていません");
                CustomDialog.Show("マトリクスが選択されていません。", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // SwitcherPresetsを初期化
            EnsureSwitcherPresetData(_modeSettingsData);

            // プリセットに現在の選択状態を保存
            _modeSettingsData.SwitcherPresets[presetNumber] = new Dictionary<int, int>(currentSelection);

            // JSONファイルに即座に保存
            ModeSettingsManager.SaveModeSettings(CurrentMode, _modeSettingsData);

            // 登録後、呼び出し済み状態に変更
            CalledSwitcherPresetNumber = presetNumber;
            SelectedSwitcherPresetNumber = presetNumber;

            LogPresetSelectionDetail("スイッチャープリセット登録完了", currentSelection);
            ActionLogger.LogResult("スイッチャープリセット登録成功", $"プリセット{presetNumber}を登録しました");
        }

        /// <summary>
        /// 選択中のスイッチャープリセットを呼び出し
        /// </summary>
        private void CallSwitcherPreset()
        {
            if (SelectedSwitcherPresetNumber == 0)
            {
                ActionLogger.LogError("スイッチャープリセット呼出", "プリセットが選択されていません");
                CustomDialog.Show("プリセットが選択されていません。", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 確認ダイアログを表示
            var result = CustomDialog.Show(
                $"プリセット{SelectedSwitcherPresetNumber}を呼び出しますか？",
                "プリセット呼出確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                ActionLogger.LogProcessing("ユーザー確認", "プリセット呼出がキャンセルされました");
                return;
            }

            ActionLogger.LogAction("スイッチャープリセット呼出", $"プリセット番号: {SelectedSwitcherPresetNumber}");
            ActionLogger.LogProcessingStart("スイッチャープリセット処理", $"プリセット{SelectedSwitcherPresetNumber}を呼び出し");
            
            // 新しいプリセットを呼び出すので、前の呼び出し済み状態を解除
            if (CalledSwitcherPresetNumber > 0 && CalledSwitcherPresetNumber != SelectedSwitcherPresetNumber)
            {
                CalledSwitcherPresetNumber = 0;
            }
            
            ApplySwitcherPresetSelection(SelectedSwitcherPresetNumber);
            
            // 現在の選択状態を保存
            ActionLogger.LogProcessing("選択状態の保存", "選択状態を保存中");
            SaveSwitcherMatrixSelection();
            
            // プリセットに含まれるすべてのマトリクス操作をAtemに送信
            var presetSelection = GetCurrentPresetSelection();
            foreach (var (row, column) in presetSelection)
            {
                SendSwitcherMatrixToAtem(row, column);
            }
            
            LogPresetSelectionDetail("スイッチャープリセット呼出完了", GetCurrentPresetSelection());
            
            // 呼び出し済みプリセット番号を設定（濃い青に変更）
            CalledSwitcherPresetNumber = SelectedSwitcherPresetNumber;
            
            ActionLogger.LogProcessingComplete("スイッチャープリセット処理");
        }

        /// <summary>
        /// スイッチャーマトリクスのセルを選択/選択解除
        /// </summary>
        private void SelectSwitcherMatrix(EsportsMatrixButton? button)
        {
            if (button == null)
            {
                ActionLogger.LogError("スイッチャーマトリクス選択", "ボタンがnullです");
                return;
            }

            ClearSwitcherPresetSelection();

            ActionLogger.LogAction("スイッチャーマトリクス選択", $"行: {button.Row}, 列: {button.Column}");
            
            ActionLogger.LogProcessingStart("マトリクス選択処理", $"行{button.Row}の他のボタンを未選択に");
            
            // 同じ行の他のボタンを全て未選択にする
            foreach (var b in SwitcherMatrixButtons.Where(b => b.Row == button.Row && b != button))
            {
                b.IsSelected = false;
            }

            // 選択状態にする（トグルしない）
            button.IsSelected = true;
            
            ActionLogger.LogProcessing("マトリクス選択状態更新", $"行: {button.Row}, 列: {button.Column}, 選択状態: {button.IsSelected}");

            // 現在の選択状態を保存
            ActionLogger.LogProcessing("マトリクス選択状態の保存", "選択状態を保存中");
            SaveSwitcherMatrixSelection();
            
            // Atemにマトリクス操作を送信
            SendSwitcherMatrixToAtem(button.Row, button.Column);
            
            ActionLogger.LogResult("マトリクス選択完了", $"行: {button.Row}, 列: {button.Column}, 選択状態: {button.IsSelected}");
            ActionLogger.LogProcessingComplete("マトリクス選択処理");
        }

        /// <summary>
        /// マトリクス操作時にプリセット選択状態を解除
        /// </summary>
        private void ClearSwitcherPresetSelection()
        {
            SelectedSwitcherPresetNumber = 0;
            CalledSwitcherPresetNumber = 0;

            if (_modeSettingsData != null)
            {
                _modeSettingsData.SelectedSwitcherPresetNumber = 0;
                ModeSettingsManager.SaveModeSettings(CurrentMode, _modeSettingsData);
            }
        }

        /// <summary>
        /// 選択状態のみをクリア（呼び出し状態は保持）
        /// </summary>
        public void ClearSwitcherPresetSelectionOnly()
        {
            SelectedSwitcherPresetNumber = 0;

            if (_modeSettingsData != null)
            {
                _modeSettingsData.SelectedSwitcherPresetNumber = 0;
                ModeSettingsManager.SaveModeSettings(CurrentMode, _modeSettingsData);
            }
        }

        /// <summary>
        /// ATEMクライアントを後から設定する（MainWindowで接続後に呼び出す）
        /// </summary>
        public void SetAtemClient(IAtemSwitcherClient? atemClient)
        {
            _atemClient = atemClient;
            ActionLogger.LogAction("ATEMクライアント設定", $"SwitcherViewModelにATEMクライアントを設定しました");

            // ATEMクライアントが設定された場合、現在の選択状態をATEMに適用
            if (_atemClient != null)
            {
                ActionLogger.LogProcessing("ATEMルーティング送信", "保存された選択状態をATEMに適用しています");
                SendCurrentSelectionToAtem();
            }
        }

        /// <summary>
        /// 現在の選択状態をATEMに送信（非同期）
        /// </summary>
        private void SendCurrentSelectionToAtem()
        {
            if (_atemClient == null) return;

            var selected = SwitcherMatrixButtons.Where(b => b.IsSelected).ToList();
            if (!selected.Any()) return;

            _ = Task.Run(async () =>
            {
                foreach (var b in selected)
                {
                    try
                    {
                        var auxIndex = GetAtemAuxIndexFromRow(b.Row);
                        var inputIndex = GetAtemInputIndexFromColumn(b.Column);

                        if (auxIndex == null || inputIndex == null)
                        {
                            continue;
                        }

                        // auxIndexは0ベース、inputIndexは1ベース（SwitcherDefinitions の定義から取得）
                        await _atemClient.RouteAuxAsync(auxIndex.Value, inputIndex.Value, CancellationToken.None);
                        ActionLogger.LogAction("ATEMルーティング送信", $"Row{b.Row} -> Col{b.Column} (AUX{auxIndex.Value + 1} -> Input{inputIndex.Value})");
                    }
                    catch (Exception ex)
                    {
                        ActionLogger.LogError("ATEMルーティングエラー", ex.Message);
                    }
                }
            });
        }

        /// <summary>
        /// 現在のスイッチャーマトリクスの選択状態を保存（行ごとの選択形式）
        /// </summary>
        public void SaveSwitcherMatrixSelection()
        {
            if (_modeSettingsData == null || CurrentMode == 0)
            {
                ActionLogger.LogError("スイッチャーマトリクス保存", "設定データまたはモードが無効です");
                return;
            }

            // 行ごとの選択を保存（行 -> 列）
            _modeSettingsData.SwitcherSelections.Clear();
            var selectedButtons = SwitcherMatrixButtons
                .Where(b => b.IsSelected)
                .ToList();

            foreach (var button in selectedButtons)
            {
                _modeSettingsData.SwitcherSelections[button.Row] = button.Column;
            }


            string modeName = ModeSettingsManager.GetModeName(CurrentMode);
            ActionLogger.LogProcessing("スイッチャーマトリクス選択状態の保存", $"モード: {modeName}, 選択行数: {_modeSettingsData.SwitcherSelections.Count}");

            // JSONファイルに即座に保存
            ModeSettingsManager.SaveModeSettings(CurrentMode, _modeSettingsData);
        }

        /// <summary>
        /// 現在のモードのスイッチャーマトリクスの選択状態を読み込み（行ごとの選択形式、メモリ上のデータを使用）
        /// </summary>
        private void LoadSwitcherMatrixSelectionForCurrentMode()
        {
            // メモリ上のデータを使用（JSONから読み込まない）
            if (_modeSettingsData == null || 
                _modeSettingsData.SwitcherSelections == null || 
                CurrentMode == 0)
            {
                ActionLogger.LogProcessing("スイッチャーマトリクス選択状態の読み込み", "保存された選択状態がありません");
                return;
            }

            string modeName = ModeSettingsManager.GetModeName(CurrentMode);
            ActionLogger.LogProcessingStart("スイッチャーマトリクス選択状態の読み込み", $"モード: {modeName}");

            // 全てのボタンを未選択にする
            foreach (var button in SwitcherMatrixButtons)
            {
                button.IsSelected = false;
            }

            ActionLogger.LogProcessing("選択状態の復元", $"復元する行数: {_modeSettingsData.SwitcherSelections.Count}");
            
            // 保存されている選択状態を反映（行ごとの選択）
            foreach (var (row, column) in _modeSettingsData.SwitcherSelections)
            {
                var button = SwitcherMatrixButtons.FirstOrDefault(b => b.Row == row && b.Column == column);
                if (button != null)
                {
                    button.IsSelected = true;
                }
            }
            
            ActionLogger.LogResult("スイッチャーマトリクス選択状態の読み込み完了", $"選択行数: {_modeSettingsData.SwitcherSelections.Count}");
            ActionLogger.LogProcessingComplete("スイッチャーマトリクス選択状態の読み込み");
        }

        private void EnsureSwitcherPresetData(ModeSettingsData settings)
        {
            if (settings.SwitcherPresets == null)
            {
                settings.SwitcherPresets = new Dictionary<int, Dictionary<int, int>>();
            }

            const int presetCount = 8;
            int rowCount = CurrentMode == 3 ? 8 : 8;
            int columnCount = CurrentMode == 3 ? 11 : 11;

            for (int preset = 1; preset <= presetCount; preset++)
            {
                if (!settings.SwitcherPresets.ContainsKey(preset))
                {
                    settings.SwitcherPresets[preset] = GenerateDefaultPreset(rowCount, columnCount, preset);
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

        private void ApplySwitcherPresetSelection(int presetNumber)
        {
            if (presetNumber <= 0 || 
                _modeSettingsData == null || 
                _modeSettingsData.SwitcherPresets == null)
        {
                return;
            }

            if (!_modeSettingsData.SwitcherPresets.TryGetValue(presetNumber, out var preset))
            {
                return;
            }

            foreach (var button in SwitcherMatrixButtons)
            {
                button.IsSelected = false;
            }

            foreach (var (row, column) in preset)
            {
                var targetButton = SwitcherMatrixButtons.FirstOrDefault(b => b.Row == row && b.Column == column);
                if (targetButton != null)
                {
                    targetButton.IsSelected = true;
                }
            }

            _modeSettingsData.SwitcherSelections = new Dictionary<int, int>(preset);
        }

        private Dictionary<int, int> GetCurrentPresetSelection()
        {
            return SwitcherMatrixButtons
                .Where(b => b.IsSelected)
                .ToDictionary(b => b.Row, b => b.Column);
        }

        private void LogPresetSelectionDetail(string title, Dictionary<int, int> selection)
        {
            if (selection.Count == 0)
            {
                ActionLogger.LogResult(title, "選択されているルーティングはありません。");
                return;
            }

            var detail = string.Join(", ", selection.OrderBy(s => s.Key)
                .Select(s => $"行{s.Key}→列{s.Value}"));
            ActionLogger.LogResult(title, detail);
        }

        /// <summary>
        /// Atem接続を初期化
        /// </summary>
        private async void InitializeAtemConnection()
        {
            // SwitcherListからマトリクススイッチャのIPアドレスを取得
            var switcher = SwitcherList?.FirstOrDefault(s => s.Name.Contains("マトリクススイッチャ") || s.Name.Contains("スイッチャ"));
            if (switcher != null && !string.IsNullOrEmpty(switcher.IpAddress))
            {
                _atemIpAddress = switcher.IpAddress;
                try
                {
                    ActionLogger.LogProcessingStart("Atem接続", $"IPアドレス: {_atemIpAddress}");
                    await _atemSwitcherClient.ConnectAsync(_atemIpAddress, CancellationToken.None);
                    _isAtemConnected = true;
                    
                    // 接続成功時に_atemClientに設定（実際に使用されるクライアント）
                    _atemClient = _atemSwitcherClient;
                    
                    ActionLogger.LogResult("Atem接続成功", $"IPアドレス: {_atemIpAddress}");
                    ActionLogger.LogProcessingComplete("Atem接続");
                    
                    // 接続成功後、現在の選択状態をATEMに適用
                    SendCurrentSelectionToAtem();
                }
                catch (Exception ex)
                {
                    ActionLogger.LogError("Atem接続エラー", $"IPアドレス: {_atemIpAddress}, エラー: {ex.Message}");
                    _isAtemConnected = false;
                }
            }
            else
            {
                ActionLogger.LogError("Atem接続", "マトリクススイッチャのIPアドレスが見つかりません");
            }
        }

        /// <summary>
        /// 列をATEM入力番号（1始まり）に変換。定義ベース。
        /// </summary>
        private int? GetAtemInputIndexFromColumn(int column)
        {
            if (_currentMatrixDef == null || column < 1 || column > _currentMatrixDef.Inputs.Count)
                return null;
            return _currentMatrixDef.Inputs[column - 1].InputIndex1Based;
        }

        /// <summary>
        /// 行をATEM AUX番号（0始まり）に変換。定義ベース。
        /// </summary>
        private int? GetAtemAuxIndexFromRow(int row)
        {
            if (_currentMatrixDef == null || row < 1 || row > _currentMatrixDef.Outputs.Count)
                return null;
            return _currentMatrixDef.Outputs[row - 1].AuxIndex0Based;
        }

        /// <summary>
        /// スイッチャー画面のマトリクス選択をATEMに送信（現在のマトリクス定義を使用）
        /// </summary>
        private void SendSwitcherMatrixToAtem(int row, int column)
        {
            RouteToAtem(row, column, matrixId: null);
        }

        /// <summary>
        /// ATEMへルーティング（AUX出力→入力）を送信する。呼び出し元はスイッチャー画面と録画画面の2箇所。
        /// matrixId を指定するとそのマトリクス定義を使用、null のときは現在のスイッチャーマトリクス定義を使用。
        /// </summary>
        public void RouteToAtem(int row, int column, string? matrixId)
        {
            if (!_isAtemConnected || _atemClient == null)
            {
                ActionLogger.LogProcessing("Atem送信スキップ", "Atemに接続されていないため送信をスキップします");
                return;
            }

            var def = matrixId != null ? SwitcherDefinitions.GetMatrix(matrixId) : _currentMatrixDef;
            if (def == null)
            {
                ActionLogger.LogError("Atem送信", $"マトリクス定義が見つかりません: {matrixId ?? "現在"}");
                return;
            }

            if (row < 1 || row > def.Outputs.Count)
            {
                ActionLogger.LogError("Atem送信", $"無効な行番号: {row}");
                return;
            }
            if (column < 1 || column > def.Inputs.Count)
            {
                ActionLogger.LogProcessing("Atem送信スキップ", $"列{column}は範囲外のため送信をスキップします");
                return;
            }

            int auxIndex = def.Outputs[row - 1].AuxIndex0Based;
            int inputIndex = def.Inputs[column - 1].InputIndex1Based;

            _ = Task.Run(async () =>
            {
                try
                {
                    ActionLogger.LogProcessing("Atem送信", $"AUX{auxIndex + 1}に入力{inputIndex}をルーティング");
                    await _atemClient.RouteAuxAsync(auxIndex, inputIndex, CancellationToken.None);
                    ActionLogger.LogResult("Atem送信成功", $"AUX{auxIndex + 1}に入力{inputIndex}をルーティングしました");
                }
                catch (Exception ex)
                {
                    ActionLogger.LogError("Atem送信エラー", $"AUX{auxIndex + 1}に入力{inputIndex}をルーティング中にエラー: {ex.Message}");
                }
            });
        }
    }
}

