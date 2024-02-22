using System;
using System.Windows.Input;

namespace ChatApp.ViewModel.Command
{
    internal class SendMessageCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        private ChatWindowViewModel? parent = null;

        public SendMessageCommand(ChatWindowViewModel? parent)
        {
            this.parent = parent;
        }

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            parent?.SendMessage_();
        }
    }
}
