using System;
using System.Globalization;
using System.Windows.Input;

namespace Shibaura_ControlHub.ViewModels
{
    /// <summary>
    /// パラメータなしのRelayCommand
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action? _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { System.Windows.Input.CommandManager.RequerySuggested += value; }
            remove { System.Windows.Input.CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter)
        {
            if (_execute == null)
                throw new InvalidOperationException("Command execution is not supported.");
            _execute.Invoke();
        }
    }

    /// <summary>
    /// パラメータ付きのRelayCommand
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?>? _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { System.Windows.Input.CommandManager.RequerySuggested += value; }
            remove { System.Windows.Input.CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            if (_canExecute == null) return true;

            if (TryConvertParameter(parameter, out var value))
            {
                return _canExecute(value);
            }

            return false;
        }

        public void Execute(object? parameter)
        {
            if (_execute == null)
            {
                throw new InvalidOperationException("Command execution is not supported.");
            }

            if (TryConvertParameter(parameter, out var value))
            {
                _execute.Invoke(value);
            }
            else
            {
                _execute.Invoke(default);
            }
        }

        private static bool TryConvertParameter(object? parameter, out T? value)
        {
            if (parameter is T directValue)
            {
                value = directValue;
                return true;
            }

            if (parameter == null)
            {
                value = default;
                return true;
            }

            try
            {
                var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

                if (parameter.GetType() == targetType || targetType.IsInstanceOfType(parameter))
                {
                    value = (T)parameter;
                    return true;
                }

                var converted = System.Convert.ChangeType(parameter, targetType, CultureInfo.InvariantCulture);
                value = (T?)converted;
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }
    }
}

