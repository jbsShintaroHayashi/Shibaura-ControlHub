using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Shibaura_ControlHub.ViewModels
{
    public class ProgressViewModel : INotifyPropertyChanged
    {
        private string _message = "処理中です...";
        private bool _isIndeterminate = true;
        private double _progressValue;

        public string Message
        {
            get => _message;
            set { _message = value; OnPropertyChanged(); }
        }

        public bool IsIndeterminate
        {
            get => _isIndeterminate;
            set { _isIndeterminate = value; OnPropertyChanged(); }
        }

        public double ProgressValue
        {
            get => _progressValue;
            set { _progressValue = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

