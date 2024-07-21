using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace WhackerLinkCommonLib.Models.IOSP
{
    public class GRP_VCH_RLS
    {
        public string? SrcId { get; set; }
        public string? DstId { get; set; }
        public string? Channel { get; set; }
        public Site? Site { get; set; }

        public override string ToString()
        {
            return $"GRP_VCH_RLS, srcId: {SrcId}, dstId: {DstId}, channel: {Channel}";
        }
    }
}