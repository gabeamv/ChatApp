using ChatApp.Commands;
using ChatApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Data;
using ChatApp.Models;
using System.Windows.Controls;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace ChatApp.ViewModels
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        public const int MAX_BYTES = 1000;
        public const int MAX_CHAR = 1000;

        private NavService _nav;
        private string _FeedbackMessage = "";
        private string _Message = "";
        private string _TestMessage = "Hello i am here, what is you name, where are you from, where are you, what am i doing here, are you okay, can i help you somehow?";
        private ObservableCollection<Payload> _ServerMessages = new ObservableCollection<Payload>();
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
        private ScrollViewer _ChatScrollView = new ScrollViewer();
        private string _IP = "";
        private string _Port = "";
        private Socket _chatSocket;
        private CancellationToken _cancelToken = default;
        private object _lock = new object();
        private Task _receive;
        private string _username;

        public ICommand Test { get; } 
        public ICommand ServerConnectCommand { get; }
        public ICommand SendMessageCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string IP
        {
            get { return _IP; }
            set { _IP = value; OnPropertyChanged(); }
        }

        public string Port
        {
            get { return _Port; }
            set { _Port = value; OnPropertyChanged(); }
        }

        public string Username
        {
            get { return _username; }
            set { _username = value; OnPropertyChanged(); }
        }

        public string FeedbackMessage
        {
            get { return _FeedbackMessage; }
            set { _FeedbackMessage = value; OnPropertyChanged(); }
        }

        public string Message
        {
            get { return _Message; }
            set { _Message = value; OnPropertyChanged(); }
        }

        public ScrollViewer ChatScrollView
        {
            get { return _ChatScrollView; }
            set { _ChatScrollView = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Payload> ServerMessages
        {
            get { return _ServerMessages; }
            set { _ServerMessages = value; OnPropertyChanged(); }
        }

        public ChatViewModel(NavService nav)
        {
            _nav = nav;
            Test = new RelayCommand(async () => await TestConnect());
            ServerConnectCommand = new RelayCommand(async () => await ServerConnect());
            SendMessageCommand = new RelayCommand(async () => await SendMessage());
            BindingOperations.EnableCollectionSynchronization(_ServerMessages, _lock);
        }

        public void TestMessages()
        {
            //for (int i = 0; i < 1000; i++) ServerMessages.Add(new Payload { Sender = "Gabe", Message = _TestMessage });
        }

        // TODO: handle exceptions and bad input
        public async Task ServerConnect()
        {
            _chatSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            int portNum;
            if (int.TryParse(Port, out portNum))
            {
                try
                {
                    await _chatSocket.ConnectAsync(IP, portNum, _cancelToken);
                }
                catch (SocketException e)
                {
                    FeedbackMessage = "Server is not up.";
                    return;
                }
                FeedbackMessage = $"You have connected to {IP}:{Port}";
                byte[] username = Encoding.ASCII.GetBytes(Username);
                await _chatSocket.SendAsync(username);
            }
            else
            {
                FeedbackMessage = "Faulty port number.";
            }
            _ = ReceiveMessage();
        }

        public async Task SendMessage()
        {
            byte[] message = Encoding.ASCII.GetBytes(Message);
            if (message.Length > MAX_BYTES)
            {
                Message = "";
                return;
            }
            int sent = await _chatSocket.SendAsync(message);
            Message = "";
        }

        public async Task ReceiveMessage() 
        {
            byte[] payloadByte = new byte[MAX_BYTES];
            char[] payloadChar = new char[MAX_CHAR];
            int numBytesReceived;
            while ((numBytesReceived = await _chatSocket.ReceiveAsync(payloadByte, SocketFlags.None, _cancelToken)) != 0)
            {
                int charCount = Encoding.ASCII.GetChars(payloadByte, 0, numBytesReceived, payloadChar, 0);
                string payloadJson = new string(payloadChar, 0, charCount);
                Payload payload = JsonSerializer.Deserialize<Payload>(payloadJson, JsonOptions);
                FeedbackMessage = $"Sender: {payload.Sender}\nMessage: {payload.Message}";
                _ServerMessages.Add(payload);
            }
            _chatSocket.Shutdown(SocketShutdown.Both);
            _chatSocket.Dispose();
        }

        public async Task TestConnect()
        {
            Socket chatSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            CancellationToken cancellationToken = default;
            await chatSocket.ConnectAsync("127.0.0.1", 8000, cancellationToken);
            // Message test to the server.
            byte[] test = Encoding.ASCII.GetBytes("Hello World, I Am Here.");
            int bytesSent = await chatSocket.SendAsync(test);

            byte[] responseBytes = new byte[512];
            char[] responseChars = new char[512];
            while (true)
            {
                int bytesReceived = await chatSocket.ReceiveAsync(responseBytes, SocketFlags.None, cancellationToken);
                if (bytesReceived == 0) break;
                int charCount = Encoding.ASCII.GetChars(responseBytes, 0, bytesReceived, responseChars, 0);
                FeedbackMessage = new string(responseChars, 0, charCount);
            }
            chatSocket.Dispose();
        }

        public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
