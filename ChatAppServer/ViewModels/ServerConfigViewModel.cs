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

namespace ChatAppServer.ViewModels
{
    public class ServerConfigViewModel : INotifyPropertyChanged
    {
        public const int MAX_BYTES = 1000;
        public const int MAX_CHAR = 1000;
        public const int NUM_CONNECTIONS = 8;
        public const int PREFIX_SIZE_BYTES = 4;
        public const string INVALID_USERNAME = "";
        public ICommand RunTestServer { get; }
        private string _FeedbackMessage = "";
        private Socket _serverSocket;
        private string IP;
        private string Port;
        private CancellationToken _cancelToken = default;
        //private ConcurrentBag<Socket> _clientConnections = new ConcurrentBag<Socket>();
        private ConcurrentDictionary<string, Socket> _clientConnections = new ConcurrentDictionary<string, Socket>();
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

        public ServerConfigViewModel(NavService nav, String ip, String port)
        {
            IP = ip;
            Port = port;
            RunTestServer = new RelayCommand(async () => await StartServer());
        }
        // TODO: socket shutdown to end server connection gracefully, then close.
        private async Task StartServer()
        {
            FeedbackMessage = "I am definitely here.";
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            long ipLong;
            int portNum;
            if (int.TryParse(Port, out portNum) && long.TryParse(IP.Replace(".", ""), out ipLong))
            {
                _serverSocket.Bind(new IPEndPoint(IPAddress.Parse(IP), portNum));
                FeedbackMessage = "I am here";
            }
            else { return; }
            _serverSocket.Listen(NUM_CONNECTIONS);
            FeedbackMessage = "Server has started!";
            while (true)
            {
                Socket clientSocket = await _serverSocket.AcceptAsync();
                string username = await InitializeUser(clientSocket);
                if (username == INVALID_USERNAME)
                {
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                    clientSocket.Dispose();
                    continue;
                }
                FeedbackMessage = $"{username} has connected.";
                _ = ReceiveData(clientSocket, username);
            }
        }
        // TODO: Implement length prefixing.
        private async Task ReceiveData(Socket clientSocket, string username)
        {
            byte[] bytes = new byte[MAX_BYTES];
            // char[] messageChar = new char[MAX_CHAR];
            //string? message = null;
            int numReceivedBytes;
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
                    // Update i to handle the next expected message.
                    i = i + PREFIX_SIZE_BYTES + length;
                }

                /*
                int charCount = Encoding.ASCII.GetChars(messageByte, 0, numReceivedBytes, messageChar, 0);
                message = new string(messageChar, 0, charCount);
                Payload payload = new Payload(username, message);
                string payloadJson = JsonSerializer.Serialize<Payload>(payload);
                FeedbackMessage = payloadJson;
                byte[] payloadJsonByte = Encoding.ASCII.GetBytes(payloadJson);

                //byte[] response = new byte[numReceivedBytes];
                //Array.Copy(receivedData, response, numReceivedBytes);
                Array.Clear(messageByte, 0, numReceivedBytes);
                //await SendResponse(response);
                await SendResponse(payloadJsonByte);
                */
            }
            _clientConnections.TryRemove(username, out clientSocket);
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
            clientSocket.Dispose();
            FeedbackMessage = $"{username} has disconnected.";
        }
        // TODO: Implement length prefixing.
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

        private async Task<string> InitializeUser(Socket clientSocket)
        {
            byte[] receivedUsernameBytes = new byte[MAX_BYTES];
            char[] usernameChar = new char[MAX_CHAR];
            string? username = null;
            int numReceivedBytes;
            numReceivedBytes = await clientSocket.ReceiveAsync(receivedUsernameBytes, SocketFlags.None, _cancelToken);

            int charCount = Encoding.ASCII.GetChars(receivedUsernameBytes, 0, numReceivedBytes, usernameChar, 0);
            username = new string(usernameChar, 0, charCount);
            if (_clientConnections.TryAdd(username, clientSocket)) return username;
            return INVALID_USERNAME;
        }

        public async Task TestServer()
        {
            TcpListener testServer = new TcpListener(IPAddress.Loopback, 8000);
            testServer.Start();

            byte[] bytes = new byte[256];

            FeedbackMessage = "Started Server...";

            while (true)
            {
                TcpClient client = await testServer.AcceptTcpClientAsync();
                NetworkStream stream = client.GetStream();

                int i;
                string? message = null;
                StringBuilder feedback = new StringBuilder();

                while ((i = await stream.ReadAsync(bytes, 0, bytes.Length)) != 0)
                {
                    message = Encoding.ASCII.GetString(bytes, 0, i);
                    FeedbackMessage = $"Message received: '{message}'";
                    byte[] msg = Encoding.ASCII.GetBytes(message);
                    stream.Write(msg, 0, msg.Length);
                }

            }
        }

        public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
