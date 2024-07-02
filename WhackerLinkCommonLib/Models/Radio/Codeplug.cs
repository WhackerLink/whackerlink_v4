#nullable disable

namespace WhackerLinkCommonLib.Models.Radio
{
    public class Codeplug
    {
        public RadioWideConfiguration RadioWide { get; set; }
        public List<System> Systems { get; set; }
        public List<Zone> Zones { get; set; }

        public class RadioWideConfiguration
        {
            public string HostVersion { get; set; }
            public string CodeplugVersion { get; set; }
            public string RadioAlias { get; set; }
            public string SerialNumber { get; set; }
            public int Model { get; set; }
        }

        public class System
        {
            public string Name { get; set; }
            public string Address { get; set; }
            public int Port { get; set; }
            public string Rid { get; set; }
        }

        public class Zone
        {
            public string Name { get; set; }
            public List<Channel> Channels { get; set; }
        }

        public class Channel
        {
            public string Name { get; set; }
            public string System { get; set; }
            public string Tgid { get; set; }
        }

        public System GetSystemForChannel(Channel channel)
        {
            return Systems.FirstOrDefault(s => s.Name == channel.System);
        }
    }
}