using ChatApp.Model;
using ChatApp.View;
using ChatApp.ViewModel.Command;
using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Input;

namespace ChatApp.ViewModel
{
    class MainWindowViewModel : INotifyPropertyChanged
    {
        private bool buttonEnabled = true;
        private string username;
        private string port;
        private string ipAddress;
        private ICommand startServer;
        private ICommand startClient;

        public MainWindowViewModel()
        {
            ChatWindowViewModel.OnClientHasLeft += EnableButton;
        }

        public string Username
        {
            get { return username; }
            set
            {
                username = value;
                OnPropertyChanged(nameof(username));
            }
        }

        public string IPAddress_
        {
            get { return ipAddress; }
            set
            {
                ipAddress = value;
                OnPropertyChanged(nameof(ipAddress));
            }
        }

        public string Port
        {
            get { return port; }
            set
            {
                port = value;
                OnPropertyChanged(nameof(port));
            }
        }

        public ICommand StartServer
        {
            get
            {
                return startServer ??= new StartServerCommand(this);

            }
            set
            {
                startServer = value;
            }
        }


        public ICommand StartClient
        {
            get
            {
                return startClient ??= new StartClientCommand(this);
            }
            set
            {
                startClient = value;
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged;

        public async void StartSession()
        {
            if (!IsValidIpAddress(IPAddress_))
            {
                _ = MessageBox.Show($"Inalid IPv4 Address.\nEither your address ( {IPAddress_} ) is in the wrong format or the address is not available to you",
                                    "INVALID IP", MessageBoxButton.OK);
                return;
            }

            if (IPAddress_ == null || IPAddress_ == "")
            {
                IPAddress_ = "127.0.0.1";
            }

            User.Username = Username;
            IPAddress ip = IPAddress.Parse(IPAddress_);
            int port = Convert.ToInt16(Port);

            if (!NetworkManager.IsPortAvailable(port)) 
            {
                _ = MessageBox.Show("Port is already in use", "Port Unavailable", MessageBoxButton.OK);
                return;
            }

            NetworkManager.StartServer(port, ip);
            ChatWindow chatWindow = new();
            chatWindow.Show();
            User.FetchSavedMessages();
        }


        public void JoinSession()
        {
            if (!IsValidIpAddress(IPAddress_))
            {
                _ = MessageBox.Show($"Invalid IPv4 Address. \nEither your address ( {IPAddress_} ) is in the wrong format or the address is not available to you",
                                    "INVALID IP", MessageBoxButton.OK);
                return;
            }

            if (!buttonEnabled)
            {
                _ = MessageBox.Show("Server is handling another client", "Server Busy", MessageBoxButton.OK);
                return;
            }

            if (IPAddress_ == null || IPAddress_ == "")
            {
                IPAddress_ = "127.0.0.1";
            }

            if (!NetworkManager.IsServerListening(IPAddress.Parse(IPAddress_), Convert.ToInt16(Port)))
            {
                _ = MessageBox.Show("Server is not running", "No Connection", MessageBoxButton.OK);
                return;
            }

            User.Username = Username;
            NetworkManager.SendRequest(Convert.ToInt16(Port), Username);
            buttonEnabled = false;
            ChatWindow chatWindow = new();
            chatWindow.Hide();
            IPAddress_ = "";
        }


        public void EnableButton()
        {
            buttonEnabled = true;
        }


        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public static bool IsValidIpAddress(string ip)
        {
            if (ip == null || ip == "")
            {
                // We will simply choose a default when we connect of 127.0.0.1
                return true;
            }

            // Split the strinbg and see if we get 4 parts for an Ipv4 address
            string[] splitValues = ip.Split('.');
            if (splitValues.Length != 4)
            {
                return false;
            }

            IPAddress? iPAddress = splitValues.All(r => byte.TryParse(r, out byte tempForParsing)) ? IPAddress.Parse(ip) : null;
            if (iPAddress == null)
            {
                return false;
            }

            // Check if the address is in the list of available addresses or 127.0.0.1
            IPAddress[] addresses = Dns.GetHostAddresses(Dns.GetHostName());
            addresses = addresses.Append(IPAddress.Parse("127.0.0.1")).ToArray();
            
            if (!Array.Exists(addresses, ip => ip.Equals(iPAddress)))
            {
                return false;
            }
            return true;
        }
    }
}
