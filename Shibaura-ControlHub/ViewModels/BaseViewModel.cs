using System.ComponentModel;
using System.Runtime.CompilerServices;
using Shibaura_ControlHub.Utils;

namespace Shibaura_ControlHub.ViewModels
{
    /// <summary>
    /// すべてのViewModelの基底クラス
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        protected int _currentMode = 0;

        /// <summary>
        /// 現在のモード（1、2、3の数字で扱う）
        /// </summary>
        public int CurrentMode
        {
            get => _currentMode;
            set
            {
                if (_currentMode != value)
                {
                    _currentMode = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentModeName)); // モード名も更新通知
                    OnModeChanged();
                }
            }
        }
        
        /// <summary>
        /// 現在のモード名（画面表示用）
        /// </summary>
        public string CurrentModeName => ModeSettingsManager.GetModeName(CurrentMode);
        
        /// <summary>
        /// モード名からモード番号を設定（互換性のため）
        /// </summary>
        /// <param name="modeName">モード名（"授業"、"遠隔"、"e-sports"など）</param>
        public void SetCurrentModeFromName(string modeName)
        {
            CurrentMode = ModeSettingsManager.GetModeNumber(modeName);
        }

        /// <summary>
        /// モードが変更されたときに呼ばれる
        /// </summary>
        protected virtual void OnModeChanged()
        {
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

