using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace ChatApp.Model
{
    internal static class NetworkManager
    {        
        private static TcpListener? server;
        private static TcpClient? client;
        private static NetworkStream? stream;
        private static Thread? listenerThread;
        private static Thread? messageThread;
        public static int Port { get; set; }
        public static IPAddress? Host { get; set; }

        public static event Action<string>? OnConnectRequest;
        public static event Action<string>? OnRequestDenied;
        public static event Action<string>? OnRequestAccepted;
        public static event Action<string>? OnConnectionClosed;
        public static event Action<string>? OnAlertReceived;
        public static event Action? OnServerKilledChat;
        public static event Action<Message>? OnMessageReceived;

        private static CancellationTokenSource listenForMessagesCancellationTokenSource = new();
        private static CancellationTokenSource listenForClientsCancellationTokenSource = new();
        

        public static bool IsPortAvailable(int port)
        {
            bool isAvailable = true;
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipGlobalProperties.GetActiveTcpListeners();
            if (ipEndPoints.Any(endPoint => endPoint.Port == port))
            {
                isAvailable = false;
            }
            return isAvailable;
        }

        public static void StartServer(int port, IPAddress ipAddress)
        {
            listenForMessagesCancellationTokenSource = new();
            listenForClientsCancellationTokenSource = new();

            Host = ipAddress;
            Port = port;
            

            if (Host != null)
            {
                server = new(Host, Port);
                listenerThread = new Thread(ListenForClients);
                listenerThread?.Start();
            }
        }

        public static void ListenForClients()
        {
            try 
            { 
                server?.Start(); 
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting server: {ex.Message}");
                Debug.WriteLine($"Inner error: {ex.InnerException}");
            }

            CancellationToken cancellationToken = listenForClientsCancellationTokenSource.Token;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    client = server?.AcceptTcpClient();
                    stream = client?.GetStream();

                    StartListeningForMessages();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error accepting client: {ex}");
                }

            }
        }

        public static void ListenForMessages()
        {
            CancellationToken cancellationToken = listenForMessagesCancellationTokenSource.Token;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Message message;
                    byte[] messageBytes = new byte[client.ReceiveBufferSize];
                    if (stream != null)
                    {
                        int bytesRead = stream.Read(messageBytes, 0, messageBytes.Length);
                        message = DeserializeMessage(messageBytes, bytesRead);
                        Task.Factory.StartNew(() => HandleMessage(message));
                    }
                }
                catch (IOException ex)
                {
                    if (ex.InnerException is SocketException sockex && sockex.SocketErrorCode == SocketError.TimedOut)
                    {
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error receiving message: {ex.Message}");
                    Debug.WriteLine($"Inner error: {ex.InnerException}");
                }
            }
        }

        private static void HandleMessage(Message message)
        {
            switch (message.Type)
            {

                case MessageType.RequestConnection:
                    OnConnectRequest?.Invoke(message.Sender);
                    break;

                case MessageType.AcceptConnection:
                    OnRequestAccepted?.Invoke(message.Sender);
                    User.ChattingWith = message.Sender;
                    break;

                case MessageType.DenyConnection:
                    OnRequestDenied?.Invoke(message.Sender);
                    break;

                case MessageType.CloseConnection:
                    CloseClient();
                    OnConnectionClosed?.Invoke(message.Sender);
                    break;

                case MessageType.TerminateChat:
                    CloseClient();
                    OnServerKilledChat?.Invoke();
                    break;

                case MessageType.Alert:
                    OnAlertReceived?.Invoke(message.Content);
                    break;

                case MessageType.Message:
                    User.AddMessage(message);
                    OnMessageReceived?.Invoke(message);
                    break;

                default: break;
            }
        }

        public static void StartListeningForMessages()
        {
            messageThread = new Thread(ListenForMessages);
            messageThread?.Start();
        }

        public static void StopListeningForMessagesThread()
        {
            listenForMessagesCancellationTokenSource?.Cancel();
        }

        private static void SendMessage(string message)
        {
            try
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                stream?.Write(messageBytes, 0, messageBytes.Length);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending message: {ex.Message}");
                Debug.WriteLine($"Inner error: {ex.InnerException}");
            }
        }

        public static void SendRequest(int port, string username)
        {
            listenForMessagesCancellationTokenSource = new();

            TcpClient tcpClient = new(Host.ToString(), port);
            client = tcpClient;
            stream = client?.GetStream();

            Message data = new(username, "", DateTime.Now, MessageType.RequestConnection, "");
            string message = SerializeMessage(data);

            SendMessage(message);

            StartListeningForMessages();
        }

        public static void SendConnectRequestResponse(bool accepted, string responseFromUser)
        {
            MessageType response = accepted ? MessageType.AcceptConnection : MessageType.DenyConnection;
            Message message = new(User.Username, responseFromUser, DateTime.Now, response);
            string serialized = SerializeMessage(message);
            SendMessage(serialized);
        }

        public static void SendCloseConnection()
        {
            Message message = new(User.Username, User.ChattingWith, DateTime.Now, MessageType.CloseConnection);
            string serialized = SerializeMessage(message);
            SendMessage(serialized);
        }

        public static void SendAlertMessage(string alert)
        {
            Message message = new(User.Username, User.ChattingWith, DateTime.Now, MessageType.Alert, alert);
            string serialized = SerializeMessage(message);
            SendMessage(serialized);
            User.AddMessage(message);
        }

        public static void SendStringAsMessage(string content, string toUserName)
        {
            Message message = ConvertStringToMessage(content, toUserName);
            string serialized = SerializeMessage(message);
            SendMessage(serialized);
            User.AddMessage(message);
        }

        public static void CloseClient()
        {
            StopListeningForMessagesThread();
            client?.Close();

            stream = null;
            client = null;
            listenForMessagesCancellationTokenSource = new CancellationTokenSource();
        }

        public static void CloseServerIfServer()
        {
            if (server != null)
            {
                listenForClientsCancellationTokenSource?.Cancel();
            }
            server?.Stop();
        }

        public static void KillClient()
        {
            Message message = new(User.Username, User.ChattingWith, DateTime.Now, MessageType.TerminateChat);
            string serialized = SerializeMessage(message);
            SendMessage(serialized);
        }

        public static bool IsServerListening(IPAddress ip, int port, int timeoutMilliseconds = 1000)
        {
            using (TcpClient tcpClient = new())
            {
                Host = ip;
                IAsyncResult result = tcpClient.BeginConnect(Host.ToString(), port, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(timeoutMilliseconds);
                if (tcpClient.Connected)
                {
                    tcpClient?.EndConnect(result);
                }

                return success;
            }
        }

        public static Message ConvertStringToMessage(string content, string toUserName)
        {
            return new Message(User.Username, toUserName, DateTime.Now, MessageType.Message, content);
        }

        public static bool IsServer()
        {
            return server != null;
        }

        public static bool IsClient()
        {
            return client != null;
        }

        private static string SerializeMessage(Message message)
        {
            return JsonConvert.SerializeObject(message);
        }

        private static Message DeserializeMessage(byte[] bytes, int size)
        {
            string jsonString = Encoding.UTF8.GetString(bytes, 0, size);
            Message result = JsonConvert.DeserializeObject<Message>(jsonString);
            return result;
        }

    }
}
