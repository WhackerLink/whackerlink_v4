using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable disable

namespace WhackerLinkServer.Models
{
    public class Affiliation
    {
        public Affiliation(string SrcId, string DstId)
        {
            this.SrcId = SrcId;
            this.DstId = DstId;
        }

        public string SrcId { get; set; }
        public string DstId { get; set; }
        public string EndPoint { get; set; }
    }
}