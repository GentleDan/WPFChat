using System;

namespace SimplePackets
{
    [Serializable]
    public class UserConnectionPacket
    {
        public string Username { get; set; }
        public string UserGuid { get; set; }
        public string[] Users { get; set; }
        public bool IsJoining { get; set; }
    }
}
