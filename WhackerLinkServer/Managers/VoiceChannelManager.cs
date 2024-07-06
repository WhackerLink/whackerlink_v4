using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhackerLinkCommonLib.Models;
using WhackerLinkServer.Models;

namespace WhackerLinkServer.Managers
{
    public class VoiceChannelManager
    {
        public List<VoiceChannel> VoiceChannels { get; private set; }

        public VoiceChannelManager()
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

        public void RemoveVoiceChannelByDstId(string dstId)
        {
            VoiceChannels.RemoveAll(vc => vc.DstId == dstId);
        }

        public bool IsVoiceChannelActive(VoiceChannel voiceChannel)
        {
            return VoiceChannels.Any(vc => vc.Frequency == voiceChannel.Frequency);
        }

        public bool IsDestinationActive(string dstId)
        {
            return VoiceChannels.Any(vc => vc.DstId == dstId);
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