using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ChatterClient
{
    internal class AsyncCommand : ICommand
    {
        private readonly Func<Task> execute;
        private readonly Func<bool> canExecute;
        private bool isExecuting;

        public AsyncCommand(Func<Task> execute, Func<bool> canExecute)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return !(isExecuting && canExecute());
        }

        public event EventHandler CanExecuteChanged;

        public async void Execute(object parameter)
        {
            isExecuting = true;
            OnCanExecuteChanged();
            try
            {
                await execute();
            }
            finally
            {
                isExecuting = false;
                OnCanExecuteChanged();
            }
        }

        private void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
