using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhackerLinkCommonLib.Models;
using WhackerLinkServer.Models;

#nullable disable

namespace WhackerLinkServer
{
    public class VoiceChannelManager
    {
        private static VoiceChannelManager _instance;
        private static readonly object _lock = new object();

        public static VoiceChannelManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new VoiceChannelManager();
                        }
                    }
                }
                return _instance;
            }
        }

        public List<VoiceChannel> VoiceChannels { get; set; }

        private VoiceChannelManager()
        {
            VoiceChannels = new List<VoiceChannel>();
        }

        public List<VoiceChannel> GetVoiceChannels()
        {
            return VoiceChannels;
        }

        public void AddVoiceChannel(VoiceChannel voiceChannel)
        {
            if (!IsVoiceChannelActive(voiceChannel))
            {
                VoiceChannels.Add(voiceChannel);
            }
            else
            {
                Console.WriteLine($"VoiceChannel with Frequency: {voiceChannel.Frequency} already active. Skipping...");
            }
        }

        public void RemoveVoiceChannel(string frequency)
        {
            VoiceChannels.RemoveAll(vc => vc.Frequency == frequency);
        }

        public void RemoveVoiceChannelByClientId(string clientId)
        {
            VoiceChannels.RemoveAll(vc => vc.ClientId == clientId);
        }

        public bool IsVoiceChannelActive(VoiceChannel voiceChannel)
        {
            return VoiceChannels.Any(vc => vc.Frequency == voiceChannel.Frequency);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Active VoiceChannels:");

            foreach (var voiceChannel in VoiceChannels)
            {
                sb.AppendLine($"DstId: {voiceChannel.DstId}, SrcId: {voiceChannel.SrcId}, Frequency: {voiceChannel.Frequency}");
            }

            return sb.ToString();
        }
    }
}