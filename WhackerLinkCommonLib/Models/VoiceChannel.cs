#nullable disable

namespace WhackerLinkCommonLib.Models
{
    public class VoiceChannel
    {
        public string DstId { get; set; }
        public string SrcId { get; set; }
        public string Frequency { get; set; }
        public string ClientId {  get; set; }
        public bool IsActive { get; set; }
        public Site Site { get; set; }
    }
}