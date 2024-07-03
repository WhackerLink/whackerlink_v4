using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhackerLinkServer.Models;

namespace WhackerLinkServer.Managers
{
    public class AffiliationsManager
    {
        public List<Affiliation> Affiliations { get; private set; }

        public AffiliationsManager()
        {
            Affiliations = new List<Affiliation>();
        }

        public List<Affiliation> GetAffiliations()
        {
            return Affiliations;
        }

        public void AddAffiliation(Affiliation affiliation)
        {
            if (!isAffiliated(affiliation))
            {
                Affiliations.Add(affiliation);
            }
            else
            {
                Console.WriteLine($"srcId: {affiliation.SrcId} on dstId: {affiliation.DstId} already affiliated. skipping....");
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

        public void RemoveAffiliationByClientId(string clientId)
        {
            Affiliations.RemoveAll(a => a.ClientId == clientId);
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