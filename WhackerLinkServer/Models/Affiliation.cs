#nullable disable

namespace WhackerLinkServer.Models
{
    public class Affiliation
    {
        public Affiliation(string clientId, string srcId, string dstId)
        {
            ClientId = clientId;
            SrcId = srcId;
            DstId = dstId;
        }

        public string ClientId { get; set; }
        public string SrcId { get; set; }
        public string DstId { get; set; }
    }
}