using System;
using System.Windows;
using System.Windows.Input;
using Shibaura_ControlHub.Views;

namespace Shibaura_ControlHub.ViewModels
{
    public class PowerOnViewModel
    {
        public ICommand PowerOnCommand { get; }

        public event Action? PowerOnRequested;

        public PowerOnViewModel()
        {
            PowerOnCommand = new RelayCommand(_ => ExecutePowerOn());
        }

        private void ExecutePowerOn()
        {
            var result = CustomDialog.Show("システムの電源をオンにしますか？", "システム電源", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                PowerOnRequested?.Invoke();
            }
        }

        private class RelayCommand : ICommand
        {
            private readonly Action<object?> _execute;
            private readonly Func<object?, bool>? _canExecute;

            public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public event EventHandler? CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }

            public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

            public void Execute(object? parameter) => _execute(parameter);
        }
    }
}

