using ChatApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ChatApp.Views
{
    // Chat GPT usage, so heavy commenting.
    public partial class ChatView : UserControl
    {
        private INotifyCollectionChanged? _currentCollection;

        public ChatView()
        {
            // Data context changes at runtime, so we need a method to execute by then to check if and when the data
            // context changes to the viewmodel.
            DataContextChanged += ContextChanged;
            InitializeComponent();
        }

        private void ContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Unsubscribe from old collection when the context has changed.
            if (_currentCollection != null)
            {
                _currentCollection.CollectionChanged -= MessagesChanged;
                return;
            }

            // Check if the current data context is the chatviewmodel data context.
            if (DataContext is ChatViewModel chatViewModel && chatViewModel.ServerMessages is INotifyCollectionChanged observable)
            {
                // Store the observable server messages as Inotifycollectionchanged. observable collection implements inotifycollectionchanged
                _currentCollection = observable;
                // subscribe to the observable server messages. when the collection changes execute the subscribed method.
                _currentCollection.CollectionChanged += MessagesChanged;
            }
        }

        private void MessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (ScrollViewer.ScrollableHeight == 0) return;
            // If the action that was done on the messages was adding a message...
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                // Calculate the distance of the scroll wheel from the bottom.
                double dist = ScrollViewer.ScrollableHeight - ScrollViewer.VerticalOffset;
                // If scroll wheel 100 pixels from the bottom, scroll wheel goes to the bottom
                if (dist <= 100) ScrollViewer.ScrollToBottom();
            }
        }
    }
}
