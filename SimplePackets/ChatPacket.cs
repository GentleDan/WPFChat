using System;

namespace SimplePackets
{
    [Serializable]
    public class ChatPacket
    {
        public string Username { get; set; }
        public string Message { get; set; }
        public string UserColor { get; set; }
    }
}
