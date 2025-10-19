using ChatAppServer.Commands;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ChatAppServer.Services;
using System.Net.WebSockets;
using System.Collections.Concurrent;
using ChatAppServer.Models;
using System.Text.Json;
using System.Windows;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.CodeDom;
using System.Diagnostics;

namespace ChatAppServer.ViewModels
{
    public class ServerConfigViewModel : INotifyPropertyChanged
    {
        public const int MAX_BYTES = 1000;
        public const int MAX_CHAR = 1000;
        public const int NUM_CONNECTIONS = 8;
        public const int PREFIX_SIZE_BYTES = 4;
        public const string INVALID_USERNAME = "";
        private const string SERVER_NAME = "SERVER";
        private const string _hasStartedButtonContent = "Stop Server";
        private const string _hasNotStartedButtonContent = "Start Server";

        public ICommand StartEndServerCommand { get; }
        public ICommand SendServerMessageCommand { get; }

        private string _FeedbackMessage = "";
        private Socket? _serverSocket;
        private string IP;
        private string Port;
        private string _serverMessage = "";
        private string _serverButtonContent = _hasNotStartedButtonContent;
        private bool _hasStarted = false;
        private object _userLock = new();
        private CancellationToken _cancelToken = default;

        private ConcurrentDictionary<string, Socket> _clientConnections = new ConcurrentDictionary<string, Socket>();

        private ObservableCollection<string> _users = new ObservableCollection<string>();
        private ObservableCollection<Payload> _history = new ObservableCollection<Payload>();
        public event EventHandler<MessageSentArgs> MessageSent;
        public event PropertyChangedEventHandler? PropertyChanged;

        public class MessageSentArgs : EventArgs
        {
            public byte[] Response;
            public MessageSentArgs(byte[] response)
            {
                Response = response;
            }
        }

        public string FeedbackMessage
        {
            get { return _FeedbackMessage; }
            set { _FeedbackMessage = value; OnPropertyChanged(); }
        }

        public string ServerMessage
        {
            get { return _serverMessage; }
            set { _serverMessage = value; OnPropertyChanged(); }
        }

        public string ServerButtonContent
        {
            get { return _serverButtonContent; }
            set { _serverButtonContent = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> Users
        {
            get { return _users; }
            set { _users = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Payload> History
        {
            get { return _history; }
            set { _history = value; OnPropertyChanged(); }
        }

        public bool HasStarted
        {
            get { return _hasStarted; }
            set { _hasStarted = value; OnPropertyChanged(); }
        }

        public ServerConfigViewModel(NavService nav, String ip, String port)
        {
            IP = ip;
            Port = port;
            StartEndServerCommand = new RelayCommand(async () => await StartEndServer());
            SendServerMessageCommand = new RelayCommand(async () => await SendServerMessage());
            BindingOperations.EnableCollectionSynchronization(_users, _userLock);
        }
        // TODO: socket shutdown to end server connection gracefully, then close.
        private async Task StartEndServer()
        {
            if (!_hasStarted)
            {
                FeedbackMessage = "I am definitely here.";
                _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                long ipLong;
                int portNum;
                if (int.TryParse(Port, out portNum) && long.TryParse(IP.Replace(".", ""), out ipLong))
                {
                    _serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    _serverSocket.Bind(new IPEndPoint(IPAddress.Parse(IP), portNum));
                    FeedbackMessage = "I am here";
                }
                else { return; }
                _serverSocket.Listen(NUM_CONNECTIONS);
                FeedbackMessage = "Server has started!";
                History.Add(new Payload(SERVER_NAME, FeedbackMessage));
                ServerButtonContent = _hasStartedButtonContent;
                _hasStarted = true;
                try
                {
                    while (_hasStarted)
                    {
                        if (_serverSocket is null) break;
                        Socket clientSocket = await _serverSocket.AcceptAsync();
                        string username = await InitializeUser(clientSocket);
                        if (username == INVALID_USERNAME)
                        {
                            clientSocket.Shutdown(SocketShutdown.Both);
                            clientSocket.Close();
                            clientSocket.Dispose();
                            continue;
                        }
                        History.Add(new Payload(SERVER_NAME, $"{username} has connected."));
                        _ = ReceiveData(clientSocket, username);
                    }
                }
                catch (SocketException e)
                {
                    FeedbackMessage = "Server stopped listening for clients.";
                    History.Add(new Payload(SERVER_NAME, FeedbackMessage));
                }
            }
            else
            {
                Shutdown();
            }
        }
        private async Task ReceiveData(Socket clientSocket, string username)
        {
            byte[] bytes = new byte[MAX_BYTES];
            int numReceivedBytes;
            /*
            try
            {
            */
                while ((numReceivedBytes = await clientSocket.ReceiveAsync(bytes, SocketFlags.None, _cancelToken)) != 0)
                {
                    int i = 0;
                    // While loop to handle all of the received bytes.
                    while (i < numReceivedBytes)
                    {
                        // Read the prefix to get the length of the message.
                        byte[] lengthPrefix = new byte[PREFIX_SIZE_BYTES];
                        Array.Copy(bytes, i, lengthPrefix, 0, PREFIX_SIZE_BYTES);
                        int length = BitConverter.ToInt32(lengthPrefix);
                        // Allocate buffer for the message in byte and char form.
                        byte[] messageByte = new byte[length];
                        char[] messageChar = new char[length];
                        // Store the bytes of the message into the buffer for messages.
                        Array.Copy(bytes, i + PREFIX_SIZE_BYTES, messageByte, 0, length);
                        // Form the char buffer from the buffer for messages.
                        int charCount = Encoding.ASCII.GetChars(messageByte, 0, length, messageChar, 0);
                        // Create the string message from the char buffer.
                        string message = new string(messageChar, 0, charCount);
                        // Create the payload object to send to all the clients.
                        Payload payload = new Payload(username, message);
                        // Serialize the payload object.
                        string payloadJson = JsonSerializer.Serialize<Payload>(payload);
                        byte[] payloadBytes = Encoding.ASCII.GetBytes(payloadJson);
                        FeedbackMessage = payloadJson;
                        // Send the response.
                        await SendResponse(payloadBytes);
                        History.Add(payload);
                        // Update i to handle the next expected message.
                        i = i + PREFIX_SIZE_BYTES + length;
                    }
                }
                /*
            }
            /*
            catch (SocketException e)
            {
                Debug.WriteLine($"{username} has disconnected.");
            }
            */
            _clientConnections.TryRemove(username, out clientSocket);
            FeedbackMessage = $"{username} has disconnected.";
            Users.Remove(username);
            History.Add(new Payload(SERVER_NAME, FeedbackMessage));
        }
        private async Task SendResponse(byte[] payloadJsonByte)
        {
            // Get the length of the json bytes.
            byte[] lengthPrefix = BitConverter.GetBytes(payloadJsonByte.Length);
            // Allocate space to store the length prefix and the json bytes.
            byte[] response = new byte[lengthPrefix.Length + payloadJsonByte.Length];
            // Copy the length prefix and the json bytes into the response buffer.
            Array.Copy(lengthPrefix, 0, response, 0, PREFIX_SIZE_BYTES);
            Array.Copy(payloadJsonByte, 0, response, PREFIX_SIZE_BYTES, payloadJsonByte.Length);

            List<Task> sendResponse = new List<Task>();
            foreach (KeyValuePair<string, Socket> client in _clientConnections)
            {
                sendResponse.Add(Task.Run(async() => await client.Value.SendAsync(response, _cancelToken)));
            }
            await Task.WhenAll(sendResponse);
        }

        private async Task SendServerMessage()
        {
            Payload payload = new Payload(SERVER_NAME, ServerMessage);
            string payloadStr = JsonSerializer.Serialize<Payload>(payload);
            byte[] message = Encoding.ASCII.GetBytes(payloadStr);
            byte[] lengthPrefix = BitConverter.GetBytes(message.Length);
            byte[] messagePrefixed = new byte[lengthPrefix.Length + message.Length];
            Array.Copy(lengthPrefix, 0, messagePrefixed, 0, lengthPrefix.Length);
            Array.Copy(message, 0, messagePrefixed, lengthPrefix.Length, message.Length);
            List<Task> sendServerMessage = new List<Task>();
            foreach(KeyValuePair<string, Socket> client in _clientConnections)
            {
                sendServerMessage.Add(Task.Run(async () => await client.Value.SendAsync(messagePrefixed, _cancelToken)));
            }
            await Task.WhenAll(sendServerMessage);
            History.Add(payload);
            ServerMessage = "";
        }

        private async Task<string> InitializeUser(Socket clientSocket)
        {
            byte[] receivedUsernameBytes = new byte[MAX_BYTES];
            char[] usernameChar = new char[MAX_CHAR];
            string? username = null;
            int numReceivedBytes;
            numReceivedBytes = await clientSocket.ReceiveAsync(receivedUsernameBytes, SocketFlags.None, _cancelToken);

            int charCount = Encoding.ASCII.GetChars(receivedUsernameBytes, 0, numReceivedBytes, usernameChar, 0);
            username = new string(usernameChar, 0, charCount);
            if (_clientConnections.TryAdd(username, clientSocket)) 
            {
                Users.Add(username);
                return username;
            } 
            return INVALID_USERNAME;
        }

        public void Shutdown()
        {
            if (_serverSocket is not null)
            {
                _serverSocket.Close();
                _serverSocket.Dispose();
                _serverSocket = null;
            }
            foreach (KeyValuePair<string, Socket> client in _clientConnections)
            {
                client.Value.Shutdown(SocketShutdown.Both);
                client.Value.Close();
                client.Value.Dispose();
                Users.Remove(client.Key);
            }
            _clientConnections.Clear();
            _serverSocket = null;
            _hasStarted = false;
            History.Clear();
            ServerButtonContent = _hasNotStartedButtonContent;
        }

        public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
