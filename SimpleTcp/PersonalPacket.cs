using System;

namespace SimpleTcp
{
    [Serializable]
    public class PersonalPacket
    {
        public string GuidId { get; set; }
        public object Package { get; set; }
    }

    [Serializable]
    public class PingPacket
    {
        public string GuidId { get; set; }
    }

    public class PacketEvents : EventArgs
    {
        public SimpleClient Sender;
        public SimpleClient Receiver;
        public object Packet;
    }

    public class PersonalPacketEvents : EventArgs
    {
        public SimpleClient Sender;
        public SimpleClient Receiver;
        public PersonalPacket Packet;
    }
}
