using System.Collections.Specialized;
using System.Windows.Input;

namespace ChatterClient
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            ((INotifyCollectionChanged)chatList.Items).CollectionChanged += Messages_CollectionChanged;
        }

        private void Messages_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null)
                return;

            if (e.NewItems.Count > 0)
            {
                chatList.ScrollIntoView(chatList.Items[chatList.Items.Count - 1]);
            }
        }

        private void MessageBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            var context = (MainWindowViewModel)DataContext;
            context.SendCommand.Execute(null);
        }
    }
}
