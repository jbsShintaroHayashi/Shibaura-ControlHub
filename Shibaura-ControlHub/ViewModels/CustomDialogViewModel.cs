using System.Windows;
using System.Windows.Input;
using Shibaura_ControlHub.Views;

namespace Shibaura_ControlHub.ViewModels
{
    /// <summary>
    /// カスタムダイアログのViewModel
    /// </summary>
    public class CustomDialogViewModel : BaseViewModel
    {
        private readonly CustomDialog _dialog;
        private MessageBoxResult _result = MessageBoxResult.None;

        public string Title { get; }
        public string Message { get; }
        public bool ShowYesNo { get; }
        public bool ShowOk => !ShowYesNo;

        public MessageBoxResult Result => _result;

        public ICommand YesCommand { get; }
        public ICommand NoCommand { get; }
        public ICommand OkCommand { get; }

        public CustomDialogViewModel(CustomDialog dialog, string title, string message, bool showYesNo)
        {
            _dialog = dialog;
            Title = title;
            Message = message;
            ShowYesNo = showYesNo;

            YesCommand = new RelayCommand(() =>
            {
                _result = MessageBoxResult.Yes;
                _dialog.DialogResult = true;
                _dialog.Close();
            });

            NoCommand = new RelayCommand(() =>
            {
                _result = MessageBoxResult.No;
                _dialog.DialogResult = false;
                _dialog.Close();
            });

            OkCommand = new RelayCommand(() =>
            {
                _result = MessageBoxResult.OK;
                _dialog.DialogResult = true;
                _dialog.Close();
            });
        }
    }
}

