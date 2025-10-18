using ChatAppServer.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ChatAppServer.Views
{
    /// <summary>
    /// Interaction logic for ServerConfigView.xaml
    /// </summary>
    public partial class ServerConfigView : UserControl
    {
        INotifyCollectionChanged? _historyCollection;
        public ServerConfigView()
        {
            DataContextChanged += ContextChanged;
            InitializeComponent();
        }

        private void ContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is ServerConfigViewModel serverConfigViewModel && serverConfigViewModel.History is INotifyCollectionChanged observable)
            {
                _historyCollection = observable;
                _historyCollection.CollectionChanged += HistoryChanged;
            }
        }

        private void HistoryChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (HistoryScrollViewer.ScrollableHeight == 0) return;
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                double dist = HistoryScrollViewer.ScrollableHeight - HistoryScrollViewer.VerticalOffset;
                if (dist <= 100) Dispatcher.BeginInvoke(() => HistoryScrollViewer.ScrollToBottom());
            }
        }
        
        private void Server_Button_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
