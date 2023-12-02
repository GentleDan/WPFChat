using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace SimpleTcp
{
    public class SimpleClient
    {
        private IPEndPoint endPoint;
        public Guid ClientId { get; private set; }
        public Socket Socket { get; private set; }

        public SimpleClient(string address, int port)
        {
            var validIp = IPAddress.TryParse(address, out var ipAddress);

            if (!validIp)
                ipAddress = Dns.GetHostAddresses(address)[0];

            endPoint = new IPEndPoint(ipAddress, port);
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public async Task<bool> Connect()
        {
            var result = await Task.Run(TryConnect);

            try
            {
                if(result)
                {
                    var guid = ReceiveGuid();
                    ClientId = Guid.Parse(guid);
                    return true;
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("Connect " + e.Message);
            }

            return false;
        }

        public SimpleClient() { }

        public async Task<string> CreateGuid(Socket socket)
        {
            return await Task.Run(() => TryCreateGuid(socket));
        }

        public async Task SendMessage(string message)
        {
            await Task.Run(() => TrySendMessage(message));
        }

        public async Task<bool> SendObject(object obj)
        {
            return await Task.Run(() => TrySendObject(obj));
        }

        public async Task<object> ReceiveObject()
        {
            return await Task.Run(TryReceiveObject);
        }

        private object TryReceiveObject()
        {
            if (Socket.Available == 0)
                return null;

            var data = new byte[Socket.ReceiveBufferSize];

            try
            {
                using (Stream s = new NetworkStream(Socket))
                {
                    s.Read(data, 0, data.Length);
                    var memory = new MemoryStream(data);
                    memory.Position = 0;

                    var formatter = new BinaryFormatter();
                    var obj = formatter.Deserialize(memory);

                    return obj;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("TryReceiveObject " + e.Message);
                return null;
            }
        }

        private bool TrySendObject(object obj)
        {
            try
            {
                using (Stream s = new NetworkStream(Socket))
                {
                    var memory = new MemoryStream();
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(memory, obj);
                    var newObj = memory.ToArray();

                    memory.Position = 0;
                    s.Write(newObj, 0, newObj.Length);
                    return true;
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("TrySendObject " + e.Message);
                return false;
            }       
        }

        public bool TrySendMessage(string message)
        {
            try
            {
                using (Stream s = new NetworkStream(Socket))
                {
                    StreamWriter writer = new StreamWriter(s);
                    writer.AutoFlush = true;

                    writer.WriteLine(message);
                    return true;
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("TrySendMessage " + e.Message);
                return false;
            }
        }

        private bool TryConnect()
        {
            try
            {
                Socket.Connect(endPoint);
                return true;
            }
            catch
            {
                Console.WriteLine("Connection failed.");
                return false;
            }
        }

        public string ReceiveGuid()
        {
            try
            {
                using (Stream s = new NetworkStream(Socket))
                {
                    var reader = new StreamReader(s);
                    s.ReadTimeout = 5000;

                    return reader.ReadLine();
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("ReceiveGuid " + e.Message);
                return null;
            }
        }

        private string TryCreateGuid(Socket socket)
        {
            Socket = socket;
            var endPoint = ((IPEndPoint)Socket.LocalEndPoint);
            this.endPoint = endPoint;

            ClientId = Guid.NewGuid();
            return ClientId.ToString();
        }

        //https://stackoverflow.com/questions/2661764/how-to-check-if-a-socket-is-connected-disconnected-in-c
        public bool IsSocketConnected()
        {
            try
            {
                bool part1 = Socket.Poll(5000, SelectMode.SelectRead);
                bool part2 = (Socket.Available == 0);
                if (part1 && part2)
                    return false;
                else
                    return true;
            }
            catch (ObjectDisposedException e)
            {
                Console.WriteLine("IsSocketConnected " + e.Message);
                return false;
            }
        }

        public async Task<bool> PingConnection()
        {
            try
            {
                var result = await SendObject(new PingPacket());
                return result;
            }
            catch (ObjectDisposedException e)
            {
                Console.WriteLine("IsSocketConnected " + e.Message);
                return false;
            }
        }

        public void Disconnect()
        {
            Socket.Close();
        }
    }
}
