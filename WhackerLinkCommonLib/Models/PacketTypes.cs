using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhackerLinkCommonLib.Models
{
        public enum PacketType
        {
            UNKOWN,
            AUDIO_DATA,
            GRP_AFF_REQ,
            GRP_AFF_RSP,
            GRP_VCH_REQ,
            GRP_VCH_RLS,
            GRP_VCH_RSP,
            U_REG_REQ,
            U_REG_RSP,
            U_DE_REG_REQ,
            U_DE_REG_RSP,
            EMRG_ALRM_REQ,
            EMRG_ALRM_RSP
        }
}