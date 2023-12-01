using SimplePackets;
using SimpleTcp;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ChatterClient
{
    public class ChatroomViewModel : BaseViewModel
    {
        private const int ThreadMillisDelayBeforeUpdate = 1;
        private const int WaitForPingMaxSeconds = 5;

        private SimpleClient client;
        private Task<bool> listenTask;
        private Task updateTask;
        private Task connectionTask;
        private DateTime pingSent;
        private DateTime pingLastSent;
        private bool pinged;
        private bool isRunning;
        private string status;

        public ObservableCollection<ChatPacket> Messages { get; }
        public ObservableCollection<string> Users { get; }

        public string Status
        {
            get => status;
            private set => OnPropertyChanged(ref status, value);
        }

        public bool IsRunning
        {
            get => isRunning;
            set => OnPropertyChanged(ref isRunning, value);
        }

        public ChatroomViewModel()
        {
            Messages = new ObservableCollection<ChatPacket>();
            Users = new ObservableCollection<string>();
        }

        public async Task Connect(string username, string address, int port)
        {
            Status = "Connecting...";

            if (SetupClient(address, port))
            {
                var packet = await GetNewConnectionPacket(username);
                await InitializeConnection(packet);
            }
        }

        private async Task InitializeConnection(PersonalPacket connectionPacket)
        {
            pinged = false;

            if (IsRunning)
            {
                updateTask = Task.Run(Update);
                await client.SendObject(connectionPacket);
                connectionTask = Task.Run(MonitorConnection);
                Status = "Connected";
            }
            else
            {
                Status = "Connection failed";
                await Disconnect();
            }
        }

        private async Task<PersonalPacket> GetNewConnectionPacket(string username)
        {
            listenTask = Task.Run(() => client.Connect());

            IsRunning = await listenTask;

            var notifyServer = new UserConnectionPacket
            {
                Username = username,
                IsJoining = true,
                UserGuid = client.ClientId.ToString()
            };

            var personalPacket = new PersonalPacket
            {
                GuidId = client.ClientId.ToString(),
                Package = notifyServer
            };

            return personalPacket;
        }

        private bool SetupClient(string address, int port)
        {
            client = new SimpleClient(address, port);
            return true;
        }

        public async Task Disconnect()
        {
            if(IsRunning)
            {
                IsRunning = false;
                await connectionTask;
                await updateTask;

                client.Disconnect();
            }

            Status = "Disconnected";

            Application.Current.Dispatcher.Invoke(delegate
            {
                Messages.Add(new ChatPacket
                {
                    Username = string.Empty,
                    Message = "You have disconnected from the server.",
                    UserColor = "black"
                });
            });
        }

        public async Task Send(string username, string message, string colorCode)
        {
            var cap = new ChatPacket
            {
                Username = username,
                Message = message,
                UserColor = colorCode
            };

            await client.SendObject(cap);
        }

        private async Task Update()
        {
            while (IsRunning)
            {
                Thread.Sleep(ThreadMillisDelayBeforeUpdate);
                var isReceived = await MonitorData();
                Console.WriteLine(isReceived);
            }
        }

        private async Task MonitorConnection()
        {
            pingSent = DateTime.Now;
            pingLastSent = DateTime.Now;

            while (IsRunning)
            {
                Thread.Sleep(ThreadMillisDelayBeforeUpdate);

                var timePassed = pingSent.TimeOfDay - pingLastSent.TimeOfDay;
                if (timePassed > TimeSpan.FromSeconds(WaitForPingMaxSeconds))
                {
                    if (pinged)
                        continue;

                    await client.PingConnection();
                    pinged = true;

                    Thread.Sleep(5000);

                    if (pinged)
                        await Task.Run(Disconnect);
                }
                else
                {
                    pingSent = DateTime.Now;
                }
            }
        }

        private async Task<bool> MonitorData()
        {
            var newObject = await client.ReceiveObject();
            Application.Current.Dispatcher.Invoke(() => ManagePacket(newObject));
            return false;
        }

        private bool ManagePacket(object packet)
        {
            switch (packet)
            {
                case null:
                    return false;
                case ChatPacket chatP:
                    Messages.Add(chatP);
                    break;
                case UserConnectionPacket connectionP:
                {
                    Users.Clear();
                    foreach (var user in connectionP.Users)
                    {
                        Users.Add(user);
                    }

                    break;
                }
                case PingPacket pingP:
                    pingLastSent = DateTime.Now;
                    pingSent = pingLastSent;
                    pinged = false;
                    break;
            }

            return true;
        }

        public void Clear()
        {
            Messages.Clear();
            Users.Clear();
        }
    }
}
