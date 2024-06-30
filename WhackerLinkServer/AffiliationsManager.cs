using WhackerLinkServer.Models;

#nullable disable

namespace WhackerLinkServer
{
    public class AffiliationsManager
    {
        public List<Affiliation> Affiliations { get; set;}

        public AffiliationsManager() { /* stub */ }

        public void AddAffiliation(string srcId, string dstId)
        {
            // TODO: Check if srcid is already aff'ed       
            Affiliations.Add(new Affiliation(srcId, dstId));
        }

        public void RemoveAffiliaton(string  srcId, string dstId)
        {
            Affiliations.Remove(new Affiliation(srcId, dstId));
        }

        public bool isAffiliated(string srcId, string dstId)
        {
            return Affiliations.Contains(new Affiliation(srcId, dstId));
        }
    }
}
