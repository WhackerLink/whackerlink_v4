using WhackerLinkCommonLib.Models.IOSP;
using WhackerLinkCommonLib.Models;
using System;

namespace WhackerLinkCommonLib.Interfaces
{
    public interface IWebSocketHandler
    {
        bool IsConnected { get; }

        void Connect(string address, int port);
        void Disconnect();
        void SendMessage(object message);

        event Action<U_REG_RSP> OnUnitRegistrationResponse;
        event Action<U_DE_REG_RSP> OnUnitDeRegistrationResponse;
        event Action<GRP_AFF_RSP> OnGroupAffiliationResponse;
        event Action<AFF_UPDATE> OnAffiliationUpdate;
        event Action<GRP_VCH_RSP> OnVoiceChannelResponse;
        event Action<GRP_VCH_RLS> OnVoiceChannelRelease;
        event Action<EMRG_ALRM_RSP> OnEmergencyAlarmResponse;
        event Action<CALL_ALRT> OnCallAlert;
        event Action<byte[], VoiceChannel> OnAudioData;
        event Action OnOpen;
        event Action OnClose;
    }
}