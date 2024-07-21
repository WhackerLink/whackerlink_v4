using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhackerLinkCommonLib.Models.IOSP
{
    public class U_DE_REG_REQ
    {
        public string? SrcId { get; set; }
        public string? SysId { get; set; }
        public string? Wacn { get; set; }
        public Site? Site { get; set; }

        public override string ToString()
        {
            return $"U_DE_REG_REQ, srcId: {SrcId}, SysId: {SysId}";
        }
    }
}
