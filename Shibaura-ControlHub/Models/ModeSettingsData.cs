using System.Collections.Generic;

namespace Shibaura_ControlHub.Models
{
    /// <summary>
    /// モードごとの設定データクラス
    /// Mode1、Mode2、Mode3それぞれのデータを保存するデータクラス
    /// JSONファイルへの保存・読み込みに使用される
    /// </summary>
    public class ModeSettingsData
    {
        /// <summary>
        /// マイク入力フェーダー値（4個分）
        /// </summary>
        public double[] MicrophoneFaderValues { get; set; } = new double[4];

        /// <summary>
        /// 出力フェーダー値（2個分）
        /// </summary>
        public double[] OutputFaderValues { get; set; } = new double[2];

        /// <summary>
        /// 出力ミュート状態（2個分：S1、S2）
        /// </summary>
        public bool[] OutputMuteStates { get; set; } = new bool[2];

        /// <summary>
        /// マイクミュート状態（4個分：M1、M2、M3、M4）
        /// </summary>
        public bool[] MicrophoneMuteStates { get; set; } = new bool[4];


        /// <summary>
        /// スイッチャーマトリクスの選択（行ごとに選択された列）
        /// 例: SwitcherSelections[1] = 3 (行1は列3が選択されている)
        /// </summary>
        public Dictionary<int, int> SwitcherSelections { get; set; } = new Dictionary<int, int>();

        /// <summary>
        /// スイッチャープリセット（プリセット番号ごと、行ごとの選択）
        /// 例: SwitcherPresets[1][1] = 3 (プリセット1、行1は列3が選択されている)
        /// </summary>
        public Dictionary<int, Dictionary<int, int>> SwitcherPresets { get; set; } 
            = new Dictionary<int, Dictionary<int, int>>();

        /// <summary>
        /// Esportsマトリクスの選択（行ごとに選択された列）
        /// 例: EsportsSelections[1] = 3 (行1は列3が選択されている)
        /// </summary>
        public Dictionary<int, int> EsportsSelections { get; set; } = new Dictionary<int, int>();

        /// <summary>
        /// Esportsプリセット（プリセット番号ごと、行ごとの選択）
        /// 例: EsportsPresets[1][1] = 3 (プリセット1、行1は列3が選択されている)
        /// </summary>
        public Dictionary<int, Dictionary<int, int>> EsportsPresets { get; set; } 
            = new Dictionary<int, Dictionary<int, int>>();

        /// <summary>
        /// 録画マトリクスの選択（行ごとに選択された列）
        /// 例: RecordingSelections[1] = 3 (録画1は列3が選択されている)
        /// </summary>
        public Dictionary<int, int> RecordingSelections { get; set; } = new Dictionary<int, int>();

        /// <summary>
        /// 選択中のスイッチャープリセット番号
        /// </summary>
        public int SelectedSwitcherPresetNumber { get; set; } = 0;

        /// <summary>
        /// 選択中の照明プリセット番号
        /// </summary>
        public int SelectedLightingPresetNumber { get; set; } = 0;

        /// <summary>
        /// 選択中のEsportsプリセット番号
        /// </summary>
        public int SelectedEsportsPresetNumber { get; set; } = 0;

        /// <summary>
        /// キャプチャマトリクスの選択（行ごとに選択された列）
        /// 例: CaptureSelections[1] = 3 (行1は列3が選択されている)
        /// </summary>
        public Dictionary<int, int> CaptureSelections { get; set; } = new Dictionary<int, int>();

        /// <summary>
        /// カメラごとの選択中のプリセット番号
        /// 例: CameraSelectedPresetNumbers["PTZカメラ-1"] = 3 (カメラ1はプリセット3が選択されている)
        /// </summary>
        public Dictionary<string, int> CameraSelectedPresetNumbers { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// カメラごとの呼び出し済みプリセット番号
        /// 例: CameraCalledPresetNumbers["PTZカメラ-1"] = 3 (カメラ1はプリセット3が呼び出し済み)
        /// </summary>
        public Dictionary<string, int> CameraCalledPresetNumbers { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// カメラ1のオートフレーミング（トラッキング）のオン/オフ状態
        /// </summary>
        public bool IsTrackingOn { get; set; } = false;
    }
}

