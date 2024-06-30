#nullable disable

namespace WhackerLinkServer.Models
{
    public class Config
    {
        public SystemConfig System { get; set; }
        public List<MasterConfig> Masters { get; set; }

        public class SystemConfig
        {
            public bool Debug;
        }

        public class MasterConfig
        {
            public string Name { get; set;}
            public string Address { get; set; }
            public int Port { get; set; }
            public string TgAclPath { get; set; }
            public string RidAclPath { get; set; }
            public List<string> ControlChannels { get; set; }
            public List<string> VoiceChannels { get; set; }
        }
    }
}