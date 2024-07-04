// SPDX-License-Identifier: AGPL-3.0-only
/**
* Digital Voice Modem - Fixed Network Equipment Core Library
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / Fixed Network Equipment Core Library
* @license AGPLv3 License (https://opensource.org/licenses/AGPL-3.0)
*
*   Copyright (C) 2022 Bryan Biedenkapp, N2PLL
*
*/

using System;

namespace fnecore.NXDN
{
    /// <summary>
    /// NXDN Message Type
    /// </summary>
    public enum NXDNMessageType : byte
    {
        /// <summary>
        /// Voice Call
        /// </summary>
        MESSAGE_TYPE_VCALL = 0x01,
        /// <summary>
        /// Voice Call - Individual
        /// </summary>
        MESSAGE_TYPE_VCALL_IV = 0x03,
        /// <summary>
        /// Data Call Header
        /// </summary>
        MESSAGE_TYPE_DCALL_HDR = 0x09,
        /// <summary>
        /// Data Call Header
        /// </summary>
        MESSAGE_TYPE_DCALL_DATA = 0x0B,
        /// <summary>
        /// Data Call Header
        /// </summary>
        MESSAGE_TYPE_DCALL_ACK = 0x0C,
        /// <summary>
        /// Transmit Release
        /// </summary>
        MESSAGE_TYPE_TX_REL = 0x08,
        /// <summary>
        /// Idle
        /// </summary>
        MESSAGE_TYPE_IDLE = 0x10,
    } // public enum NXDNMessageType : byte
} // namespace fnecore.NXDN
