using ChatAppServer.Commands;
using ChatAppServer.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ChatAppServer
{
    /// <summary>
    /// Interaction logic for IpPortWindow.xaml
    /// </summary>
    public partial class IpPortWindow : Window, INotifyPropertyChanged
    {
        private object _vm;
        private string _ip;
        private string _port;
        private string _feedbackMessage;

        public ICommand ConfirmCommand { get; }

        public string IP
        {
            get { return _ip; }
            set
            {
                _ip = value;
                OnPropertyChanged();
            }
        }
        public string Port
        {
            get { return _port; }
            set
            {
                _port = value;
                OnPropertyChanged();
            }
        }

        public string FeedbackMessage
        {
            get { return _feedbackMessage; }
            set
            {
                _feedbackMessage = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        // Will be instantiated by injecting a view model, either start view model or sever config view model
        public IpPortWindow(object vm)
        {
            _vm = vm;
            Debug.WriteLine($"VM Should Be Here: {_vm}");
            ConfirmCommand = new RelayCommand(() => Confirm());
            DataContext = this;
            InitializeComponent();
            

        }

        public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Confirm()
        {
            if (_vm is StartViewModel startViewModel)
            {
                if (IP is null || Port is null)
                {
                    startViewModel.FeedbackMessage = "Empty IP or Port.";
                    this.Close();
                }
                else
                {
                    startViewModel.IP = IP;
                    startViewModel.Port = Port;
                    startViewModel.NavServerConfigCommand.Execute(null);
                    this.Close();
                }
            }
        }
    }
}
