#nullable disable

namespace WhackerLinkServer.Models
{
    public class RidAclEntry
    {
        public string Rid {  get; set; }
        public bool Allowed {  get; set; }
        public string Alias { get; set; }
    }
}
