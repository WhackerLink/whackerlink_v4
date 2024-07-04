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

using fnecore.EDAC;

namespace fnecore.P25
{
    /// <summary>
    /// Represents the data header for PDU P25 packets.
    /// </summary>
    public class DataHeader
    {
        private Trellis trellis;
        private byte blocksToFollow;
        private byte padCount;
        private uint dataOctets;

        private byte[] data;

        /// <summary>
        /// Flag indicating if acknowledgement is needed.
        /// </summary>
        public bool AckNeeded;

        /// <summary>
        /// Flag indicating if this is an outbound data packet.
        /// </summary>
        public bool Outbound;

        /// <summary>
        /// Data packet format.
        /// </summary>
        public byte Format;

        /// <summary>
        /// Service access point.
        /// </summary>
        public byte SAP;

        /// <summary>
        /// Manufacturer ID.
        /// </summary>
        public byte MFId;

        /// <summary>
        /// Logical link ID.
        /// </summary>
        public uint LLId;

        /// <summary>
        /// Flag indicating whether or not this data packet is a full message.
        /// </summary>
        /// <remarks>When a response header, this represents the extended flag.</summary>
        public bool FullMessage;

        /// <summary>
        /// Synchronize Flag.
        /// </summary>
        public bool Synchronize;

        /// <summary>
        /// Fragment Sequence Number.
        /// </summary>
        public byte FSN;

        /// <summary>
        /// Send Sequence Number.
        /// </summary>
        public byte Ns;

        /// <summary>
        /// Flag indicating whether or not this is the last fragment in a message.
        /// </summary>
        public bool LastFragment;

        /// <summary>
        /// Offset of the header.
        /// </summary>
        public byte HeaderOffset;

        /** Response Data */
        /// <summary>
        /// Source Logical link ID.
        /// </summary>
        public uint SrcLLId;

        /// <summary>
        /// Response class.
        /// </summary>
        public byte ResponseClass;

        /// <summary>
        /// Response type.
        /// </summary>
        public byte ResponseType;

        /// <summary>
        /// Response status.
        /// </summary>
        public byte ResponseStatus;

        /** AMBT Data */
        /// <summary>
        /// Alternate Trunking Block Opcode
        /// </summary>
        public byte AMBTOpcode;

        /// <summary>
        /// Alternate Trunking Block Field 8
        /// </summary>
        public byte AMBTField8;

        /// <summary>
        /// Alternate Trunking Block Field 9
        /// </summary>
        public byte AMBTField9;

        /// <summary>
        /// Total number of blocks to follow this header.
        /// </summary>
        public byte BlocksToFollow
        {
            get { return blocksToFollow; }
            set
            {
                this.blocksToFollow = value;

                // recalculate count of data octets
                if (Format == P25Defines.PDU_FMT_CONFIRMED)
                {
                    dataOctets = (uint)(16 * blocksToFollow - 4 - padCount);
                }
                else
                {
                    dataOctets = (uint)(12 * blocksToFollow - 4 - padCount);
                }
            }
        }

        /// <summary>
        /// Count of block padding.
        /// </summary>
        public byte PadCount
        {
            get { return padCount; }
            set
            {
                padCount = value;

                // recalculate count of data octets
                if (Format == P25Defines.PDU_FMT_CONFIRMED)
                {
                    dataOctets = (uint)(16 * blocksToFollow - 4 - padCount);
                }
                else
                {
                    dataOctets = (uint)(12 * blocksToFollow - 4 - padCount);
                }
            }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="DataHeader"/> class.
        /// </summary>
        public DataHeader()
        {
            trellis = new Trellis();
            Reset();
        }

        /// <summary>
        /// Decodes P25 PDU data header.
        /// </summary>
        /// <param name="pduData"></param>
        /// <returns>True, if PDU data header was decoded, otherwise false.</returns>
        public bool Decode(byte[] pduData)
        {
            if (pduData == null)
                throw new NullReferenceException("pduData");

            // decode 1/2 rate Trellis & check CRC-CCITT 16
            bool valid = trellis.Decode12(pduData, ref data);
            if (valid)
                valid = CRC.CheckCCITT162(data, P25Defines.P25_PDU_HEADER_LENGTH_BYTES);
            if (!valid)
            {
                return false;
            }

            AckNeeded = (data[0U] & 0x40U) == 0x40U;                                // Acknowledge Needed
            Outbound = (data[0U] & 0x20U) == 0x20U;                                 // Inbound/Outbound
            Format = (byte)(data[0U] & 0x1FU);                                      // Packet Format

            SAP = (byte)(data[1U] & 0x3FU);                                         // Service Access Point

            MFId = data[2U];                                                        // Mfg Id.

            LLId = (uint)((data[3U] << 16) + (data[4U] << 8) + data[5U]);           // Logical Link ID

            FullMessage = (data[6U] & 0x80U) == 0x80U;                              // Full Message Flag
            blocksToFollow = (byte)(data[6U] & 0x7FU);                              // Block Frames to Follow

            padCount = (byte)(data[7U] & 0x1FU);                                    // Pad Count
            if (Format == P25Defines.PDU_FMT_RSP || Format == P25Defines.PDU_FMT_AMBT) {
                padCount = 0;
            }

            if (Format == P25Defines.PDU_FMT_CONFIRMED) {
                dataOctets = (uint)(16 * blocksToFollow - 4 - padCount);
            }
            else {
                dataOctets = (uint)(12 * blocksToFollow - 4 - padCount);
            }

            switch (Format)
            {
                case P25Defines.PDU_FMT_CONFIRMED:
                    Synchronize = (data[8U] & 0x80U) == 0x80U;                      // Re-synchronize Flag

                    Ns = (byte)((data[8U] >> 4) & 0x07U);                           // Packet Sequence No.
                    FSN = (byte)(data[8U] & 0x07U);                                 // Fragment Sequence No.
                    LastFragment = (data[8U] & 0x08U) == 0x08U;                     // Last Fragment Flag

                    HeaderOffset = (byte)(data[9U] & 0x3FU);                        // Data Header Offset
                    break;
                case P25Defines.PDU_FMT_RSP:
                    AckNeeded = false;
                    SAP = P25Defines.PDU_SAP_USER_DATA;
                    ResponseClass = (byte)((data[1U] >> 6) & 0x03U);                // Response Class
                    ResponseType = (byte)((data[1U] >> 3) & 0x07U);                 // Response Type
                    ResponseStatus = (byte)(data[1U] & 0x07U);                      // Response Status
                    if (!FullMessage)
                    {
                        SrcLLId = (uint)((data[7U] << 16) + (data[8U] << 8) + data[9U]); // Source Logical Link ID
                    }
                    break;

                case P25Defines.PDU_FMT_AMBT:
                case P25Defines.PDU_FMT_UNCONFIRMED:
                default:
                    if (Format == P25Defines.PDU_FMT_AMBT)
                    {
                        AMBTOpcode = (byte)(data[7U] & 0x3FU);                      // AMBT Opcode
                        AMBTField8 = data[8U];                                      // AMBT Field 8
                        AMBTField9 = data[9U];                                      // AMBT Field 9
                    }

                    AckNeeded = false;
                    Synchronize = false;

                    Ns = 0;
                    FSN = 0;
                    HeaderOffset = 0;
                    break;
            }

            return true;
        }

        /// <summary>
        /// Encodes P25 PDU data header.
        /// </summary>
        /// <param name="pduData"></param>
        public void Encode(ref byte[] pduData)
        {
            if (pduData == null)
                throw new NullReferenceException("pduData");

            byte[] header = new byte[P25Defines.P25_PDU_HEADER_LENGTH_BYTES];

            if (Format == P25Defines.PDU_FMT_UNCONFIRMED || Format == P25Defines.PDU_FMT_RSP)
                AckNeeded = false;

            if (Format == P25Defines.PDU_FMT_CONFIRMED && !AckNeeded)
                AckNeeded = true; // force set this to true

            header[0U] = (byte)((AckNeeded ? 0x40U : 0x00U) +                       // Acknowledge Needed
                (Outbound ? 0x20U : 0x00U) +                                        // Inbound/Outbound
                (Format & 0x1FU));                                                  // Packet Format

            header[1U] = (byte)(SAP & 0x3FU);                                       // Service Access Point
            header[1U] |= 0xC0;

            header[2U] = MFId;                                                      // Mfg Id.

            header[3U] = (byte)((LLId >> 16) & 0xFFU);                              // Logical Link ID
            header[4U] = (byte)((LLId >> 8) & 0xFFU);
            header[5U] = (byte)((LLId >> 0) & 0xFFU);

            header[6U] = (byte)((FullMessage ? 0x80U : 0x00U) +                     // Full Message Flag
                (blocksToFollow & 0x7FU));                                          // Blocks Frames to Follow

            switch (Format)
            {
                case P25Defines.PDU_FMT_CONFIRMED:
                    header[7U] = (byte)(padCount & 0x1FU);                          // Pad Count
                    header[8U] = (byte)((Synchronize ? 0x80U : 0x00U) +             // Re-synchronize Flag
                        ((Ns & 0x07U) << 4) +                                       // Packet Sequence No.
                        (LastFragment ? 0x08U : 0x00U) +                            // Last Fragment Flag
                        (FSN & 0x07));                                              // Fragment Sequence No.

                    header[9U] = (byte)(HeaderOffset & 0x3FU);                      // Data Header Offset
                    break;
                case P25Defines.PDU_FMT_RSP:
                    header[1U] = (byte)(((ResponseClass & 0x03U) << 6) +            // Response Class
                        ((ResponseType & 0x07U) << 3) +                             // Response Type
                        ((ResponseStatus & 0x07U)));                                // Response Status
                    if (!FullMessage)
                    {
                        header[7U] = (byte)((SrcLLId >> 16) & 0xFFU);               // Source Logical Link ID
                        header[8U] = (byte)((SrcLLId >> 8) & 0xFFU);
                        header[9U] = (byte)((SrcLLId >> 0) & 0xFFU);
                    }
                    break;
                case P25Defines.PDU_FMT_AMBT:
                    header[7U] = (byte)(AMBTOpcode & 0x3FU);                        // AMBT Opcode
                    header[8U] = AMBTField8;                                        // AMBT Field 8
                    header[9U] = AMBTField9;                                        // AMBT Field 9
                    break;
                case P25Defines.PDU_FMT_UNCONFIRMED:
                default:
                    header[7U] = (byte)(padCount & 0x1FU);                          // Pad Count
                    header[8U] = 0x00;
                    header[9U] = (byte)(HeaderOffset & 0x3FU);                      // Data Header Offset
                    break;
            }

            // compute CRC-CCITT 16
            CRC.AddCCITT162(ref header, P25Defines.P25_PDU_HEADER_LENGTH_BYTES);

            // encode 1/2 rate Trellis
            trellis.Encode12(header, ref pduData);
        }

        /// <summary>
        /// Helper to reset data values to defaults.
        /// </summary>
        public void Reset()
        {
            AckNeeded = false;
            Outbound = false;

            Format = P25Defines.PDU_FMT_CONFIRMED;

            SAP = 0;
            MFId = P25Defines.P25_MFG_STANDARD;
            LLId = 0;

            FullMessage = true;
            blocksToFollow = 0;
            padCount = 0;

            dataOctets = 0;

            Synchronize = false;

            Ns = 0;
            FSN = 0;
            LastFragment = true;

            HeaderOffset = 0;

            SrcLLId = 0;
            ResponseClass = P25Defines.PDU_ACK_CLASS_NACK;
            ResponseType = P25Defines.PDU_ACK_TYPE_NACK_ILLEGAL;
            ResponseStatus = 0;

            AMBTOpcode = 0;
            AMBTField8 = 0;
            AMBTField9 = 0;

            data = new byte[P25Defines.P25_PDU_HEADER_LENGTH_BYTES];
        }

        /// <summary>
        /// Gets the total number of data octets.
        /// </summary>
        /// <returns></returns>
        public uint GetDataOctets()
        {
            return dataOctets;
        }

        /// <summary>
        /// Gets the raw header data.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public uint GetData(ref byte[] buffer, uint offset)
        {
            if (buffer == null)
                throw new NullReferenceException("buffer");

            for (uint i = 0; i < P25Defines.P25_PDU_HEADER_LENGTH_BYTES; i++)
                buffer[i + offset] = data[i];

            return P25Defines.P25_PDU_HEADER_LENGTH_BYTES;
        }
    } // public class DataHeader
} // namespace fnecore.P25
