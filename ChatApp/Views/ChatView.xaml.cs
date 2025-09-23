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
    public partial class ChatView : UserControl, INotifyCollectionChanged
    {
        public ChatView()
        {
            InitializeComponent();
        }

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

    }
}
