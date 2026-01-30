using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using Shibaura_ControlHub.Models;
using Shibaura_ControlHub.Services;
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

        private readonly DispatcherTimer _timecodeTimer;
        private readonly ObservableCollection<EquipmentStatus> _monitoringList;
        private readonly HyperDeckTcpService _hyperDeckTcpService = new HyperDeckTcpService();

        private bool _isOverallRecording;
        private DateTime? _overallRecordingStartTime;
        private string _overallRecordingStatusText = "待機中";
        private string _overallTimecode = "00:00:00";

        private bool _isRecording1;
        private DateTime? _recording1StartTime;
        private string _recording1StatusText = "待機中";
        private string _recording1Timecode = "00:00:00";

        private bool _isRecording2;
        private DateTime? _recording2StartTime;
        private string _recording2StatusText = "待機中";
        private string _recording2Timecode = "00:00:00";

        /// <summary>
        /// 録画マトリクスボタンリスト（2行×10列 = 20個）
        /// </summary>
        public ObservableCollection<EsportsMatrixButton> RecordingMatrixButtons { get; set; } = new ObservableCollection<EsportsMatrixButton>();

        /// <summary>
        /// 録画マトリクスの行ラベル（2行分）
        /// </summary>
        public ObservableCollection<string> RecordingMatrixRowLabels { get; set; } = new ObservableCollection<string>();

        /// <summary>
        /// 録画マトリクスの列ラベル（10列分）
        /// </summary>
        public ObservableCollection<string> RecordingMatrixColumnLabels { get; set; } = new ObservableCollection<string>();

        // Commands
        public ICommand SelectRecordingMatrixCommand { get; private set; } = null!;
        public ICommand StartOverallRecordingCommand { get; private set; } = null!;
        public ICommand StopOverallRecordingCommand { get; private set; } = null!;
        public ICommand StartRecording1Command { get; private set; } = null!;
        public ICommand StopRecording1Command { get; private set; } = null!;
        public ICommand StartRecording2Command { get; private set; } = null!;
        public ICommand StopRecording2Command { get; private set; } = null!;

        /// <summary>
        /// 録画マトリクスの選択に合わせてスイッチャーへルーティング送信を要求するイベント。(row, column, matrixId)。
        /// ModeControlViewModel で SwitcherViewModel.RouteToAtem(row, column, matrixId) に接続している。
        /// </summary>
        public event Action<int, int, string>? RoutingRequested;

        public bool IsOverallRecording
        {
            get => _isOverallRecording;
            private set
            {
                if (_isOverallRecording != value)
                {
                    _isOverallRecording = value;
                    OnPropertyChanged();
                }
            }
        }

        public string OverallRecordingStatusText
        {
            get => _overallRecordingStatusText;
            private set
            {
                if (_overallRecordingStatusText != value)
                {
                    _overallRecordingStatusText = value;
                    OnPropertyChanged();
                }
            }
        }

        public string OverallTimecode
        {
            get => _overallTimecode;
            private set
            {
                if (_overallTimecode != value)
                {
                    _overallTimecode = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsRecording1
        {
            get => _isRecording1;
            private set
            {
                if (_isRecording1 != value)
                {
                    _isRecording1 = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Recording1StatusText
        {
            get => _recording1StatusText;
            private set
            {
                if (_recording1StatusText != value)
                {
                    _recording1StatusText = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Recording1Timecode
        {
            get => _recording1Timecode;
            private set
            {
                if (_recording1Timecode != value)
                {
                    _recording1Timecode = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsRecording2
        {
            get => _isRecording2;
            private set
            {
                if (_isRecording2 != value)
                {
                    _isRecording2 = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Recording2StatusText
        {
            get => _recording2StatusText;
            private set
            {
                if (_recording2StatusText != value)
                {
                    _recording2StatusText = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Recording2Timecode
        {
            get => _recording2Timecode;
            private set
            {
                if (_recording2Timecode != value)
                {
                    _recording2Timecode = value;
                    OnPropertyChanged();
                }
            }
        }

        public RecordingViewModel(string mode, ObservableCollection<EquipmentStatus> monitoringList)
        {
            SetCurrentModeFromName(mode);
            _monitoringList = monitoringList;
            
            LoadModeSettings();
            InitializeRecordingMatrix();
            InitializeCommands();

            _timecodeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timecodeTimer.Tick += (_, __) => UpdateTimecodes();
            _timecodeTimer.Start();
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

            // モード変更に伴い、復元された録画マトリクス選択をATEMへルーティング送信（録画のルーティングも変更する）
            var selected = RecordingMatrixButtons.Where(b => b.IsSelected).ToList();
            if (selected.Count > 0)
            {
                var selectionSummary = string.Join(", ", selected.Select(b => $"Row{b.Row}->Col{b.Column}"));
                ActionLogger.LogProcessing("録画ルーティング送信", $"モード変更に伴い録画で選択していたものをATEMに適用します (選択数: {selected.Count}) [{selectionSummary}]");
                RequestRoutingForCurrentSelection();
            }
            else
            {
                ActionLogger.LogProcessing("録画ルーティング送信", "モード変更時: 録画マトリクスに選択がありません（スキップ）");
            }
        }

        /// <summary>
        /// コマンドの初期化
        /// </summary>
        private void InitializeCommands()
        {
            SelectRecordingMatrixCommand = new RelayCommand<EsportsMatrixButton>(SelectRecordingMatrix);
            StartOverallRecordingCommand = new RelayCommand(StartOverallRecording, () => !IsOverallRecording);
            StopOverallRecordingCommand = new RelayCommand(StopOverallRecording, () => IsOverallRecording);
            StartRecording1Command = new RelayCommand(StartRecording1, () => !IsRecording1);
            StopRecording1Command = new RelayCommand(StopRecording1, () => IsRecording1);
            StartRecording2Command = new RelayCommand(StartRecording2, () => !IsRecording2);
            StopRecording2Command = new RelayCommand(StopRecording2, () => IsRecording2);
        }

        private async void StartOverallRecording()
        {
            var (ok1, ok2) = await StartBothAsync().ConfigureAwait(false);
            if (!ok1 && !ok2)
            {
                return;
            }

            // UIスレッドで反映
            await Dispatcher.Yield();
            IsOverallRecording = ok1 || ok2;
            _overallRecordingStartTime = DateTime.UtcNow;
            OverallRecordingStatusText = "録画中";
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }

        private async void StopOverallRecording()
        {
            var (ok1, ok2) = await StopBothAsync().ConfigureAwait(false);
            if (!ok1 && !ok2)
            {
                return;
            }

            await Dispatcher.Yield();
            IsOverallRecording = false;
            _overallRecordingStartTime = null;
            OverallRecordingStatusText = "待機中";
            OverallTimecode = "00:00:00";
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }

        private async void StartRecording1()
        {
            var ok = await StartOneAsync(1).ConfigureAwait(false);
            if (!ok)
            {
                return;
            }

            await Dispatcher.Yield();
            IsRecording1 = true;
            _recording1StartTime = DateTime.UtcNow;
            Recording1StatusText = "録画中";
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }

        private async void StopRecording1()
        {
            var ok = await StopOneAsync(1).ConfigureAwait(false);
            if (!ok)
            {
                return;
            }

            await Dispatcher.Yield();
            IsRecording1 = false;
            _recording1StartTime = null;
            Recording1StatusText = "待機中";
            Recording1Timecode = "00:00:00";
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }

        private async void StartRecording2()
        {
            var ok = await StartOneAsync(2).ConfigureAwait(false);
            if (!ok)
            {
                return;
            }

            await Dispatcher.Yield();
            IsRecording2 = true;
            _recording2StartTime = DateTime.UtcNow;
            Recording2StatusText = "録画中";
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }

        private async void StopRecording2()
        {
            var ok = await StopOneAsync(2).ConfigureAwait(false);
            if (!ok)
            {
                return;
            }

            await Dispatcher.Yield();
            IsRecording2 = false;
            _recording2StartTime = null;
            Recording2StatusText = "待機中";
            Recording2Timecode = "00:00:00";
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }

        private EquipmentStatus? FindHyperDeck(int recorderNumber)
        {
            // 名前の揺れ吸収（例: 録画機１/録画機1/再生機１/再生機1）
            string[] candidates = recorderNumber switch
            {
                1 => new[] { "録画機１", "録画機1", "再生機１", "再生機1", "REC1", "rec1" },
                2 => new[] { "録画機２", "録画機2", "再生機２", "再生機2", "REC2", "rec2" },
                _ => Array.Empty<string>()
            };

            return _monitoringList.FirstOrDefault(e =>
                !string.IsNullOrWhiteSpace(e.IpAddress) &&
                candidates.Any(c => e.Name.Contains(c, StringComparison.OrdinalIgnoreCase)));
        }

        private async Task<bool> StartOneAsync(int recorderNumber)
        {
            var deck = FindHyperDeck(recorderNumber);
            if (deck == null)
            {
                ActionLogger.LogError("HyperDeck録画開始", $"録画機{recorderNumber} のIPが見つかりません（monitoring_equipment.json を確認してください）");
                return false;
            }

            try
            {
                ActionLogger.LogProcessing("HyperDeck録画開始", $"録画機{recorderNumber}: {deck.IpAddress}:{deck.Port} へ record");
                await _hyperDeckTcpService.RecordAsync(deck.IpAddress, deck.Port).ConfigureAwait(false);
                ActionLogger.LogResult("HyperDeck録画開始", $"録画機{recorderNumber}: record を送信しました");
                return true;
            }
            catch (Exception ex)
            {
                ActionLogger.LogError("HyperDeck録画開始", $"録画機{recorderNumber}: 送信失敗: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> StopOneAsync(int recorderNumber)
        {
            var deck = FindHyperDeck(recorderNumber);
            if (deck == null)
            {
                ActionLogger.LogError("HyperDeck録画停止", $"録画機{recorderNumber} のIPが見つかりません（monitoring_equipment.json を確認してください）");
                return false;
            }

            try
            {
                ActionLogger.LogProcessing("HyperDeck録画停止", $"録画機{recorderNumber}: {deck.IpAddress}:{deck.Port} へ stop");
                await _hyperDeckTcpService.StopAsync(deck.IpAddress, deck.Port).ConfigureAwait(false);
                ActionLogger.LogResult("HyperDeck録画停止", $"録画機{recorderNumber}: stop を送信しました");
                return true;
            }
            catch (Exception ex)
            {
                ActionLogger.LogError("HyperDeck録画停止", $"録画機{recorderNumber}: 送信失敗: {ex.Message}");
                return false;
            }
        }

        private async Task<(bool ok1, bool ok2)> StartBothAsync()
        {
            var t1 = StartOneAsync(1);
            var t2 = StartOneAsync(2);
            await Task.WhenAll(t1, t2).ConfigureAwait(false);
            return (t1.Result, t2.Result);
        }

        private async Task<(bool ok1, bool ok2)> StopBothAsync()
        {
            var t1 = StopOneAsync(1);
            var t2 = StopOneAsync(2);
            await Task.WhenAll(t1, t2).ConfigureAwait(false);
            return (t1.Result, t2.Result);
        }

        private void UpdateTimecodes()
        {
            if (IsOverallRecording && _overallRecordingStartTime.HasValue)
            {
                OverallTimecode = FormatTimecode(DateTime.UtcNow - _overallRecordingStartTime.Value);
            }

            if (IsRecording1 && _recording1StartTime.HasValue)
            {
                Recording1Timecode = FormatTimecode(DateTime.UtcNow - _recording1StartTime.Value);
            }

            if (IsRecording2 && _recording2StartTime.HasValue)
            {
                Recording2Timecode = FormatTimecode(DateTime.UtcNow - _recording2StartTime.Value);
            }
        }

        private static string FormatTimecode(TimeSpan elapsed)
        {
            if (elapsed < TimeSpan.Zero)
            {
                elapsed = TimeSpan.Zero;
            }

            var hours = (int)elapsed.TotalHours;
            return $"{hours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
        }

        /// <summary>
        /// 録画マトリクスを初期化（SwitcherDefinitions.Recording から定義を取得）
        /// </summary>
        private void InitializeRecordingMatrix()
        {
            string modeName = CurrentModeName;
            ActionLogger.LogProcessingStart("録画マトリクス初期化", $"モード: {modeName}");

            var def = SwitcherDefinitions.GetMatrix(SwitcherDefinitions.MatrixIdRecording);
            if (def == null)
            {
                ActionLogger.LogError("録画マトリクス初期化", "マトリクス定義 Recording が見つかりません");
                return;
            }

            RecordingMatrixButtons.Clear();
            RecordingMatrixRowLabels.Clear();
            RecordingMatrixColumnLabels.Clear();

            foreach (var o in def.Outputs)
                RecordingMatrixRowLabels.Add(o.Label);
            foreach (var i in def.Inputs)
                RecordingMatrixColumnLabels.Add(i.Label);

            int rowCount = def.RowCount;
            int columnCount = def.ColumnCount;
            ActionLogger.LogProcessing("マトリクス生成", $"{rowCount}行×{columnCount}列のマトリクスを生成中");
            for (int row = 1; row <= rowCount; row++)
            {
                for (int column = 1; column <= columnCount; column++)
                {
                    RecordingMatrixButtons.Add(new EsportsMatrixButton
                    {
                        Row = row,
                        Column = column,
                        IsSelected = false
                    });
                }
            }

            ActionLogger.LogResult("録画マトリクス初期化完了", $"行数: {rowCount}, 列数: {columnCount}");
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
            RoutingRequested?.Invoke(button.Row, button.Column, SwitcherDefinitions.MatrixIdRecording);

            ActionLogger.LogResult("マトリクス選択完了", $"行: {button.Row}, 列: {button.Column}, 選択状態: {button.IsSelected}");
            ActionLogger.LogProcessingComplete("マトリクス選択処理");
        }

        /// <summary>
        /// 現在の録画マトリクス選択に合わせてATEMへルーティングを送信する。モード変更時およびユーザーが録画マトリクスを選択したときに呼ばれる。
        /// </summary>
        private void RequestRoutingForCurrentSelection()
        {
            var selected = RecordingMatrixButtons.Where(b => b.IsSelected).ToList();
            if (selected.Count == 0)
            {
                return;
            }

            foreach (var b in selected)
            {
                RoutingRequested?.Invoke(b.Row, b.Column, SwitcherDefinitions.MatrixIdRecording);
            }
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
        /// 録画マトリクスの選択状態を保存（行ごとの選択形式）。モード切替前に呼び出してモードごとの状態を保存する。
        /// </summary>
        public void SaveRecordingMatrixSelection()
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

