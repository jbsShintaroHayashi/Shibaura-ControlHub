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
using Shibaura_ControlHub.Services;
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

        /// <summary>
        /// モニタリング機器リスト（Bose DSP の送信先取得用）
        /// </summary>
        private readonly IEnumerable<EquipmentStatus>? _monitoringList;

        /// <summary>
        /// デジタルミキサ（Bose ControlSpace DSP）制御。DigitalMixerManager でフェーダー・ミュートを送信。
        /// </summary>
        private readonly DigitalMixerManager? _digitalMixerManager;

        /// <summary>
        /// DSP から状態を取得して UI に反映中は true。この間はフェーダー/ミュート変更で DSP へ送信しない。
        /// </summary>
        private bool _isUpdatingFromDsp;

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

        public MicrophoneViewModel(string mode, ObservableCollection<EquipmentStatus> microphoneList, IEnumerable<EquipmentStatus>? monitoringList = null)
        {
            MicrophoneList = microphoneList;
            _monitoringList = monitoringList;
            _digitalMixerManager = DigitalMixerManager.CreateFromMonitoringList(monitoringList);
            SetCurrentModeFromName(mode);
            
            // モード設定を読み込む（JSONファイルから常に最新の設定を読み込む）
            LoadModeSettings();
            
            int modeNumber = CurrentMode;
            if (modeNumber > 0)
            {
                // LoadModeSettings()で既に_allModeSettingsに設定が読み込まれている
                _modeSettingsData = _allModeSettings[modeNumber];
                
                if (_modeSettingsData == null) return;
                
                // 音量はJSONに保存・読み込みしない。フェーダー/ミュートはデフォルトで初期化し、DSP取得で表示する
                InitializeFaderValuesFromJson();
                
                // ミュート状態のコレクションを初期化（JSONファイルの値を使用）
                InitializeMuteStatesFromJson();
            }
            SetupFaderValueChangedHandler();
            // 音量はJSONに保存・読み込みしない。起動時はDSPから現在の状態を取得して表示する
            _ = FetchAndDisplayCurrentStateFromDspAsync();
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

            // モード切替時はDSPから現在の状態を取得して表示（クリア／70%初期化は不要。取得結果で上書きする）
            _ = FetchAndDisplayCurrentStateFromDspAsync();
        }

        /// <summary>
        /// DSP からゲイン・ミュート状態を取得し、UI に反映する。
        /// 取得できなかった場合は 0 dB 相当（70%）・ミュート解除で表示する。
        /// </summary>
        private async Task FetchAndDisplayCurrentStateFromDspAsync()
        {
            if (_digitalMixerManager == null) return;

            var result = await _digitalMixerManager.GetAllGainStateAsync().ConfigureAwait(false);

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _isUpdatingFromDsp = true;
                try
                {
                    const double DefaultPercent0Db = 70.0; // 0 dB = ユニティゲイン

                    if (result == null)
                    {
                        // 取得できなかった場合: 0 dB 相当で 70%、ミュート解除に設定
                        ApplyFaderAndMuteToUi(DefaultPercent0Db, isMuted: false, micCount: 4, outCount: 2);
                    }
                    else
                    {
                        for (int i = 0; i < 4 && i < result.VolumeDb.Length; i++)
                        {
                            double percent = DigitalMixerManager.DecibelToPercentForGain(result.VolumeDb[i]);
                            if (i < MicrophoneFaderValues.Count)
                                MicrophoneFaderValues[i] = percent;
                            else
                                MicrophoneFaderValues.Add(percent);
                        }
                        for (int i = 0; i < 2 && i + 4 < result.VolumeDb.Length; i++)
                        {
                            double percent = DigitalMixerManager.DecibelToPercentForGain(result.VolumeDb[i + 4]);
                            if (i < OutputFaderValues.Count)
                                OutputFaderValues[i] = percent;
                            else
                                OutputFaderValues.Add(percent);
                        }
                        for (int i = 0; i < 4 && i < result.IsMuted.Length; i++)
                        {
                            if (i < MicrophoneMuteStates.Count)
                                MicrophoneMuteStates[i] = result.IsMuted[i];
                            else
                                MicrophoneMuteStates.Add(result.IsMuted[i]);
                        }
                        for (int i = 0; i < 2 && i + 4 < result.IsMuted.Length; i++)
                        {
                            if (i < OutputMuteStates.Count)
                                OutputMuteStates[i] = result.IsMuted[i + 4];
                            else
                                OutputMuteStates.Add(result.IsMuted[i + 4]);
                        }
                    }
                    OnPropertyChanged(nameof(MicrophoneFaderValues));
                    OnPropertyChanged(nameof(OutputFaderValues));
                    OnPropertyChanged(nameof(MicrophoneMuteStates));
                    OnPropertyChanged(nameof(OutputMuteStates));
                }
                finally
                {
                    _isUpdatingFromDsp = false;
                }
            });
        }

        /// <summary>
        /// 指定したフェーダー％とミュート状態を全チャンネルに適用（取得失敗時の 0 dB / 70% 用）。
        /// </summary>
        private void ApplyFaderAndMuteToUi(double percent, bool isMuted, int micCount, int outCount)
        {
            for (int i = 0; i < micCount; i++)
            {
                if (i < MicrophoneFaderValues.Count)
                    MicrophoneFaderValues[i] = percent;
                else
                    MicrophoneFaderValues.Add(percent);
            }
            for (int i = 0; i < outCount; i++)
            {
                if (i < OutputFaderValues.Count)
                    OutputFaderValues[i] = percent;
                else
                    OutputFaderValues.Add(percent);
            }
            for (int i = 0; i < micCount; i++)
            {
                if (i < MicrophoneMuteStates.Count)
                    MicrophoneMuteStates[i] = isMuted;
                else
                    MicrophoneMuteStates.Add(isMuted);
            }
            for (int i = 0; i < outCount; i++)
            {
                if (i < OutputMuteStates.Count)
                    OutputMuteStates[i] = isMuted;
                else
                    OutputMuteStates.Add(isMuted);
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
            
            // 常にJSONファイルから最新の設定を読み込む
            var settings = ModeSettingsManager.LoadModeSettings(modeNumber);
            
            if (settings.MicrophoneFaderValues == null || settings.MicrophoneFaderValues.Length == 0)
                settings.MicrophoneFaderValues = new double[4] { 50.0, 50.0, 50.0, 50.0 };
            if (settings.OutputFaderValues == null || settings.OutputFaderValues.Length == 0)
                settings.OutputFaderValues = new double[2] { 50.0, 50.0 };
            // ミュート状態はJSONファイルの値を使用（初期化しない）
            
            _allModeSettings[modeNumber] = settings;
            
            if (modeNumber == CurrentMode)
                _modeSettingsData = _allModeSettings[modeNumber];
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
            if (_isUpdatingFromDsp) return;
            if (e.NewItems != null && e.NewStartingIndex >= 0 && e.NewStartingIndex < MicrophoneFaderValues.Count)
            {
                int micNumber = e.NewStartingIndex + 1;
                if (e.NewItems.Count > 0 && e.NewItems[0] is double value)
                {
                    _ = _digitalMixerManager?.SetGainVolumeByChannelAsync(micNumber, value);
                    SaveCurrentFaderValuesToEquipmentSettings();
                }
            }
        }

        /// <summary>
        /// 出力フェーダー値が変更されたときの処理
        /// </summary>
        private void OutputFaderValues_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isUpdatingFromDsp) return;
            if (e.NewItems != null && e.NewStartingIndex >= 0 && e.NewStartingIndex < OutputFaderValues.Count)
            {
                int outputNumber = e.NewStartingIndex + 1;
                if (e.NewItems.Count > 0 && e.NewItems[0] is double value)
                {
                    int dspChannel = outputNumber + 4;
                    _ = _digitalMixerManager?.SetGainVolumeByChannelAsync(dspChannel, value);
                    SaveCurrentOutputFaderValuesToEquipmentSettings();
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
            if (_modeSettingsData == null) return;
            
            // イベントハンドラを一時的に解除して、読み込み時のイベント発火を防ぐ
            MicrophoneFaderValues.CollectionChanged -= MicrophoneFaderValues_CollectionChanged;
            OutputFaderValues.CollectionChanged -= OutputFaderValues_CollectionChanged;
            
            try
            {
                // 音量はJSONに保存・読み込みしない。ユニティ（0dB＝70%）で初期化し、DSP取得で上書きする
                const double DefaultFaderPercent = 70.0;
                for (int i = 0; i < 4; i++)
                    MicrophoneFaderValues.Add(DefaultFaderPercent);
                for (int i = 0; i < 2; i++)
                    OutputFaderValues.Add(DefaultFaderPercent);
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
                // 音量はJSONに保存・読み込みしない。ミュート解除で初期化し、DSP取得で上書きする
                for (int i = 0; i < 2; i++)
                    OutputMuteStates.Add(false);
                for (int i = 0; i < 4; i++)
                    MicrophoneMuteStates.Add(false);
            }
            finally
            {
                // イベントハンドラを再設定
                MicrophoneMuteStates.CollectionChanged += MicrophoneMuteStates_CollectionChanged;
                OutputMuteStates.CollectionChanged += OutputMuteStates_CollectionChanged;
            }
        }

        /// <summary>
        /// JSONから読み込んだフェーダー・ミュート値を DigitalMixerManager で Bose DSP へ反映
        /// </summary>
        private void ApplyStoredSettingsToHardware()
        {
            if (_digitalMixerManager == null) return;

            // マイクフェーダー CH1～4
            for (int i = 0; i < MicrophoneFaderValues.Count && i < 4; i++)
            {
                int ch = i + 1;
                _ = _digitalMixerManager.SetGainVolumeByChannelAsync(ch, MicrophoneFaderValues[i]);
            }
            // 出力フェーダー CH5～6
            for (int i = 0; i < OutputFaderValues.Count && i < 2; i++)
            {
                int ch = i + 5;
                _ = _digitalMixerManager.SetGainVolumeByChannelAsync(ch, OutputFaderValues[i]);
            }
            // マイクミュート CH1～4
            for (int i = 0; i < MicrophoneMuteStates.Count && i < 4; i++)
            {
                int ch = i + 1;
                _ = _digitalMixerManager.SetGainMuteByChannelAsync(ch, MicrophoneMuteStates[i]);
            }
            // 出力ミュート CH5～6
            for (int i = 0; i < OutputMuteStates.Count && i < 2; i++)
            {
                int ch = i + 5;
                _ = _digitalMixerManager.SetGainMuteByChannelAsync(ch, OutputMuteStates[i]);
            }
        }


        /// <summary>
        /// マイクミュート状態が変更されたときの処理
        /// </summary>
        private void MicrophoneMuteStates_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isUpdatingFromDsp) return;
            if (e.NewItems != null && e.NewStartingIndex >= 0 && e.NewStartingIndex < MicrophoneMuteStates.Count)
            {
                int micNumber = e.NewStartingIndex + 1;
                if (e.NewItems.Count > 0 && e.NewItems[0] is bool isMuted)
                {
                    _ = _digitalMixerManager?.SetGainMuteByChannelAsync(micNumber, isMuted);
                    SaveCurrentMuteStatesToEquipmentSettings();
                }
            }
        }

        /// <summary>
        /// 出力ミュート状態が変更されたときの処理
        /// </summary>
        private void OutputMuteStates_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isUpdatingFromDsp) return;
            if (e.NewItems != null && e.NewStartingIndex >= 0 && e.NewStartingIndex < OutputMuteStates.Count)
            {
                int outputNumber = e.NewStartingIndex + 1;
                if (e.NewItems.Count > 0 && e.NewItems[0] is bool isMuted)
                {
                    int dspChannel = outputNumber + 4;
                    _ = _digitalMixerManager?.SetGainMuteByChannelAsync(dspChannel, isMuted);
                    SaveCurrentMuteStatesToEquipmentSettings();
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

