using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ChatterClient
{
    public class MainWindowViewModel : BaseViewModel
    {
        private string customPort = "8000";
        private string username;
        private string address;
        private string message;
        private string colorCode;
        private ChatroomViewModel chatRoom;

        public string Username
        {
            get => username;
            set => OnPropertyChanged(ref username, value);
        }

        public string Address
        {
            get => address;
            set => OnPropertyChanged(ref address, value);
        }

        public string CustomPort
        {
            get => customPort;
            set => OnPropertyChanged(ref customPort, value);
        }

        public string Message
        {
            get => message;
            set => OnPropertyChanged(ref message, value);
        }

        public string ColorCode
        {
            get => colorCode;
            set => OnPropertyChanged(ref colorCode, value);
        }

        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand SendCommand { get; }

        public ChatroomViewModel ChatRoom
        {
            get => chatRoom;
            private set => OnPropertyChanged(ref chatRoom, value);
        }

        public MainWindowViewModel()
        {
            ChatRoom = new ChatroomViewModel();
            ConnectCommand = new AsyncCommand(Connect, CanConnect);
            DisconnectCommand = new AsyncCommand(Disconnect, CanDisconnect);
            SendCommand = new AsyncCommand(Send, CanSend);
        }

        private async Task Connect()
        {
            ChatRoom = new ChatroomViewModel();

            var validPort = int.TryParse(CustomPort, out var socketPort);

            if (!validPort)
            {
                DisplayError("Please provide a valid port.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Address))
            {
                DisplayError("Please provide a valid address.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Username))
            {
                DisplayError("Please provide a username.");
                return;
            }

            ChatRoom.Clear();
            await Task.Run(() => ChatRoom.Connect(Username, Address, socketPort));
        }

        private async Task Disconnect()
        {
            if (ChatRoom == null)
                DisplayError("You are not connected to a server.");

            if (ChatRoom != null)
                await ChatRoom.Disconnect();
        }

        private async Task Send()
        {
            if (ChatRoom == null)
                DisplayError("You are not connected to a server.");

            if (ChatRoom != null)
                await ChatRoom.Send(Username, Message, ColorCode);

            Message = string.Empty;
        }

        private bool CanConnect() => !ChatRoom.IsRunning;
        private bool CanDisconnect() => ChatRoom.IsRunning;
        private bool CanSend() => !string.IsNullOrWhiteSpace(Message) && ChatRoom.IsRunning;

        private void DisplayError(string message) => MessageBox.Show(message, "Something went wrong...", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
