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
            public string Name { get; set; }
            public string Address { get; set; }
            public int Port { get; set; }
            public RestConfig Rest { get; set; }
            public ReporterConfiguration Reporter { get; set; }
            public VocoderModes VocoderMode { get; set; }
            public string TgAclPath { get; set; }
            public RidAclConfiguration RidAcl { get; set; }
            public List<string> ControlChannels { get; set; }
            public List<string> VoiceChannels { get; set; }
        }

        public class RestConfig
        {
            public bool Enabled { get; set; }
            public string Address { get; set; }
            public int Port { get; set; }
        }

        public class ReporterConfiguration
        {
            public bool Enabled { get; set; }
            public string Address { get; set; }
            public int Port { get; set; }
        }

        public class RidAclConfiguration
        {
            public bool Enabled { get; set; }
            public string Path { get; set; }
            public int ReloadInterval { get; set; }
        }
    }
}