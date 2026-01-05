using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Shibaura_ControlHub;
using Shibaura_ControlHub.Models;
using Shibaura_ControlHub.Utils;
using Shibaura_ControlHub.Views;

namespace Shibaura_ControlHub.ViewModels
{
    /// <summary>
    /// 照明制御のViewModel
    /// </summary>
    public class LightingViewModel : BaseViewModel
    {
        private readonly Dictionary<int, ModeSettingsData> _allModeSettings = new Dictionary<int, ModeSettingsData>();
        private ModeSettingsData? _modeSettingsData;
        
        
        private int _selectedLightingPresetNumber = 0;

        /// <summary>
        /// 選択中の照明プリセット番号
        /// </summary>
        public int SelectedLightingPresetNumber
        {
            get => _selectedLightingPresetNumber;
            set
            {
                _selectedLightingPresetNumber = value;
                OnPropertyChanged();
            }
        }

        private int _calledLightingPresetNumber = 0;

        /// <summary>
        /// 呼び出し済みの照明プリセット番号
        /// </summary>
        public int CalledLightingPresetNumber
        {
            get => _calledLightingPresetNumber;
            set
            {
                _calledLightingPresetNumber = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand SelectLightingPresetCommand { get; private set; } = null!;
        public ICommand CallLightingPresetCommand { get; private set; } = null!;

        public LightingViewModel(string mode)
        {
            SetCurrentModeFromName(mode);
            
            LoadModeSettings();
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
                SelectedLightingPresetNumber = _modeSettingsData.SelectedLightingPresetNumber;
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
        /// 指定されたモードの設定を読み込む（起動時のみJSONから読み込む）
        /// </summary>
        /// <param name="mode">モード名（"授業"、"遠隔"、"e-sports"など）</param>
        private void LoadModeSettings(string mode)
        {
            int modeNumber = ModeSettingsManager.GetModeNumber(mode);
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

        /// <summary>
        /// コマンドの初期化
        /// </summary>
        private void InitializeCommands()
        {
            SelectLightingPresetCommand = new RelayCommand<int>(SelectLightingPreset);
            CallLightingPresetCommand = new RelayCommand(CallLightingPreset);
        }

        /// <summary>
        /// 照明プリセットを選択
        /// </summary>
        private void SelectLightingPreset(int presetNumber)
        {
            ActionLogger.LogAction("照明プリセット選択", $"プリセット番号: {presetNumber}");
            SelectedLightingPresetNumber = presetNumber;
            
            // プリセット選択状態を保存
            if (_modeSettingsData != null)
            {
                _modeSettingsData.SelectedLightingPresetNumber = presetNumber;
                
                // JSONファイルに即座に保存
                ModeSettingsManager.SaveModeSettings(CurrentMode, _modeSettingsData);
            }
        }

        /// <summary>
        /// 選択中の照明プリセットを呼び出し
        /// </summary>
        private void CallLightingPreset()
        {
            if (SelectedLightingPresetNumber == 0)
            {
                ActionLogger.LogError("照明プリセット呼出", "プリセットが選択されていません");
                CustomDialog.Show("プリセットが選択されていません。", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ActionLogger.LogAction("照明プリセット呼出", $"プリセット番号: {SelectedLightingPresetNumber}");
            ActionLogger.LogProcessingStart("照明プリセット処理", $"プリセット{SelectedLightingPresetNumber}を呼び出し");

            // ハードコーディングされたプリセットデータを取得
            ActionLogger.LogProcessing("プリセットデータの取得", $"プリセット{SelectedLightingPresetNumber}のデータを取得中");
            var presetSelection = GetHardcodedLightingPreset(SelectedLightingPresetNumber);
            
            if (presetSelection == null || presetSelection.Count == 0)
            {
                ActionLogger.LogError("照明プリセット呼出", $"プリセット{SelectedLightingPresetNumber}のデータが定義されていません");
                CustomDialog.Show($"プリセット{SelectedLightingPresetNumber}のデータが定義されていません。", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // UDP送信機能は削除されました
            
            // 呼び出し済みプリセット番号を設定（濃い青に変更）
            CalledLightingPresetNumber = SelectedLightingPresetNumber;
            
            ActionLogger.LogResult("照明プリセット呼出完了", $"プリセット{SelectedLightingPresetNumber}を呼び出しました");
            ActionLogger.LogProcessingComplete("照明プリセット処理");
            
            CustomDialog.Show(
                $"照明プリセット{SelectedLightingPresetNumber}を呼び出しました。",
                "プリセット呼び出し",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        /// <summary>
        /// ハードコーディングされた照明プリセットデータを取得
        /// </summary>
        private List<int>? GetHardcodedLightingPreset(int presetNumber)
        {
            // ここにプリセット1～8のデータを定義
            return presetNumber switch
            {
                1 => new List<int> { /* プリセット1のデータをここに記述 */ },
                2 => new List<int> { /* プリセット2のデータをここに記述 */ },
                3 => new List<int> { /* プリセット3のデータをここに記述 */ },
                4 => new List<int> { /* プリセット4のデータをここに記述 */ },
                5 => new List<int> { /* プリセット5のデータをここに記述 */ },
                6 => new List<int> { /* プリセット6のデータをここに記述 */ },
                7 => new List<int> { /* プリセット7のデータをここに記述 */ },
                8 => new List<int> { /* プリセット8のデータをここに記述 */ },
                _ => null
            };
        }

        /// <summary>
        /// 選択状態のみをクリア（呼び出し状態は保持）
        /// </summary>
        public void ClearLightingPresetSelectionOnly()
        {
            SelectedLightingPresetNumber = 0;

            if (_modeSettingsData != null)
            {
                _modeSettingsData.SelectedLightingPresetNumber = 0;
                ModeSettingsManager.SaveModeSettings(CurrentMode, _modeSettingsData);
            }
        }
    }
}

