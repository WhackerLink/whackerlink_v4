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
    public class RtpHeader
    {
        private Random rand;
        private static DateTime start = DateTime.Now;
        private static uint previousTS = Constants.InvalidTS;

        private byte version;
        private bool padding;
        private byte cc;

        /// <summary>
        /// RTP Protocol Version.
        /// </summary>
        public byte Version { get => version; }

        /// <summary>
        /// Flag indicating if the packet has trailing padding.
        /// </summary>
        public bool Padding { get => padding; }

        /// <summary>
        /// Flag indicating the presense of an extension header.
        /// </summary>
        public bool Extension { get; set; }

        /// <summary>
        /// Count of contributing source IDs that follow the SSRC.
        /// </summary>
        public byte CSRCCount { get => cc; }

        /// <summary>
        /// Flag indicating application-specific behavior.
        /// </summary>
        public bool Marker { get; set; }

        /// <summary>
        /// Format of the payload contained within the packet.
        /// </summary>
        public byte PayloadType { get; set; }

        /// <summary>
        /// Sequence number for the RTP packet.
        /// </summary>
        public ushort Sequence { get; set; }

        /// <summary>
        /// RTP packet timestamp.
        /// </summary>
        public uint Timestamp { get; set; }

        /// <summary>
        /// Synchronization Source ID.
        /// </summary>
        public uint SSRC { get; set; }

        /*
        ** Methods
        */
        /// <summary>
        /// Initializes a new instance of the <see cref="RtpHeader"/> class.
        /// </summary>
        /// <param name="noIncrement"></param>
        public RtpHeader()
        {
            // bryanb: this isn't perfect -- but we don't need cryptographically
            // secure numbers
            rand = new Random(Guid.NewGuid().GetHashCode());

            version = 2;
            padding = false;
            Extension = false;
            cc = 0;
            Marker = false;
            PayloadType = 0;
            Sequence = 0;
            Timestamp = Constants.InvalidTS;
            SSRC = 0;
        }

        /// <summary>
        /// Decode a RTP header.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool Decode(byte[] data)
        {
            if (data == null)
                return false;

            // check for invalid version
            if (((data[0U] >> 6) & 0x03) != 0x02U) {
                return false;
            }

            version = (byte)((data[0] >> 6) & 0x03U);                           // RTP Version
            padding = ((data[0] & 0x20) == 0x20U);                              // Padding Flag
            Extension = ((data[0] & 0x10) == 0x10U);                            // Extension Header Flag
            cc = (byte)(data[0] & 0x0F);                                        // CSRC Count
            Marker = ((data[1] & 0x80) == 0x80U);                               // Marker Flag
            PayloadType = (byte)(data[1] & 0x7F);                               // Payload Type
            Sequence = (ushort)((data[2] << 8) | (data[3] << 0));               // Sequence

            Timestamp = FneUtils.ToUInt32(data, 4);                             // Timestamp
            SSRC = FneUtils.ToUInt32(data, 8);                                  // Synchronization Source ID

            return true;
        }

        /// <summary>
        /// Encode a RTP header.
        /// </summary>
        /// <param name="data"></param>
        public void Encode(ref byte[] data)
        {
            if (data == null)
                return;

            data[0] = (byte)((version << 6) +                                   // RTP Version
                (padding ? 0x20U : 0x00U) +                                     // Padding Flag
                (Extension ? 0x10U : 0x00U) +                                   // Extension Header Flag
                (cc & 0x0FU));                                                  // CSRC Count
            data[1] = (byte)((Marker ? 0x80U : 0x00U) +                         // Marker Flag
                (PayloadType & 0x7FU));                                         // Payload Type
            data[2] = (byte)((Sequence >> 8) & 0xFFU);                          // Sequence MSB
            data[3] = (byte)((Sequence >> 0) & 0xFFU);                          // Sequence LSB

            if (previousTS == Constants.InvalidTS)
            {
                TimeSpan timeSinceStart = DateTime.Now - start;
                ulong microSeconds = (ulong)(timeSinceStart.Ticks * Constants.RtpGenericClockRate);
                Timestamp = (uint)(microSeconds);
                previousTS = Timestamp;
            }
            else
            {
                Timestamp = previousTS + (Constants.RtpGenericClockRate / 133);
                previousTS = Timestamp;
            }

            FneUtils.WriteBytes(Timestamp, ref data, 4);                        // Timestamp
            FneUtils.WriteBytes(SSRC, ref data, 8);                             // Synchronization Source ID
        }

        /// <summary>
        /// Helper to reset the start timestamp.
        /// </summary>
        public static void ResetStartTime()
        {
            start = DateTime.Now;
            previousTS = Constants.InvalidTS;
        }
    } // public class RtpHeader
} // namespace fnecore
