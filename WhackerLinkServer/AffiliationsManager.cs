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

        public void AddAffiliation(Affiliation affiliation)
        {
            if (!isAffiliated(affiliation))
            {
                Affiliations.Add(affiliation);
            }
        }

        public void RemoveAffiliation(Affiliation affiliation)
        {
            Affiliations.RemoveAll(a => a.SrcId == affiliation.SrcId && a.DstId == affiliation.DstId);
        }

        public void RemoveAffiliation(string srcId)
        {
            Affiliations.RemoveAll(a => a.SrcId == srcId);
        }

        public bool isAffiliated(Affiliation affiliation)
        {
            return Affiliations.Any(a => a.SrcId == affiliation.SrcId && a.DstId == affiliation.DstId);
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