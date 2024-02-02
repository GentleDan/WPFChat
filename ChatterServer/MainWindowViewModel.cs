﻿using SimplePackets;
using SimpleTcp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ChatterServer
{
    public class MainWindowViewModel : BaseViewModel
    {
        private string customPort = "8000";
        private string internalAddress;
        private string status = "Idle";

        public string InternalAddress
        {
            get => internalAddress;
            set => OnPropertyChanged(ref internalAddress, value);
        }

        public string Port
        {
            get => customPort;
            set => OnPropertyChanged(ref customPort, value);
        }

        public string Status
        {
            get => status;
            private set => OnPropertyChanged(ref status, value);
        }

        private int _clientsConnected;
        public int ClientsConnected
        {
            get { return _clientsConnected; }
            set { OnPropertyChanged(ref _clientsConnected, value); }
        }

        public ObservableCollection<string> Outputs { get; set; }
        public Dictionary<string,string> Usernames = new Dictionary<string, string>();

        public ICommand RunCommand { get; set; }
        public ICommand StopCommand { get; set; }

        private SimpleServer _server;
        private bool _isRunning;

        private Task _updateTask;
        private Task _listenTask;

        public MainWindowViewModel()
        {
            Outputs = new ObservableCollection<string>();

            RunCommand = new AsyncCommand(Run);
            StopCommand = new AsyncCommand(Stop);
        }

        private async Task Run()
        {
            Status = "Connecting...";
            await SetupServer();
            _server.Open();
            _listenTask = Task.Run(() => _server.Start());
            _updateTask = Task.Run(() => Update());
            _isRunning = true;
        }

        private async Task SetupServer()
        {
            Status = "Validating socket...";
            if (!int.TryParse(Port, out var socketPort))
            {
                DisplayError("Port value is not valid.");
                return;
            }

            Status = "Obtaining IP...";
            await Task.Run(() => GetInternalIp());
            Status = "Setting up server...";
            _server = new SimpleServer(IPAddress.Any, socketPort);

            Status = "Setting up events...";
            _server.OnConnectionAccepted += Server_OnConnectionAccepted;
            _server.OnConnectionRemoved += Server_OnConnectionRemoved;
            _server.OnPacketSent += Server_OnPacketSent;
            _server.OnPersonalPacketSent += Server_OnPersonalPacketSent;
            _server.OnPersonalPacketReceived += Server_OnPersonalPacketReceived;
            _server.OnPacketReceived += Server_OnPacketReceived;
        }

        private void Update()
        {
            while(_isRunning)
            {
                Thread.Sleep(5);
                if (!_server.IsRunning)
                {
                    Task.Run(() => Stop());
                    return;
                }

                ClientsConnected = _server.Connections.Count;
                Status = "Running";
            }
        }

        private async Task Stop()
        {
            InternalAddress = string.Empty;
            _isRunning = false;
            ClientsConnected = 0;
            _server.Close();

            await _listenTask;
            await _updateTask;
            Status = "Stopped";
        }


        private void GetInternalIp()
        {
            var localIpAddress = string.Empty;

            try
            {
                foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (networkInterface.OperationalStatus != OperationalStatus.Up
                        || networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                        continue;

                    foreach (var ip in networkInterface.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(ip.Address))
                            continue;

                        localIpAddress = ip.Address.ToString();
                        break;
                    }

                    if (!string.IsNullOrEmpty(localIpAddress))
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при получении локального IP-адреса: " + ex.Message);
            }

            InternalAddress = "192.168.0.103";
        }

        private void DisplayError(string message)
        {
            MessageBox.Show(message, "Woah there!", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Server_OnPacketSent(object sender, PacketEvents e)
        {
            WriteOutput("Packet Sent");
        }

        private void Server_OnPacketReceived(object sender, PacketEvents e)
        {
            WriteOutput("Packet Received");
        }

        private void Server_OnPersonalPacketSent(object sender, PersonalPacketEvents e)
        {
            WriteOutput("Personal Packet Sent");
        }

        private void Server_OnPersonalPacketReceived(object sender, PersonalPacketEvents e)
        {
            if(e.Packet.Package is UserConnectionPacket ucp)
            {
                var notification = new ChatPacket
                {
                    Username = "Server",
                    Message = "A new user has joined the chat",
                    UserColor = Colors.Purple.ToString()
                };

                if (Usernames.Keys.Contains(ucp.UserGuid))
                    Usernames.Remove(ucp.UserGuid);
                else
                    Usernames.Add(ucp.UserGuid, ucp.Username);

                ucp.Users = Usernames.Values.ToArray();

                Task.Run(() => _server.SendObjectToClients(ucp)).Wait();
                Thread.Sleep(500);
                Task.Run(() => _server.SendObjectToClients(notification)).Wait();
            }

            WriteOutput("Personal Packet Received");
        }

        private void Server_OnConnectionAccepted(object sender, PacketEvents e)
        {
            WriteOutput("Client Connected: " + e.Sender.Socket.RemoteEndPoint.ToString());
        }

        private void Server_OnConnectionRemoved(object sender, PacketEvents e)
        {
            if(!Usernames.ContainsKey(e.Sender.ClientId.ToString()))
            {
                return;
            }

            var notification = new ChatPacket
            {
                Username = "Server",
                Message = "A user has left the chat",
                UserColor = Colors.Purple.ToString()
            };

            var userPacket = new UserConnectionPacket
            {
                UserGuid = e.Sender.ClientId.ToString(),
                Username = Usernames[e.Sender.ClientId.ToString()],
                IsJoining = false
            };

            if(Usernames.Keys.Contains(userPacket.UserGuid))
                Usernames.Remove(userPacket.UserGuid);

            userPacket.Users = Usernames.Values.ToArray();

            if(_server.Connections.Count != 0)
            {
                Task.Run(() => _server.SendObjectToClients(userPacket)).Wait();
                Task.Run(() => _server.SendObjectToClients(notification)).Wait();
            }
            WriteOutput("Client Disconnected: " + e.Sender.Socket.RemoteEndPoint.ToString());
        }

        private void WriteOutput(string message)
        {
            App.Current.Dispatcher.Invoke(delegate
            {
                Outputs.Add(message);
            });
        }
    }
}
