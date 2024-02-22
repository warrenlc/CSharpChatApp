using System;
using System.Windows.Input;

namespace ChatApp.ViewModel.Command
{
    internal class StartClientCommand : ICommand
    {

        public event EventHandler? CanExecuteChanged;
        private MainWindowViewModel? parent = null;

        public StartClientCommand(MainWindowViewModel? parent)
        {
            this.parent = parent;
        }

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            parent?.JoinSession();
        }

    }
}
