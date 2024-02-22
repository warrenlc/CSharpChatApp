using System;
using System.Windows.Input;

namespace ChatApp.ViewModel.Command
{
    internal class AlertCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        private ChatWindowViewModel? parent = null;

        public AlertCommand(ChatWindowViewModel? parent)
        {
            this.parent = parent;
        }

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            parent?.Alert_();
        }
    }
 
}
