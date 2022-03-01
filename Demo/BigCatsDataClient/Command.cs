using System;
using System.Windows.Input;

namespace BigCatsDataClient
{
    public class Command : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        private Action<object?> _execute;
        private Func<object?, bool> _canExecute;

        public Command(Action<object?> execute, Func<object?, bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute.Invoke(parameter);
        }

        public void Execute(object? parameter)
        {
            _execute.Invoke(parameter);
        }

        public void Touch()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
