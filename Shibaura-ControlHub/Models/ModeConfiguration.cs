namespace Shibaura_ControlHub.Models
{
    /// <summary>
    /// モード設定情報
    /// </summary>
    public class ModeConfiguration
    {
        /// <summary>
        /// モード1の名前
        /// </summary>
        public string Mode1Name { get; set; } = "授業";

        /// <summary>
        /// モード2の名前
        /// </summary>
        public string Mode2Name { get; set; } = "遠隔";

        /// <summary>
        /// モード3の名前
        /// </summary>
        public string Mode3Name { get; set; } = "e-sports";

       /// <summary>
        /// モード1でマイクが利用可能か
        /// </summary>
        public bool Mode1MicrophoneAvailable => true;

        /// <summary>
        /// モード1でカメラが利用可能か
        /// </summary>
        public bool Mode1CameraAvailable => true;

        /// <summary>
        /// モード1でスイッチャーが利用可能か
        /// </summary>
        public bool Mode1SwitcherAvailable => true;

        /// <summary>
        /// モード2でマイクが利用可能か
        /// </summary>
        public bool Mode2MicrophoneAvailable => true;

        /// <summary>
        /// モード2でカメラが利用可能か
        /// </summary>
        public bool Mode2CameraAvailable => true;

        /// <summary>
        /// モード2でスイッチャーが利用可能か
        /// </summary>
        public bool Mode2SwitcherAvailable => true;

        /// <summary>
        /// モード2で映像録画が利用可能か
        /// </summary>
        public bool Mode2RecordingAvailable => true;

        /// <summary>
        /// モード3でマイクが利用可能か
        /// </summary>
        public bool Mode3MicrophoneAvailable => true;

        /// <summary>
        /// モード3でカメラが利用可能か
        /// </summary>
        public bool Mode3CameraAvailable => true;

        /// <summary>
        /// モード3でスイッチャーが利用可能か
        /// </summary>
        public bool Mode3SwitcherAvailable => true;

        /// <summary>
        /// モード3で映像録画が利用可能か
        /// </summary>
        public bool Mode3RecordingAvailable => true;

        /// <summary>
        /// プログレスバーが0%から100%まで到達するまでの時間（秒）
        /// </summary>
        public int ProgressDurationSeconds { get; set; } = 2;
    }
}

