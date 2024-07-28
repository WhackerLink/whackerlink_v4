/*
 * Some source from https://github.com/dvmproject/dvmbridge
 */

using fnecore;

#nullable disable

namespace WhackerLink2Dvm.Models
{
    public class Config
    {
        public int DropTimeMs = 180;
        public int SourceId;
        public int DestinationId;
        public int Slot = 1;

        public FneConfiguration Fne { get; set; }
        public WhackerLinkConfiguration WhackerLink { get; set; }

        public class FneConfiguration
        {
            public string Name = "BRIDGE";
            public uint PeerId;
            public string Address = "127.0.0.1";
            public int Port = 62031;
            public string Passphrase;
            public bool Ecnrypted;
            public string PresharedKey;
        }

        public class WhackerLinkConfiguration
        {
            public string Name { get; set; }
            public string Address { get; set; }
            public int Port { get; set; }
        }

        public static PeerDetails ConvertToDetails(FneConfiguration peer)
        {
            PeerDetails details = new PeerDetails();

            // identity
            details.Identity = peer.Name;
            details.RxFrequency = 0;
            details.TxFrequency = 0;

            // system info
            details.Latitude = 0.0d;
            details.Longitude = 0.0d;
            details.Height = 1;
            details.Location = "Digital Network";

            // channel data
            details.TxPower = 0;
            details.TxOffsetMhz = 0.0f;
            details.ChBandwidthKhz = 0.0f;
            details.ChannelID = 0;
            details.ChannelNo = 0;

            // RCON
            details.Password = "ABCD123";
            details.Port = 9990;

            details.Software = $"WhackerLink2Dvm";

            return details;
        }
    }
}