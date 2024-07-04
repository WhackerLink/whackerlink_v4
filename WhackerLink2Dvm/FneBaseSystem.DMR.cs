// SPDX-License-Identifier: AGPL-3.0-only
/**
* Digital Voice Modem - Audio Bridge
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / Audio Bridge
* @license AGPLv3 License (https://opensource.org/licenses/AGPL-3.0)
*
*   Copyright (C) 2022-2024 Bryan Biedenkapp, N2PLL
*
*/

using fnecore.DMR;
using fnecore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhackerLink2Dvm
{
    /// <summary>
    /// Implements a FNE system base.
    /// </summary>
    public abstract partial class FneSystemBase : fnecore.FneSystemBase
    {

        /*
        ** Methods
        */

        /// <summary>
        /// Callback used to validate incoming NXDN data.
        /// </summary>
        /// <param name="peerId">Peer ID</param>
        /// <param name="srcId">Source Address</param>
        /// <param name="dstId">Destination Address</param>
        /// <param name="callType">Call Type (Group or Private)</param>
        /// <param name="messageType">NXDN Message Type</param>
        /// <param name="frameType">Frame Type</param>
        /// <param name="streamId">Stream ID</param>
        /// <param name="message">Raw message data</param>
        /// <returns>True, if data stream is valid, otherwise false.</returns>
        protected override bool DMRDataValidate(uint peerId, uint srcId, uint dstId, byte bit, CallType callType, FrameType type, DMRDataType messageType, uint streamId, byte[] message)
        {
            return true;
        }

        /// <summary>
        /// Event handler used to process incoming NXDN data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void DMRDataReceived(object sender, DMRDataReceivedEvent e)
        {
            /* unsupported for now */
            return;
        }
    } // public abstract partial class FneSystemBase : fnecore.FneSystemBase
}