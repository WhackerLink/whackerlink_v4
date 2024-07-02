using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhackerLinkCommonLib.Models.IOSP
{
    public class EMRG_ALRM_REQ
    {
        public string? SrcId { get; set; }
        public string? DstId { get; set; }

        public override string ToString()
        {
            return $"EMRG_ALRM_REQ, srcId: {SrcId}, dstId: {DstId}";
        }
    }
}