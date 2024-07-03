using Nancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhackerLinkCommonLib.Interfaces;

namespace WhackerLinkServer.Models
{
    public class VoiceChannelModule : NancyModule
    {
        public VoiceChannelModule(IMasterService masterService)
        {
            Get("/api/voiceChannel/query", _ =>
            {
                var voiceChannels = masterService.GetVoiceChannels();
                return Response.AsJson(voiceChannels);
            });
        }
    }
}
