using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Shibaura_ControlHub.Models;
using Shibaura_ControlHub;
using Shibaura_ControlHub.Utils;
using Shibaura_ControlHub.Views;

namespace Shibaura_ControlHub.ViewModels
{
    /// <summary>
    /// マイク制御のViewModel
    /// </summary>
    public class MicrophoneViewModel : BaseViewModel
    {
        /// <summary>
        /// 全モード（Mode1、Mode2、Mode3）の設定データを保持するDictionary
        /// キー: モード番号（1、2、3）- 内部処理では数字で扱う
        /// 値: ModeSettingsData（各モードの設定データ）
        /// 起動時にJSONファイルから読み込み、以降はメモリ上のデータを使用
        /// </summary>
        private Dictionary<int, ModeSettingsData> _allModeSettings = new Dictionary<int, ModeSettingsData>();
        
        /// <summary>
        /// 現在選択中のモードの設定データ
        /// モード変更時に更新される
        /// </summary>
        private ModeSettingsData? _modeSettingsData;
        

        public ObservableCollection<EquipmentStatus> MicrophoneList { get; set; }
        
        /// <summary>
        /// マイクフェーダー値（4個分）
        /// </summary>
        public ObservableCollection<double> MicrophoneFaderValues { get; set; } = new ObservableCollection<double>();
        
        /// <summary>
        /// 出力フェーダー値（2個分：S1、S2）
        /// </summary>
        public ObservableCollection<double> OutputFaderValues { get; set; } = new ObservableCollection<double>();

        /// <summary>
        /// 出力ミュート状態（2個分：S1、S2）
        /// </summary>
        public ObservableCollection<bool> OutputMuteStates { get; set; } = new ObservableCollection<bool>();

        /// <summary>
        /// マイクミュート状態（4個分：M1、M2、M3、M4）
        /// </summary>
        public ObservableCollection<bool> MicrophoneMuteStates { get; set; } = new ObservableCollection<bool>();

        public MicrophoneViewModel(string mode, ObservableCollection<EquipmentStatus> microphoneList)
        {
            MicrophoneList = microphoneList;
            SetCurrentModeFromName(mode);
            
            // モード設定を読み込む（JSONファイルから常に最新の設定を読み込む）
            LoadModeSettings();
            
            int modeNumber = CurrentMode;
            if (modeNumber > 0)
            {
                // LoadModeSettings()で既に_allModeSettingsに設定が読み込まれている
                _modeSettingsData = _allModeSettings[modeNumber];
                
                if (_modeSettingsData == null)
                {
                    ActionLogger.LogError("設定読み込み", $"モード{modeNumber}の設定データがnullです");
                    return;
                }
                
                ActionLogger.LogAction("フェーダー値読み込み", $"モード{modeNumber}のフェーダー値を読み込み中");
                if (_modeSettingsData.MicrophoneFaderValues != null && _modeSettingsData.MicrophoneFaderValues.Length == 4)
                {
                    ActionLogger.LogProcessing("マイクフェーダー値", $"読み込み値: [{string.Join(", ", _modeSettingsData.MicrophoneFaderValues)}]");
                }
                if (_modeSettingsData.OutputFaderValues != null && _modeSettingsData.OutputFaderValues.Length == 2)
                {
                    ActionLogger.LogProcessing("出力フェーダー値", $"読み込み値: [{string.Join(", ", _modeSettingsData.OutputFaderValues)}]");
                }
                
                // フェーダー値のコレクションを初期化（JSONファイルの値を使用）
                InitializeFaderValuesFromJson();
                
                // ミュート状態のコレクションを初期化（JSONファイルの値を使用）
                InitializeMuteStatesFromJson();
            }
            SetupFaderValueChangedHandler();
            ApplyStoredSettingsToHardware();
        }

        /// <summary>
        /// モード変更時の処理
        /// </summary>
        protected override void OnModeChanged()
        {
            base.OnModeChanged();
            
            int modeNumber = CurrentMode;
            if (modeNumber == 0) return;
            
            // 常にJSONファイルから最新の設定を読み込む
            LoadModeSettings(ModeSettingsManager.GetModeName(CurrentMode));
            
            _modeSettingsData = _allModeSettings[modeNumber];
            
            // フェーダー値のコレクションをクリアして再初期化（JSONファイルの値を使用）
            MicrophoneFaderValues.Clear();
            OutputFaderValues.Clear();
            InitializeFaderValuesFromJson();
            
            // ミュート状態のコレクションをクリアして再初期化（JSONファイルの値を使用）
            MicrophoneMuteStates.Clear();
            OutputMuteStates.Clear();
            InitializeMuteStatesFromJson();
            
            OnPropertyChanged(nameof(MicrophoneFaderValues));
            OnPropertyChanged(nameof(OutputFaderValues));
            OnPropertyChanged(nameof(MicrophoneMuteStates));
            OnPropertyChanged(nameof(OutputMuteStates));

            ApplyStoredSettingsToHardware();
        }

        /// <summary>
        /// モード設定を読み込む（起動時のみJSONから読み込む）
        /// </summary>
        private void LoadModeSettings()
        {
            LoadModeSettings(ModeSettingsManager.GetModeName(CurrentMode));
        }

        /// <summary>
        /// 指定されたモードの設定を読み込む（起動時のみJSONファイルから読み込む）
        /// </summary>
        /// <param name="mode">モード名（"授業"、"遠隔"、"e-sports"など）</param>
        private void LoadModeSettings(string mode)
        {
            int modeNumber = ModeSettingsManager.GetModeNumber(mode);
            LoadModeSettingsForModeNumber(modeNumber);
        }

        /// <summary>
        /// 指定されたモード番号の設定を読み込む（JSONファイルから常に最新の設定を読み込む）
        /// </summary>
        /// <param name="modeNumber">モード番号（1、2、3）</param>
        internal void LoadModeSettingsForModeNumber(int modeNumber)
        {
            if (modeNumber == 0) return;
            
            ActionLogger.LogAction("設定読み込み", $"モード{modeNumber}の設定をJSONファイルから読み込み中");
            
            // 常にJSONファイルから最新の設定を読み込む
            var settings = ModeSettingsManager.LoadModeSettings(modeNumber);
            
            if (settings.MicrophoneFaderValues == null || settings.MicrophoneFaderValues.Length == 0)
            {
                ActionLogger.LogProcessing("マイクフェーダー値初期化", "JSONファイルに値が存在しないため、50.0で初期化");
                settings.MicrophoneFaderValues = new double[4] { 50.0, 50.0, 50.0, 50.0 };
            }
            else
            {
                ActionLogger.LogProcessing("マイクフェーダー値読み込み", $"読み込み値: [{string.Join(", ", settings.MicrophoneFaderValues)}]");
            }
            
            if (settings.OutputFaderValues == null || settings.OutputFaderValues.Length == 0)
            {
                ActionLogger.LogProcessing("出力フェーダー値初期化", "JSONファイルに値が存在しないため、50.0で初期化");
                settings.OutputFaderValues = new double[2] { 50.0, 50.0 };
            }
            else
            {
                ActionLogger.LogProcessing("出力フェーダー値読み込み", $"読み込み値: [{string.Join(", ", settings.OutputFaderValues)}]");
            }
            // ミュート状態はJSONファイルの値を使用（初期化しない）
            
            _allModeSettings[modeNumber] = settings;
            
            if (modeNumber == CurrentMode)
            {
                _modeSettingsData = _allModeSettings[modeNumber];
                ActionLogger.LogResult("設定読み込み完了", $"モード{modeNumber}の設定を読み込みました");
            }
        }

        /// <summary>
        /// フェーダー値変更イベントハンドラの設定
        /// </summary>
        private void SetupFaderValueChangedHandler()
        {
            if (MicrophoneFaderValues != null)
            {
                MicrophoneFaderValues.CollectionChanged -= MicrophoneFaderValues_CollectionChanged;
                MicrophoneFaderValues.CollectionChanged += MicrophoneFaderValues_CollectionChanged;
            }

            if (OutputFaderValues != null)
            {
                OutputFaderValues.CollectionChanged -= OutputFaderValues_CollectionChanged;
                OutputFaderValues.CollectionChanged += OutputFaderValues_CollectionChanged;
            }

            if (MicrophoneMuteStates != null)
            {
                MicrophoneMuteStates.CollectionChanged -= MicrophoneMuteStates_CollectionChanged;
                MicrophoneMuteStates.CollectionChanged += MicrophoneMuteStates_CollectionChanged;
            }

            if (OutputMuteStates != null)
            {
                OutputMuteStates.CollectionChanged -= OutputMuteStates_CollectionChanged;
                OutputMuteStates.CollectionChanged += OutputMuteStates_CollectionChanged;
            }
        }

        /// <summary>
        /// マイクフェーダー値が変更されたときの処理
        /// </summary>
        private void MicrophoneFaderValues_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null && e.NewStartingIndex >= 0 && e.NewStartingIndex < MicrophoneFaderValues.Count)
            {
                int micNumber = e.NewStartingIndex + 1;
                if (e.NewItems.Count > 0 && e.NewItems[0] is double value)
                {
                    ActionLogger.LogAction("マイクフェーダー値変更", $"マイク番号: {micNumber}, 値: {value:0.0}");
                    
                    ActionLogger.LogProcessingStart("マイクフェーダー値更新", $"マイク{micNumber}の値を{value:0.0}に更新");
                    
                    // UDP送信機能は削除されました
                    
                    string modeName = CurrentModeName;
                    ActionLogger.LogProcessing("フェーダー値の保存", $"モード: {modeName}に保存");
                    SaveCurrentFaderValuesToEquipmentSettings();
                    
                    if (_modeSettingsData != null && CurrentMode != 0)
                    {
                        ActionLogger.LogProcessing("JSONファイルへの保存", $"モード: {modeName}の設定をJSONファイルに保存");
                        ModeSettingsManager.SaveModeSettings(CurrentMode, _modeSettingsData);
                    }
                    
                    ActionLogger.LogResult("マイクフェーダー値更新完了", $"マイク{micNumber}の値を{value:0.0}に更新しました");
                    ActionLogger.LogProcessingComplete("マイクフェーダー値更新");
                }
            }
        }

        /// <summary>
        /// 出力フェーダー値が変更されたときの処理
        /// </summary>
        private void OutputFaderValues_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null && e.NewStartingIndex >= 0 && e.NewStartingIndex < OutputFaderValues.Count)
            {
                int outputNumber = e.NewStartingIndex + 1;
                if (e.NewItems.Count > 0 && e.NewItems[0] is double value)
                {
                    ActionLogger.LogAction("出力フェーダー値変更", $"出力番号: {outputNumber}, 値: {value:0.0}");
                    
                    ActionLogger.LogProcessingStart("出力フェーダー値更新", $"出力{outputNumber}の値を{value:0.0}に更新");
                    
                    string modeName = CurrentModeName;
                    ActionLogger.LogProcessing("出力フェーダー値の保存", $"モード: {modeName}に保存");
                    SaveCurrentOutputFaderValuesToEquipmentSettings();
                    
                    if (_modeSettingsData != null && CurrentMode != 0)
                    {
                        ActionLogger.LogProcessing("JSONファイルへの保存", $"モード: {modeName}の設定をJSONファイルに保存");
                        ModeSettingsManager.SaveModeSettings(CurrentMode, _modeSettingsData);
                    }
                    
                    ActionLogger.LogResult("出力フェーダー値更新完了", $"出力{outputNumber}の値を{value:0.0}に更新しました");
                    ActionLogger.LogProcessingComplete("出力フェーダー値更新");
                }
            }
        }


        /// <summary>
        /// 現在のモードのフェーダー値を保存（メモリ上に保存）
        /// </summary>
        public void SaveCurrentFaderValuesToEquipmentSettings()
        {
            if (_modeSettingsData == null || CurrentMode == 0) return;

            if (MicrophoneFaderValues.Count >= 4)
            {
                _modeSettingsData.MicrophoneFaderValues = new double[4]
                {
                    MicrophoneFaderValues[0],
                    MicrophoneFaderValues[1],
                    MicrophoneFaderValues[2],
                    MicrophoneFaderValues[3]
                };
            }
            else
            {
                var values = new double[4];
                for (int i = 0; i < 4; i++)
                {
                    values[i] = i < MicrophoneFaderValues.Count ? MicrophoneFaderValues[i] : 50.0;
                }
                _modeSettingsData.MicrophoneFaderValues = values;
            }
        }

        /// <summary>
        /// 現在のモードの出力フェーダー値を保存（メモリ上に保存）
        /// </summary>
        public void SaveCurrentOutputFaderValuesToEquipmentSettings()
        {
            if (_modeSettingsData == null || CurrentMode == 0) return;

            // 出力は2個のみなので、最初の2個だけを保存
            if (OutputFaderValues.Count >= 2)
            {
                _modeSettingsData.OutputFaderValues = new double[2]
                {
                    OutputFaderValues[0],
                    OutputFaderValues[1]
                };
            }
            else
            {
                // 2個未満の場合は、不足分を50.0で埋める
                var values = new double[2];
                for (int i = 0; i < 2; i++)
                {
                    values[i] = i < OutputFaderValues.Count ? OutputFaderValues[i] : 50.0;
                }
                _modeSettingsData.OutputFaderValues = values;
            }
        }

        /// <summary>
        /// JSONファイルからフェーダー値を初期化
        /// </summary>
        private void InitializeFaderValuesFromJson()
        {
            if (_modeSettingsData == null)
            {
                ActionLogger.LogError("フェーダー値初期化", "_modeSettingsDataがnullです");
                return;
            }
            
            // イベントハンドラを一時的に解除して、読み込み時のイベント発火を防ぐ
            MicrophoneFaderValues.CollectionChanged -= MicrophoneFaderValues_CollectionChanged;
            OutputFaderValues.CollectionChanged -= OutputFaderValues_CollectionChanged;
            
            try
            {
                // マイクフェーダー値の初期化（JSONファイルの値を使用）
                if (_modeSettingsData.MicrophoneFaderValues != null && 
                    _modeSettingsData.MicrophoneFaderValues.Length >= 4)
                {
                    if (_modeSettingsData.MicrophoneFaderValues.Length > 4)
                    {
                        ActionLogger.LogProcessing("マイクフェーダー値修正", $"8個の値が検出されました。最初の4個のみを使用します。");
                    }
                    
                    for (int i = 0; i < 4; i++)
                    {
                        double value = _modeSettingsData.MicrophoneFaderValues[i];
                        MicrophoneFaderValues.Add(value);
                        ActionLogger.LogProcessing("マイクフェーダー値設定", $"インデックス{i}: {value}");
                    }
                }
                else
                {
                    ActionLogger.LogProcessing("マイクフェーダー値初期化", "JSONファイルに値が存在しないため、50.0で初期化");
                    // JSONファイルに値が存在しない場合は50.0で初期化
                    for (int i = 0; i < 4; i++)
                    {
                        MicrophoneFaderValues.Add(50.0);
                    }
                }

                // 出力フェーダー値の初期化（JSONファイルの値を使用）
                if (_modeSettingsData.OutputFaderValues != null && 
                    _modeSettingsData.OutputFaderValues.Length >= 2)
                {
                    if (_modeSettingsData.OutputFaderValues.Length > 2)
                    {
                        ActionLogger.LogProcessing("出力フェーダー値修正", $"{_modeSettingsData.OutputFaderValues.Length}個の値が検出されました。最初の2個のみを使用します。");
                    }
                    
                    for (int i = 0; i < 2; i++)
                    {
                        double value = _modeSettingsData.OutputFaderValues[i];
                        OutputFaderValues.Add(value);
                        ActionLogger.LogProcessing("出力フェーダー値設定", $"インデックス{i}: {value}");
                    }
                }
                else
                {
                    ActionLogger.LogProcessing("出力フェーダー値初期化", "JSONファイルに値が存在しないため、50.0で初期化");
                    // JSONファイルに値が存在しない場合は50.0で初期化
                    for (int i = 0; i < 2; i++)
                    {
                        OutputFaderValues.Add(50.0);
                    }
                }
            }
            finally
            {
                // イベントハンドラを再設定
                MicrophoneFaderValues.CollectionChanged += MicrophoneFaderValues_CollectionChanged;
                OutputFaderValues.CollectionChanged += OutputFaderValues_CollectionChanged;
            }
        }

        /// <summary>
        /// JSONファイルからミュート状態を初期化（初回のみ）
        /// </summary>
        private void InitializeMuteStatesFromJson()
        {
            if (_modeSettingsData == null) return;
            
            // イベントハンドラを一時的に解除して、読み込み時のイベント発火を防ぐ
            MicrophoneMuteStates.CollectionChanged -= MicrophoneMuteStates_CollectionChanged;
            OutputMuteStates.CollectionChanged -= OutputMuteStates_CollectionChanged;
            
            try
            {
                // 出力ミュート状態の初期化（JSONファイルの値を使用）
                // 出力は2個のみなので、4個保存されている場合は最初の2個だけを使用
                if (_modeSettingsData.OutputMuteStates != null && 
                    _modeSettingsData.OutputMuteStates.Length >= 2)
                {
                    // 4個保存されている場合は最初の2個だけを使用
                    if (_modeSettingsData.OutputMuteStates.Length > 2)
                    {
                        ActionLogger.LogProcessing("出力ミュート状態修正", $"{_modeSettingsData.OutputMuteStates.Length}個の値が検出されました。最初の2個のみを使用します。");
                    }
                    
                    for (int i = 0; i < 2; i++)
                    {
                        OutputMuteStates.Add(_modeSettingsData.OutputMuteStates[i]);
                    }
                }
                else
                {
                    // JSONファイルに値が存在しない場合はfalseで初期化
                    for (int i = 0; i < 2; i++)
                    {
                        OutputMuteStates.Add(false);
                    }
                }

                // マイクミュート状態の初期化（JSONファイルの値を使用）
                if (_modeSettingsData.MicrophoneMuteStates != null && 
                    _modeSettingsData.MicrophoneMuteStates.Length == 4)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        MicrophoneMuteStates.Add(_modeSettingsData.MicrophoneMuteStates[i]);
                    }
                }
                else
                {
                    // JSONファイルに値が存在しない場合はfalseで初期化
                    for (int i = 0; i < 4; i++)
                    {
                        MicrophoneMuteStates.Add(false);
                    }
                }
            }
            finally
            {
                // イベントハンドラを再設定
                MicrophoneMuteStates.CollectionChanged += MicrophoneMuteStates_CollectionChanged;
                OutputMuteStates.CollectionChanged += OutputMuteStates_CollectionChanged;
            }
        }

        /// <summary>
        /// JSONから読み込んだフェーダー・ミュート値を機材へ反映
        /// </summary>
        private void ApplyStoredSettingsToHardware()
        {
            // UDP送信機能は削除されました
        }


        /// <summary>
        /// マイクミュート状態が変更されたときの処理
        /// </summary>
        private void MicrophoneMuteStates_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null && e.NewStartingIndex >= 0 && e.NewStartingIndex < MicrophoneMuteStates.Count)
            {
                int micNumber = e.NewStartingIndex + 1;
                if (e.NewItems.Count > 0 && e.NewItems[0] is bool isMuted)
                {
                    ActionLogger.LogAction("マイクミュート状態変更", $"マイク番号: {micNumber}, ミュート: {(isMuted ? "ON" : "OFF")}");
                    
                    ActionLogger.LogProcessingStart("マイクミュート状態更新", $"マイク{micNumber}のミュートを{(isMuted ? "ON" : "OFF")}に更新");
                    
                    // UDP送信機能は削除されました
                    
                    string modeName = CurrentModeName;
                    ActionLogger.LogProcessing("ミュート状態の保存", $"モード: {modeName}に保存");
                    SaveCurrentMuteStatesToEquipmentSettings();
                    
                    if (_modeSettingsData != null && CurrentMode != 0)
                    {
                        ActionLogger.LogProcessing("JSONファイルへの保存", $"モード: {modeName}の設定をJSONファイルに保存");
                        ModeSettingsManager.SaveModeSettings(CurrentMode, _modeSettingsData);
                    }
                    
                    ActionLogger.LogResult("マイクミュート状態更新完了", $"マイク{micNumber}のミュートを{(isMuted ? "ON" : "OFF")}に更新しました");
                    ActionLogger.LogProcessingComplete("マイクミュート状態更新");
                }
            }
        }

        /// <summary>
        /// 出力ミュート状態が変更されたときの処理
        /// </summary>
        private void OutputMuteStates_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null && e.NewStartingIndex >= 0 && e.NewStartingIndex < OutputMuteStates.Count)
            {
                int outputNumber = e.NewStartingIndex + 1;
                if (e.NewItems.Count > 0 && e.NewItems[0] is bool isMuted)
                {
                    ActionLogger.LogAction("出力ミュート状態変更", $"出力番号: {outputNumber}, ミュート: {(isMuted ? "ON" : "OFF")}");
                    
                    ActionLogger.LogProcessingStart("出力ミュート状態更新", $"出力{outputNumber}のミュートを{(isMuted ? "ON" : "OFF")}に更新");
                    
                    // UDP送信機能は削除されました
                    
                    string modeName = CurrentModeName;
                    ActionLogger.LogProcessing("ミュート状態の保存", $"モード: {modeName}に保存");
                    SaveCurrentMuteStatesToEquipmentSettings();
                    
                    if (_modeSettingsData != null && CurrentMode != 0)
                    {
                        ActionLogger.LogProcessing("JSONファイルへの保存", $"モード: {modeName}の設定をJSONファイルに保存");
                        ModeSettingsManager.SaveModeSettings(CurrentMode, _modeSettingsData);
                    }
                    
                    ActionLogger.LogResult("出力ミュート状態更新完了", $"出力{outputNumber}のミュートを{(isMuted ? "ON" : "OFF")}に更新しました");
                    ActionLogger.LogProcessingComplete("出力ミュート状態更新");
                }
            }
        }

        /// <summary>
        /// 現在のモードのミュート状態を保存（メモリ上に保存）
        /// </summary>
        public void SaveCurrentMuteStatesToEquipmentSettings()
        {
            if (_modeSettingsData == null || CurrentMode == 0) return;

            // コレクションのサイズを確認してから配列に変換
            if (OutputMuteStates.Count >= 2)
            {
                _modeSettingsData.OutputMuteStates = new bool[2] 
                { 
                    OutputMuteStates[0], 
                    OutputMuteStates[1] 
                };
            }
            else
            {
                // サイズが不足している場合はfalseで初期化
                _modeSettingsData.OutputMuteStates = new bool[2] { false, false };
            }

            if (MicrophoneMuteStates.Count >= 4)
            {
                _modeSettingsData.MicrophoneMuteStates = new bool[4] 
                { 
                    MicrophoneMuteStates[0], 
                    MicrophoneMuteStates[1], 
                    MicrophoneMuteStates[2], 
                    MicrophoneMuteStates[3] 
                };
            }
            else
            {
                // サイズが不足している場合はfalseで初期化
                _modeSettingsData.MicrophoneMuteStates = new bool[4] { false, false, false, false };
            }
        }

        /// <summary>
        /// 指定されたモードの設定データを取得
        /// </summary>
        public ModeSettingsData? GetModeSettingsData(int modeNumber)
        {
            if (_allModeSettings.ContainsKey(modeNumber))
            {
                return _allModeSettings[modeNumber];
            }
            return null;
        }

    }
}

