// SPDX-License-Identifier: AGPL-3.0-only
/**
* Digital Voice Modem - Fixed Network Equipment Core Library
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / Fixed Network Equipment Core Library
* @license AGPLv3 License (https://opensource.org/licenses/AGPL-3.0)
*
*   Copyright (C) 2023 Bryan Biedenkapp, N2PLL
*
*/

using System;

namespace fnecore
{
    /// <summary>
    /// 
    /// </summary>
    public class RtpExtensionHeader
    {
        protected int offset = 0;

        /// <summary>
        /// Format of the extension header payload contained within the packet.
        /// </summary>
        public ushort PayloadType { get; set; }

        /// <summary>
        /// Length of the extension header payload (in 32-bit units).
        /// </summary>
        public ushort PayloadLength { get; set; }

        /*
        ** Methods
        */
        /// <summary>
        /// Initializes a new instance of the <see cref="RtpExtensionHeader"/> class.
        /// </summary>
        /// <param name="offset"></param>
        public RtpExtensionHeader(int offset = 12) // 12 bytes is the length of the RTP Header
        {
            this.offset = offset;
            PayloadType = 0;
            PayloadLength = 0;
        }

        /// <summary>
        /// Decode a RTP header.
        /// </summary>
        /// <param name="data"></param>
        public virtual bool Decode(byte[] data)
        {
            if (data == null)
                return false;

            PayloadType = (ushort)((data[0 + offset] << 8) | (data[1 + offset] << 0));      // Payload Type
            PayloadLength = (ushort)((data[2 + offset] << 8) | (data[3 + offset] << 0));    // Payload Length

            return true;
        }

        /// <summary>
        /// Encode a RTP header.
        /// </summary>
        /// <param name="data"></param>
        public virtual void Encode(ref byte[] data)
        {
            if (data == null)
                return;

            data[0 + offset] = (byte)((PayloadType >> 8) & 0xFFU);              // Payload Type MSB
            data[1 + offset] = (byte)((PayloadType >> 0) & 0xFFU);              // Payload Type LSB
            data[2 + offset] = (byte)((PayloadLength >> 8) & 0xFFU);            // Payload Length MSB
            data[3 + offset] = (byte)((PayloadLength >> 0) & 0xFFU);            // Payload Length LSB
        }
    } // public class RtpExtensionHeader
} // namespace fnecore
