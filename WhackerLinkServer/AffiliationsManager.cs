using System.Text;
using WhackerLinkServer.Models;

#nullable disable

namespace WhackerLinkServer
{
    public class AffiliationsManager
    {
        public List<Affiliation> Affiliations { get; set;}

        public AffiliationsManager()
        {
            Affiliations = new List<Affiliation>();
        }

        public void AddAffiliation(string srcId, string dstId)
        {
            if (!isAffiliated(srcId, dstId))
            {
                Affiliations.Add(new Affiliation(srcId, dstId));
            }
        }

        public void RemoveAffiliation(string srcId, string dstId)
        {
            Affiliations.RemoveAll(a => a.SrcId == srcId && a.DstId == dstId);
        }

        public bool isAffiliated(string srcId, string dstId)
        {
            return Affiliations.Any(a => a.SrcId == srcId && a.DstId == dstId);
        }

        public bool IsSrcIdAffiliated(string srcId)
        {
            return Affiliations.Any(a => a.SrcId == srcId);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Affiliations table:");

            foreach (var affiliation in Affiliations)
            {
                sb.AppendLine($"SrcId: {affiliation.SrcId}, DstId: {affiliation.DstId}");
            }

            return sb.ToString();
        }
    }
}