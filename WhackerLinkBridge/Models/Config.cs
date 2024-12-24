using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using WhackerLinkLib.Models;

#nullable disable

namespace WhackerLinkBridge.Models
{
    public class Config
    {
        public SystemConfiguration system {  get; set; }
        public DvmBridgeConfig dvmBridge { get; set; }
        public MasterConfig master { get; set; }

        public class SystemConfiguration
        {
            public BridgeModes Mode;
            public Site Site;
            public string dstId;
        }

        public class DvmBridgeConfig
        {
            public string Name { get; set; }
            public string Address { get; set; }
            public int RxPort { get; set; }
            public int TxPort { get; set; }
        }

        public class MasterConfig
        {
            public string Name { get; set; }
            public string Address { get; set; }
            public int Port { get; set; }
        }
    }

    public static class ConfigLoader
    {
        public static Config LoadConfig(string path)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var yaml = File.ReadAllText(path);
            return deserializer.Deserialize<Config>(yaml);
        }
    }
}