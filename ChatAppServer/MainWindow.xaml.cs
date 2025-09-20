using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ChatAppServer.Commands;

namespace ChatAppServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ICommand RunTestServer { get; }
        private string _FeedbackMessage = "";

        public event PropertyChangedEventHandler? PropertyChanged;

        public string FeedbackMessage
        {
            get { return _FeedbackMessage; }
            set { _FeedbackMessage = value; OnPropertyChanged(); }
        }
        
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            RunTestServer = new RelayCommand(async () => await TestServer());
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
                    byte[] msg = Encoding.ASCII.GetBytes(message.ToUpper());
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