using Nancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhackerLinkCommonLib.Interfaces;

namespace WhackerLinkServer.RestApi.Modules
{
    public class VoiceChannelModule : NancyModule
    {
        public VoiceChannelModule(IMasterService masterService)
        {
            Get("/api/voiceChannel/query", _ =>
            {
                var availableVoiceChannels = masterService.GetAvailableVoiceChannels();
                var voiceChannels = masterService.GetVoiceChannels();

                var response = new
                {
                    AvailableVoiceChannels = availableVoiceChannels,
                    ActiveVoiceChannels = voiceChannels
                };

                return Response.AsJson(response);
            });
        }
    }
}