using System.Windows;
using System.Windows.Input;
using Shibaura_ControlHub.ViewModels;

namespace Shibaura_ControlHub.Views
{
    /// <summary>
    /// CustomDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class CustomDialog : Window
    {
        public CustomDialogViewModel ViewModel { get; }

        public CustomDialog(string title, string message, bool showYesNo = false)
        {
            InitializeComponent();
            ViewModel = new CustomDialogViewModel(this, title, message, showYesNo);
            DataContext = ViewModel;
        }

        public static MessageBoxResult Show(string message, string title, MessageBoxButton button, MessageBoxImage icon)
        {
            bool showYesNo = button == MessageBoxButton.YesNo;
            var dialog = new CustomDialog(title, message, showYesNo);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            dialog.ShowDialog();
            return dialog.ViewModel.Result;
        }
    }
}

