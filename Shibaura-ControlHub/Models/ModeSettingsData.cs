using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Shibaura_ControlHub.Models
{
    /// <summary>モードごとの設定（JSON保存用）</summary>
    public class ModeSettingsData
    {
        /// <summary>マイクフェーダー4ch（DSP取得のためJSON非保存）</summary>
        [JsonIgnore]
        public double[] MicrophoneFaderValues { get; set; } = new double[4];

        /// <summary>出力フェーダー2ch（DSP取得のためJSON非保存）</summary>
        [JsonIgnore]
        public double[] OutputFaderValues { get; set; } = new double[2];

        /// <summary>出力ミュート2ch（DSP取得のためJSON非保存）</summary>
        [JsonIgnore]
        public bool[] OutputMuteStates { get; set; } = new bool[2];

        /// <summary>マイクミュート4ch（DSP取得のためJSON非保存）</summary>
        [JsonIgnore]
        public bool[] MicrophoneMuteStates { get; set; } = new bool[4];

        /// <summary>スイッチャー選択（行→列）</summary>
        public Dictionary<int, int> SwitcherSelections { get; set; } = new Dictionary<int, int>();

        /// <summary>スイッチャープリセット（プリセット番号→行→列）</summary>
        public Dictionary<int, Dictionary<int, int>> SwitcherPresets { get; set; }
            = new Dictionary<int, Dictionary<int, int>>();

        /// <summary>録画選択（行→列）</summary>
        public Dictionary<int, int> RecordingSelections { get; set; } = new Dictionary<int, int>();

        /// <summary>選択中スイッチャープリセット番号</summary>
        public int SelectedSwitcherPresetNumber { get; set; } = 0;

        /// <summary>カメラ別・選択中プリセット番号</summary>
        public Dictionary<string, int> CameraSelectedPresetNumbers { get; set; } = new Dictionary<string, int>();

        /// <summary>カメラ別・呼出済みプリセット番号</summary>
        public Dictionary<string, int> CameraCalledPresetNumbers { get; set; } = new Dictionary<string, int>();

        /// <summary>カメラ1トラッキングON/OFF</summary>
        public bool IsTrackingOn { get; set; } = false;
    }
}

