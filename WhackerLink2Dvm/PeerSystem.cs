// SPDX-License-Identifier: AGPL-3.0-only
/**
* Digital Voice Modem - Audio Bridge
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / Audio Bridge
* @license AGPLv3 License (https://opensource.org/licenses/AGPL-3.0)
*
*   Copyright (C) 2023 Bryan Biedenkapp, N2PLL
*
*/

using fnecore;
using System.Net;
using WhackerLink2Dvm.Models;

#nullable disable

namespace WhackerLink2Dvm
{
    /// <summary>
    /// Implements a peer FNE router system.
    /// </summary>
    public class PeerSystem : FneSystemBase
    {
        protected FnePeer peer;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="PeerSystem"/> class.
        /// </summary>
        public PeerSystem() : base(Create())
        {
            this.peer = (FnePeer)fne;
        }

        /// <summary>
        /// Internal helper to instantiate a new instance of <see cref="FnePeer"/> class.
        /// </summary>
        /// <param name="config">Peer stanza configuration</param>
        /// <returns><see cref="FnePeer"/></returns>
        private static FnePeer Create()
        {
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, WhackerLink2Dvm.config.Fne.Port);
            string presharedKey = WhackerLink2Dvm.config.Fne.Encrypted ? WhackerLink2Dvm.config.Fne.PresharedKey : null;

            if (WhackerLink2Dvm.config.Fne.Address == null)
                throw new NullReferenceException("address");
            if (WhackerLink2Dvm.config.Fne.Address == string.Empty)
                throw new ArgumentException("address");

            // handle using address as IP or resolving from hostname to IP
            try
            {
                endpoint = new IPEndPoint(IPAddress.Parse(WhackerLink2Dvm.config.Fne.Address), WhackerLink2Dvm.config.Fne.Port);
            }
            catch (FormatException)
            {
                IPAddress[] addresses = Dns.GetHostAddresses(WhackerLink2Dvm.config.Fne.Address);
                if (addresses.Length > 0)
                    endpoint = new IPEndPoint(addresses[0], WhackerLink2Dvm.config.Fne.Port);
            }

            

            FnePeer peer = new FnePeer(WhackerLink2Dvm.config.Fne.Name, WhackerLink2Dvm.config.Fne.PeerId, endpoint, presharedKey);

            // set configuration parameters
            peer.RawPacketTrace = true;

            peer.PingTime = 5;
            peer.Passphrase = WhackerLink2Dvm.config.Fne.Passphrase;
            peer.Information.Details = Config.ConvertToDetails(WhackerLink2Dvm.config.Fne);

            peer.PeerConnected += Peer_PeerConnected;

            return peer;
        }

        /// <summary>
        /// Event action that handles when a peer connects.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private static void Peer_PeerConnected(object sender, PeerConnectedEvent e)
        {
            /* stub */
        }

        /// <summary>
        /// Helper to send a activity transfer message to the master.
        /// </summary>
        /// <param name="message">Message to send</param>
        public void SendActivityTransfer(string message)
        {
            /* stub */
        }

        /// <summary>
        /// Helper to send a diagnostics transfer message to the master.
        /// </summary>
        /// <param name="message">Message to send</param>
        public void SendDiagnosticsTransfer(string message)
        {
            /* stub */
        }

        protected override void KeyResponse(object sender, KeyResponseEvent e)
        {
            throw new NotImplementedException();
        }
    } // public class PeerSystem
}