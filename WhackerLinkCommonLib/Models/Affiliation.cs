#nullable disable

using WhackerLinkCommonLib.Models;

namespace WhackerLinkServer.Models
{
    public class Affiliation
    {
        public Affiliation(string clientId, string srcId, string dstId, Site site)
        {
            ClientId = clientId;
            SrcId = srcId;
            DstId = dstId;
            Site = site;
        }

        public string ClientId { get; set; }
        public string SrcId { get; set; }
        public string DstId { get; set; }
        public Site Site { get; set; }
    }
}