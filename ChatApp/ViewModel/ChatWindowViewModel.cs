using ChatApp.Exceptions;
using ChatApp.Model;
using ChatApp.View;
using ChatApp.ViewModel.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Input;

namespace ChatApp.ViewModel
{
    public class ChatWindowViewModel : INotifyPropertyChanged
    {
        private readonly string TITLE = "A - Hee - Hee";
        private readonly string ALERT = "A - Hee - Hee - Hee!";
        private readonly static string ALERT_FILE = "michael-jackson_07.wav";
        private readonly string ALERT_FILE_PATH = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Sound", ALERT_FILE);

        private ICommand sendMessage;
        private ICommand closeChat;
        private ICommand searchChatHistory;
        private ICommand alert;
        private string usernameMe;
        private string usernameOther;

        private readonly ChatWindow window;
        private string nameToSearch;
        private string message;

        public ObservableCollection<Message> Messages { get; }
        public ObservableCollection<Message> History { get; set; }

        public static event Action? OnClientHasLeft;


        public string Message
        {
            get { return message; }
            set
            {
                message = value;
                OnPropertyChanged(nameof(message));
            }
        }

        public string UsernameMe
        {
            get { return usernameMe; }
            set
            {
                usernameMe = User.Username;
            }
        }

        public string UsernameOther
        {
            get { return usernameOther; }
            set
            {
                if (usernameOther != value)
                {
                    usernameOther = value;
                    OnPropertyChanged(nameof(UsernameOther));
                }
            }
        }


        public string NameToSearch
        {
            get { return nameToSearch; }
            set
            {
                if (nameToSearch != value)
                {
                    nameToSearch = value;
                    OnPropertyChanged(nameof(nameToSearch));
                }
            }

        }


        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public ChatWindowViewModel(ChatWindow window)
        {
            this.window = window;
            UsernameMe = UsernameMe;
            UsernameOther = UsernameOther;
            Messages = new ObservableCollection<Message>();
            History = new ObservableCollection<Message>();

            NetworkManager.OnConnectRequest += RenderApproveDenyConnectionDialog;
            NetworkManager.OnRequestAccepted += RenderRequestApprovedDialog;
            NetworkManager.OnRequestDenied += RenderRequestDeniedDialog;
            NetworkManager.OnMessageReceived += UpdateMessageFeed;
            NetworkManager.OnConnectionClosed += RenderSessionClosedDialog;
            NetworkManager.OnAlertReceived += EnactAlert;
            NetworkManager.OnServerKilledChat += TerminateSession;

            window.Closed += OnClosed;
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            NetworkManager.OnConnectRequest -= RenderApproveDenyConnectionDialog;
            NetworkManager.OnRequestAccepted -= RenderRequestApprovedDialog;
            NetworkManager.OnRequestDenied -= RenderRequestDeniedDialog;
            NetworkManager.OnMessageReceived -= UpdateMessageFeed;
            NetworkManager.OnConnectionClosed -= RenderSessionClosedDialog;
            NetworkManager.OnAlertReceived -= EnactAlert;
            NetworkManager.OnServerKilledChat -= TerminateSession;

            User.SaveMessages();
            window.Closed -= OnClosed;
            window.DataContext = null;
            CloseChat_();
        }

        public ICommand CloseChat
        {
            get
            {
                return closeChat ??= new CloseChatCommand(this);
            }
            set
            {
                closeChat = value;
            }
        }

        public ICommand SendMessage
        {
            get
            {
                return sendMessage ??= new SendMessageCommand(this);
            }
            set
            {
                sendMessage = value;
            }
        }

        public ICommand SearchChatHistory
        {
            get
            {
                return searchChatHistory ??= new SearchChatHistoryCommand(this);
            }
            set
            {
                searchChatHistory = value;
            }
        }

        public ICommand Alert
        {
            get
            {
                return alert ??= new AlertCommand(this);
            }
            set
            {
                alert = value;
            }
        }


        public void SendMessage_()
        {
            try
            {
                if (!NetworkManager.IsClient())
                {
                    throw (new NotConnectedException("Cannot send message if not connected to a client"));
                }
                NetworkManager.SendStringAsMessage(message, usernameOther);
                Message messageInstance = NetworkManager.ConvertStringToMessage(message, usernameOther);
                UpdateMessageFeed(messageInstance);
                Message = "";
            }
            catch (NotConnectedException nce)
            {
                _ = MessageBox.Show(nce.Message, "Not connected", MessageBoxButton.OK);
            }
        }


        private void UpdateMessageFeed(Message message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Messages.Add(message);
            });
        }

        public void RenderRequestDeniedDialog(string username)
        {
            _ = MessageBox.Show($"Your request was denied by user {username}", "DENIED", MessageBoxButton.OK);
            Application.Current.Dispatcher.Invoke(() =>
            {
                window.Close();
            }); 
        }

        public void RenderRequestApprovedDialog(string username)
        {
            _ = MessageBox.Show($"Your request was accepted by user {username}", "APPROVED", MessageBoxButton.OK);
            Application.Current?.Dispatcher.Invoke(() =>
            {
                UsernameOther = username;
                User.ChattingWith = username;
                window.Show();
                User.FetchSavedMessages();
            });
            
        }


        public void RenderApproveDenyConnectionDialog(string user)
        {

            MessageBoxResult result = MessageBox.Show($"Do you want to accept incoming connection from {user}?", "NEW CONNECTION", MessageBoxButton.YesNo);
            switch (result)
            {
                case MessageBoxResult.Yes:
                    UsernameOther = user;
                    User.ChattingWith = user;
                    NetworkManager.SendConnectRequestResponse(true, UsernameMe);
                    break;
                case MessageBoxResult.No:
                    NetworkManager.SendConnectRequestResponse(false, UsernameMe);
                    break;
                default:
                    break;
            }

        }

        private void EnactAlert(string alert)
        {
            SoundPlayer player = new(ALERT_FILE_PATH);
            player.Play();
            _ = MessageBox.Show(alert, TITLE, MessageBoxButton.OK);
        }


        public void RenderSessionClosedDialog(string user)
        {
            _ = MessageBox.Show($"User {user} has left the chat.", "User has left", MessageBoxButton.OK);
        }

        public void SearchHistory_()
        {
            List<Message> messages = User.FindConversationsFromSearchTerm(NameToSearch);
            messages.Reverse();
            foreach (Message message in messages)
            {
                History.Add(message);
            }
        }

        public void CloseChat_()
        {
            if (NetworkManager.IsServer())
            {
                NetworkManager.KillClient();
            }
            NetworkManager.SendCloseConnection();
            NetworkManager.StopListeningForMessagesThread();
            NetworkManager.CloseServerIfServer();
            NetworkManager.CloseClient();
            window.Close();
            OnClientHasLeft?.Invoke();
        }

        public void Alert_()
        {
            try
            {
                if (!NetworkManager.IsClient())
                {
                    throw (new NotConnectedException("Cannot alert if not connected to a client"));
                }
                NetworkManager.SendAlertMessage(ALERT);
            } catch(NotConnectedException nce)
            {
                _ = MessageBox.Show(nce.Message, "Not connected", MessageBoxButton.OK);
            }
          
        }

        public void TerminateSession()
        {
            _ = MessageBox.Show($"Server has been terminated. Chat session will now close.", "Server terminated", MessageBoxButton.OK);
            window.Close();
        }
    }
}


