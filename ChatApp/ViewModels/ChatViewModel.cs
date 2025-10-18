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
using System.Diagnostics;
using System.Net;

namespace ChatApp.ViewModels
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        public const int MAX_BYTES = 1000;
        public const int MAX_CHAR = 1000;
        public const int PREFIX_SIZE_BYTES = 4;

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
        public ICommand Spam { get; }

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
            Spam = new RelayCommand(async () => await TestSpam());
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
            await ReceiveMessage();
        }
        // TODO: Implement length prefixing
        public async Task SendMessage()
        {
            // Encode the user's inputted message.
            byte[] message = Encoding.ASCII.GetBytes(Message);
           
            if (message.Length > MAX_BYTES)
            {
                Message = "";
                return;
            }

            // Buffer that will store the length of the message.
            byte[] lengthBuffer = BitConverter.GetBytes(message.Length);
            // Allocate the buffer that will store both the length of the message and the message itself.
            byte[] messagePrefixed = new byte[lengthBuffer.Length + message.Length];
            // Copy the lengthBuffer and the message buffer into one buffer.
            Array.Copy(lengthBuffer, 0, messagePrefixed, 0, lengthBuffer.Length);
            Array.Copy(message, 0, messagePrefixed, lengthBuffer.Length, message.Length);
            try
            {
                int sent = await _chatSocket.SendAsync(messagePrefixed);
            }
            catch (SocketException e)
            {
                FeedbackMessage = "Message failed to send.";
            }
            Message = "";
        }
        // TODO: Implement length prefixing
        public async Task ReceiveMessage() 
        {
            byte[] bytes = new byte[MAX_BYTES];
            //char[] payloadChar = new char[MAX_CHAR];
            int numBytesReceived;
            try
            {
                while ((numBytesReceived = await _chatSocket.ReceiveAsync(bytes, SocketFlags.None, _cancelToken)) != 0)
                {

                    int i = 0;
                    while (i < numBytesReceived)
                    {
                        // Read the prefix from the bytes to get the length of the message.
                        byte[] lengthPrefix = new byte[PREFIX_SIZE_BYTES];
                        Array.Copy(bytes, i, lengthPrefix, 0, PREFIX_SIZE_BYTES);
                        int length = BitConverter.ToInt32(lengthPrefix);
                        // Allocate buffers for the message in byte form and char form.
                        byte[] jsonBytes = new byte[length];
                        char[] jsonChar = new char[length];
                        // Store the bytes of thee json into the buffer for json bytes.
                        try
                        {
                            Array.Copy(bytes, i + PREFIX_SIZE_BYTES, jsonBytes, 0, length);
                        }
                        catch (ArgumentException e)
                        {
                            Debug.WriteLine($"Something went wrong!\nBytes received: {numBytesReceived}\nPrefix Length: {lengthPrefix.Length}\ni: {i}");
                        }
                        Debug.WriteLine($"Bytes received: {numBytesReceived}\nPrefix Length: {lengthPrefix.Length}\ni: {i}");
                        // Form the char buffer from the buffer for the json.
                        int charCount = Encoding.ASCII.GetChars(jsonBytes, 0, length, jsonChar, 0);
                        // Create the string json from the char buffer.
                        string json = new string(jsonChar, 0, charCount);
                        
                        try
                        {
                            Payload payload = JsonSerializer.Deserialize<Payload>(json, JsonOptions);
                            FeedbackMessage = $"Sender: {payload.Sender}\nMessage: {payload.Message}";
                            _ServerMessages.Add(payload);
                        }
                        catch(JsonException e)
                        {
                            Debug.WriteLine($"Something wrong with the payload: {json}");
                        }
                        i = i + PREFIX_SIZE_BYTES + length;
                    }
                }
            }
            catch (SocketException e)
            {
                FeedbackMessage = "You have been disconnected from the server.";
                Debug.WriteLine("Socket exception has occurred");
            }

        }

        public void Disconnect()
        {
            if (_chatSocket is null) return;
            if (!_chatSocket.Connected) return;
            _chatSocket.Shutdown(SocketShutdown.Both);
            _chatSocket.Close();
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

        public async Task TestSpam()
        {
            byte[] spam = Encoding.ASCII.GetBytes("fuck you");
            byte[] lengthPrefix = BitConverter.GetBytes(spam.Length);
            byte[] spamPrefixed = new byte[lengthPrefix.Length + spam.Length];
            Array.Copy(lengthPrefix, 0, spamPrefixed, 0, lengthPrefix.Length);
            Array.Copy(spam, 0, spamPrefixed, lengthPrefix.Length, spam.Length);
            for (int i = 0; i < 15; i++)
            {
                await _chatSocket.SendAsync(spamPrefixed);
            }
        }

        public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
