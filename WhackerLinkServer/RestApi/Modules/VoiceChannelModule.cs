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

                var response = new
                {
                    ControlChannels = masterService.GetControlChannels(),
                    AvailableVoiceChannels = masterService.GetAvailableVoiceChannels(),
                    ActiveVoiceChannels = masterService.GetVoiceChannels()
                };

                return Response.AsJson(response);
            });
        }
    }
}