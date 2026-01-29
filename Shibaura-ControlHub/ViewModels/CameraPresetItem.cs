using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Shibaura_ControlHub.ViewModels
{
    /// <summary>
    /// カメラプリセットのUI表示用アイテム
    /// </summary>
    public class CameraPresetItem : INotifyPropertyChanged
    {
        private bool _isRegistered;
        private int _number;

        public int Number
        {
            get => _number;
            set
            {
                if (_number != value)
                {
                    _number = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public bool IsRegistered
        {
            get => _isRegistered;
            set
            {
                _isRegistered = value;
                OnPropertyChanged();
            }
        }
        
        public ICommand CallCommand { get; set; } = null!;
        public ICommand RegisterCommand { get; set; } = null!;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

