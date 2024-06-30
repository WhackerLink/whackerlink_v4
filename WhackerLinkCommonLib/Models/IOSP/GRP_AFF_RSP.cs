using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhackerLinkCommonLib.Models.IOSP
{
    public class GRP_AFF_RSP
    {
        public string? SrcId { get; set; }
        public string? DstId { get; set; }
        public string? SysId { get; set; }
        public int Status { get; set; }

        public override string ToString()
        {
            return $"GRP_AFF_RSP, status: {(ResponseType)Status}, srcId: {SrcId}, dstId: {DstId}";
        }
    }
}