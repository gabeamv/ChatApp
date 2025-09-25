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

namespace ChatAppServer.ViewModels
{
    public class ServerConfigViewModel : INotifyPropertyChanged
    {
        public const int MAX_BYTES = 1000;
        public const int MAX_CHAR = 1000;
        public ICommand RunTestServer { get; }
        private string _FeedbackMessage = "";
        private Socket _serverSocket;
        private string IP = "127.0.0.1";
        private string Port = "8000";
        private CancellationToken _cancelToken = default;
        private ConcurrentBag<Socket> _clientConnections = new ConcurrentBag<Socket>();
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

        public ServerConfigViewModel(NavService nav)
        {
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
            _serverSocket.Listen();
            FeedbackMessage = "Server has started!";
            while (true)
            {
                Socket clientSocket = await _serverSocket.AcceptAsync();
                _clientConnections.Add(clientSocket);
                _ = ReceiveData(clientSocket);
            }
        }
        private async Task ReceiveData(Socket clientSocket)
        {
            byte[] receivedData = new byte[MAX_BYTES];
            string? message = null;
            int numReceivedBytes;
            while ((numReceivedBytes = await clientSocket.ReceiveAsync(receivedData, SocketFlags.None, _cancelToken)) != 0)
            {
                message = Encoding.ASCII.GetString(receivedData);
                FeedbackMessage = message;
                byte[] response = new byte[numReceivedBytes];
                Array.Copy(receivedData, response, numReceivedBytes);
                Array.Clear(receivedData, 0, numReceivedBytes);
                await SendResponse(response);
            }
            _clientConnections.TryTake(out Socket removedSocket);
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Dispose();
        }

        private async Task SendResponse(byte[] response)
        {
            List<Task> sendResponse = new List<Task>();
            foreach (Socket client in _clientConnections)
            {
                sendResponse.Add(Task.Run(() => client.SendAsync(response)));
            }
            await Task.WhenAll(sendResponse);
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
