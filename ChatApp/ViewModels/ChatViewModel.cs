using ChatApp.Commands;
using ChatApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ChatApp.ViewModels
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        private NavService _nav;
        private string _FeedbackMessage = "";
        public ICommand Test { get; } 
        public event PropertyChangedEventHandler? PropertyChanged;
        public string FeedbackMessage
        {
            get { return _FeedbackMessage; }
            set { _FeedbackMessage = value; OnPropertyChanged(); }
        }

        public ChatViewModel(NavService nav)
        {
            _nav = nav;
            Test = new RelayCommand(async () => await TestConnect());
        }

        public async Task TestConnect()
        {
            using Socket chatSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            CancellationToken cancellationToken = default;
            await chatSocket.ConnectAsync("localhost", 8000, cancellationToken);
            // Message test to the server.
            byte[] test = Encoding.ASCII.GetBytes("Hello World, I Am Here.");
            int bytesSent = await chatSocket.SendAsync(test);

            byte[] responseBytes = new byte[256];
            char[] responseChars = new char[256];
            while (true)
            {
                int bytesReceived = await chatSocket.ReceiveAsync(responseBytes, SocketFlags.None, cancellationToken);
                if (bytesReceived == 0) break;
                int charCount = Encoding.ASCII.GetChars(responseBytes, 0, bytesReceived, responseChars, 0);
                FeedbackMessage = new string(responseChars, 0, charCount);
            }
        }

        public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
