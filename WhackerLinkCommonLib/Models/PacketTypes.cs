using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhackerLinkCommonLib.Models
{
        public enum PacketType
        {
            UNKOWN = 0x00,
            AUDIO_DATA = 0x01,
            GRP_AFF_REQ = 0x02,
            GRP_AFF_RSP = 0x03,
            AFF_UPDATE = 0x04,
            GRP_VCH_REQ = 0x05,
            GRP_VCH_RLS = 0x06,
            GRP_VCH_RSP = 0x07,
            U_REG_REQ = 0x08,
            U_REG_RSP = 0x09,
            U_DE_REG_REQ = 0x10,
            U_DE_REG_RSP = 0x11,
            EMRG_ALRM_REQ = 0x12,
            EMRG_ALRM_RSP = 0x13,
            CALL_ALRT = 0x14,
            CALL_ALRT_REQ = 0x15
    }
}