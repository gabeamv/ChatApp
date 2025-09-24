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

namespace ChatAppServer.ViewModels
{
    public class ServerConfigViewModel : INotifyPropertyChanged
    {

        public ICommand RunTestServer { get; }
        private string _FeedbackMessage = "";
        private Socket _serverSocket;
        private string IP = "";
        private string Port = "";


        public event PropertyChangedEventHandler? PropertyChanged;

        public string FeedbackMessage
        {
            get { return _FeedbackMessage; }
            set { _FeedbackMessage = value; OnPropertyChanged(); }
        }

        public ServerConfigViewModel(NavService nav)
        {
            RunTestServer = new RelayCommand(async () => await TestServer());
        }

        private void StartServer()
        {
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            long ipLong;
            int portNum;
            if (int.TryParse(Port, out portNum) && long.TryParse(IP, out ipLong))
            {
                _serverSocket.Bind(new IPEndPoint(ipLong, portNum));
            }
            else { return; }
            _serverSocket.Listen();
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
