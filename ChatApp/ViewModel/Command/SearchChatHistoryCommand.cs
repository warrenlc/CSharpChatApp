using System;
using System.Windows.Input;

namespace ChatApp.ViewModel.Command
{
    internal class SearchChatHistoryCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        private ChatWindowViewModel? parent = null;

        public SearchChatHistoryCommand(ChatWindowViewModel? parent)
        { 
            this.parent = parent;
        }

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            parent?.SearchHistory_();
        }
    }
}
