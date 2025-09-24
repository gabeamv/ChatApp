using ChatAppServer.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ChatAppServer.ViewModels
{
    
    public class MainViewModel : INotifyPropertyChanged
    {
        object _currentViewModel;
        private NavService _nav;

        public event PropertyChangedEventHandler? PropertyChanged;

        public object? CurrentViewModel
        {
            get { return _currentViewModel; }
            set { _currentViewModel = value; OnPropertyChanged(); }
        }

        public MainViewModel()
        {
            _nav = new NavService((object viewModel) => CurrentViewModel = viewModel);
            _currentViewModel = new ServerConfigViewModel(_nav);
        }
        public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
