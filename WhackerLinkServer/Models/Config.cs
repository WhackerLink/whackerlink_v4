namespace WhackerLinkServer.Models
{
    public class Config
    {
        public SystemConfig? System { get; set; }

        public class SystemConfig
        {
            public string? NetworkName { get; set; }
            public string? NetworkBindAddress { get; set; }
            public int NetworkBindPort { get; set; }
            public List<string>? ControlChannels { get; set; }
            public List<string>? VoiceChannels { get; set; }
        }
    }
}