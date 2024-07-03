using System.Collections.Generic;
using WhackerLinkCommonLib.Models;
using WhackerLinkServer.Models;

namespace WhackerLinkCommonLib.Interfaces
{
    public interface IMasterService
    {
        List<Affiliation> GetAffiliations();
        List<VoiceChannel> GetVoiceChannels();
        List<string> GetAvailableVoiceChannels();
        void Start();
    }
}