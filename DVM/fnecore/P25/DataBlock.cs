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
    /// Represents the data block for PDU P25 packets.
    /// </summary>
    public class DataBlock
    {
        private Trellis trellis;

        private byte headerSap;

        private byte[] data;

        /// <summary>Sets the data block serial number.</summary>
        public byte SerialNo;

        /// <summary>Flag indicating this is the last block in a sequence of block.</summary>
        public bool LastBlock;

        /// <summary>Logical link ID.</summary>
        public uint LLId;

        /// <summary>Service access point.</summary>
        public byte SAP;

        /// <summary>
        /// Data format.
        /// </summary>
        public byte Format;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="DataBlock"/> class.
        /// </summary>
        public DataBlock()
        {
            SerialNo = 0;
            LastBlock = false;
            LLId = 0;
            SAP = 0;

            trellis = new Trellis();
            Format = P25Defines.PDU_FMT_CONFIRMED;
            headerSap = 0;

            data = new byte[P25Defines.P25_PDU_CONFIRMED_DATA_LENGTH_BYTES];
        }

        /// <summary>
        /// Decodes P25 PDU data block.
        /// </summary>
        /// <param name="pduData"></param>
        /// <param name="header">Instance of the DataHeader class.</param>
        /// <returns>True, if data block was decoded, otherwise false.</returns>
        public bool Decode(byte[] pduData, DataHeader header)
        {
            if (pduData == null)
                throw new NullReferenceException("pduData");

            byte[] buffer = new byte[P25Defines.P25_PDU_CONFIRMED_LENGTH_BYTES];

            Format = header.Format;
            headerSap = header.SAP;

            // set these to reasonable defaults
            SerialNo = 0;
            LastBlock = false;
            LLId = 0;

            if (Format == P25Defines.PDU_FMT_CONFIRMED) {
                // decode 3/4 rate Trellis
                bool valid = trellis.Decode34(pduData, ref buffer);
                if (!valid) {
                    return false;
                }

                SerialNo = (byte)((buffer[0] & 0xFEU) >> 1);                        // Confirmed Data Serial No.
                ushort crc = (ushort)(((buffer[0] & 0x01U) << 8) + buffer[1]);      // CRC-9 Check Sum

                data = new byte[P25Defines.P25_PDU_CONFIRMED_DATA_LENGTH_BYTES];

                // if this is extended addressing and the first block decode the SAP and LLId
                if (headerSap == P25Defines.PDU_SAP_EXT_ADDR && SerialNo == 0U) {
                    SAP = (byte)(buffer[5U] & 0x3FU);                               // Service Access Point
                    LLId = (uint)((buffer[2U] << 16) + (buffer[3U] << 8) + buffer[4U]); // Logical Link ID

                    for (uint i = 0; i < P25Defines.P25_PDU_CONFIRMED_DATA_LENGTH_BYTES; i++)
                        data[i] = buffer[i + 2];                                    // Payload Data
                }
                else {
                    for (uint i = 0; i < P25Defines.P25_PDU_CONFIRMED_DATA_LENGTH_BYTES; i++)
                        data[i] = buffer[i + 2];                                    // Payload Data
                }

                // compute CRC-9 for the packet
                ushort calculated = CRC.Crc9(buffer, 144);
                if (((crc ^ calculated) != 0) && ((crc ^ calculated) != 0x1FFU))
                    System.Diagnostics.Trace.WriteLine($"P25_DUID_PDU, fmt = {Format}, invalid crc = {crc} != {calculated}");
            }
            else if ((Format == P25Defines.PDU_FMT_UNCONFIRMED) || (Format == P25Defines.PDU_FMT_RSP) || (Format == P25Defines.PDU_FMT_AMBT))
            {
                // decode 1/2 rate Trellis
                bool valid = trellis.Decode12(pduData, ref buffer);
                if (!valid)
                    return false;

                data = new byte[P25Defines.P25_PDU_UNCONFIRMED_LENGTH_BYTES];
                for (uint i = 0; i < P25Defines.P25_PDU_UNCONFIRMED_LENGTH_BYTES; i++)
                    data[i] = buffer[i];                                            // Payload Data
            }
            else
                return false; // unknown format value

            return true;
        }

        /// <summary>
        /// Encodes a P25 PDU data block.
        /// </summary>
        /// <param name="pduData">Buffer to encode data block to.</param>
        public void Encode(ref byte[] pduData)
        {
            if (pduData == null)
                throw new NullReferenceException("pduData");

            if (Format == P25Defines.PDU_FMT_CONFIRMED)
            {
                byte[] buffer = new byte[P25Defines.P25_PDU_CONFIRMED_LENGTH_BYTES];

                buffer[0U] = (byte)((SerialNo << 1) & 0xFEU);                       // Confirmed Data Serial No.

                // if this is extended addressing and the first block decode the SAP and LLId
                if (headerSap == P25Defines.PDU_SAP_EXT_ADDR && SerialNo == 0U)
                {
                    buffer[5U] = (byte)(SAP & 0x3FU);                               // Service Access Point

                    buffer[2U] = (byte)((LLId >> 16) & 0xFFU);                      // Logical Link ID
                    buffer[3U] = (byte)((LLId >> 8) & 0xFFU);
                    buffer[4U] = (byte)((LLId >> 0) & 0xFFU);

                    for (uint i = 0; i < P25Defines.P25_PDU_CONFIRMED_DATA_LENGTH_BYTES - 4U; i++)
                        buffer[i + 6] = data[i];
                }
                else
                {
                    for (uint i = 0; i < P25Defines.P25_PDU_CONFIRMED_DATA_LENGTH_BYTES; i++)
                        buffer[i + 2] = data[i];
                }

                ushort crc = CRC.Crc9(buffer, 144);
                buffer[0U] = (byte)(buffer[0U] + ((crc >> 8) & 0x01U));             // CRC-9 Check Sum (b8)
                buffer[1U] = (byte)(crc & 0xFFU);                                   // CRC-9 Check Sum (b0 - b7)

                trellis.Encode34(buffer, ref pduData);
            }
            else if (Format == P25Defines.PDU_FMT_UNCONFIRMED || Format == P25Defines.PDU_FMT_RSP || Format == P25Defines.PDU_FMT_AMBT)
            {
                byte[] buffer = new byte[P25Defines.P25_PDU_UNCONFIRMED_LENGTH_BYTES];
                for (uint i = 0; i < P25Defines.P25_PDU_UNCONFIRMED_LENGTH_BYTES - 4U; i++)
                    buffer[i] = data[i];

                trellis.Encode12(buffer, ref pduData);
            }
            else
                return; // unknown format value
        }

        /// <summary>
        /// Sets the raw data stored in the data block.
        /// </summary>
        /// <param name="buffer"></param>
        public void SetData(byte[] buffer)
        {
            if (buffer == null)
                throw new NullReferenceException("buffer");

            if (Format == P25Defines.PDU_FMT_CONFIRMED)
            {
                for (uint i = 0; i < P25Defines.P25_PDU_CONFIRMED_DATA_LENGTH_BYTES; i++)
                    data[i] = buffer[i];
            }
            else if (Format == P25Defines.PDU_FMT_UNCONFIRMED || Format == P25Defines.PDU_FMT_RSP || Format == P25Defines.PDU_FMT_AMBT)
            {
                for (uint i = 0; i < P25Defines.P25_PDU_UNCONFIRMED_LENGTH_BYTES; i++)
                    data[i] = buffer[i];
            }
            else
                return; // unknown format value
        }

        /// <summary>
        /// Gets the raw data stored in the data block.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public uint GetData(ref byte[] buffer, uint offset)
        {
            if (buffer == null)
                throw new NullReferenceException("buffer");

            if (Format == P25Defines.PDU_FMT_CONFIRMED)
            {
                for (uint i = 0; i < P25Defines.P25_PDU_CONFIRMED_DATA_LENGTH_BYTES; i++)
                    buffer[i + offset] = data[i];
                return P25Defines.P25_PDU_CONFIRMED_DATA_LENGTH_BYTES;
            }
            else if (Format == P25Defines.PDU_FMT_UNCONFIRMED || Format == P25Defines.PDU_FMT_RSP || Format == P25Defines.PDU_FMT_AMBT)
            {
                for (uint i = 0; i < P25Defines.P25_PDU_UNCONFIRMED_LENGTH_BYTES; i++)
                    buffer[i + offset] = data[i];
                return P25Defines.P25_PDU_UNCONFIRMED_LENGTH_BYTES;
            }
            else
                return 0; // unknown format value
        }
    } // public class DataBlock
} // namespace fnecore.P25
