using ChatAppServer.Commands;
using ChatAppServer.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ChatAppServer.ViewModels
{
    public class StartViewModel : INotifyPropertyChanged
    {
        private string _ip;
        private string _port;
        private string _feedbackMessage;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand ConfigCommand { get; }
        public ICommand NavServerConfigCommand { get; }
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

        private NavService _nav;
        public StartViewModel(NavService nav)
        {
            _nav = nav;
            ConfigCommand = new RelayCommand(() => Config());
            NavServerConfigCommand = new RelayCommand(() => _nav.NavigateTo(new ServerConfigViewModel(_nav, IP, Port)));
        }

        public void Config()
        {
            Window ipPort = new IpPortWindow(this);
            ipPort.ShowDialog();
        }

        public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
