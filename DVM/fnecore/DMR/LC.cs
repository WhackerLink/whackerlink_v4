// SPDX-License-Identifier: AGPL-3.0-only
/**
* Digital Voice Modem - Fixed Network Equipment Core Library
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / Fixed Network Equipment Core Library
* @derivedfrom MMDVMHost (https://github.com/g4klx/MMDVMHost)
* @license AGPLv3 License (https://opensource.org/licenses/AGPL-3.0)
*
*   Copyright (C) 2022 Bryan Biedenkapp, N2PLL
*
*/

using System;

namespace fnecore.DMR
{
    /// <summary>
    /// Represents DMR link control data.
    /// </summary>
    public class LC
    {
        private bool R;

        /// <summary>
        /// Flag indicating whether link protection is enabled.
        /// </summary>
        public bool PF;

        /// <summary>
        /// Full-link control opcode.
        /// </summary>
        public byte FLCO;

        /// <summary>
        /// Feature ID.
        /// </summary>
        public byte FID;

        /// <summary>
        /// Source ID.
        /// </summary>
        public uint SrcId;

        /// <summary>
        /// Destination ID.
        /// </summary>
        public uint DstId;

        /** Service Options */
        /// <summary>
        /// Flag indicating the emergency bits are set.
        /// </summary>
        public bool Emergency;

        /// <summary>
        /// Flag indicating that encryption is enabled.
        /// </summary>
        public bool Encrypted;

        /// <summary>
        /// Flag indicating broadcast operation.
        /// </summary>
        public bool Broadcast;

        /// <summary>
        /// Flag indicating OVCM operation.
        /// </summary>
        public bool OVCM;

        /// <summary>
        /// Priority level for the traffic.
        /// </summary>
        public byte Priority;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="LC"/> class.
        /// </summary>
        public LC()
        {
            PF = false;

            FLCO = 0;
            FID = 0;

            SrcId = 0;
            DstId = 0;

            Emergency = false;
            Encrypted = false;
            Broadcast = false;
            OVCM = false;
            Priority = 2;
            
            R = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LC"/> class.
        /// </summary>
        /// <param name="bytes"></param>
        public LC(byte[] bytes)
        {
            PF = (bytes[0U] & 0x80U) == 0x80U;
            R = (bytes[0U] & 0x40U) == 0x40U;

            FLCO = (byte)(bytes[0U] & 0x3FU);

            FID = bytes[1U];

            Emergency = (bytes[2U] & 0x80U) == 0x80U;                                 // Emergency Flag
            Encrypted = (bytes[2U] & 0x40U) == 0x40U;                                 // Encryption Flag
            Broadcast = (bytes[2U] & 0x08U) == 0x08U;                                 // Broadcast Flag
            OVCM = (bytes[2U] & 0x04U) == 0x04U;                                      // OVCM Flag
            Priority = (byte)(bytes[2U] & 0x03U);                                     // Priority

            DstId = (uint)(bytes[3U] << 16 | bytes[4U] << 8 | bytes[5U]);             // Destination Address
            SrcId = (uint)(bytes[6U] << 16 | bytes[7U] << 8 | bytes[8U]);             // Source Address
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LC"/> class.
        /// </summary>
        /// <param name="bits"></param>
        public LC(bool[] bits)
        {
            PF = bits[0U];
            R = bits[1U];

            byte temp1 = 0, temp2 = 0, temp3 = 0;
            FneUtils.BitsToByteBE(bits, 0, ref temp1);
            FLCO = (byte)(temp1 & 0x3FU);

            FneUtils.BitsToByteBE(bits, 8, ref temp2);
            FID = temp2;

            FneUtils.BitsToByteBE(bits, 16, ref temp3);

            Emergency = (temp3 & 0x80U) == 0x80U;                                   // Emergency Flag
            Encrypted = (temp3 & 0x40U) == 0x40U;                                   // Encryption Flag
            Broadcast = (temp3 & 0x08U) == 0x08U;                                   // Broadcast Flag
            OVCM = (temp3 & 0x04U) == 0x04U;                                        // OVCM Flag
            Priority = (byte)(temp3 & 0x03U);                                       // Priority

            byte d1 = 0, d2 = 0, d3 = 0;
            FneUtils.BitsToByteBE(bits, 24, ref d1);
            FneUtils.BitsToByteBE(bits, 32, ref d2);
            FneUtils.BitsToByteBE(bits, 40, ref d3);

            byte s1 = 0, s2 = 0, s3 = 0;
            FneUtils.BitsToByteBE(bits, 48, ref s1);
            FneUtils.BitsToByteBE(bits, 56, ref s2);
            FneUtils.BitsToByteBE(bits, 64, ref s3);

            SrcId = (uint)(s1 << 16 | s2 << 8 | s3);                                // Source Address
            DstId = (uint)(d1 << 16 | d2 << 8 | d3);                                // Destination Address
        }

        /// <summary>
        /// Gets LC data as bytes.
        /// </summary>
        /// <returns></returns>
        public byte[] GetBytes()
        {
            byte[] lcData = new byte[12];
            GetData(ref lcData);

            return lcData;
        }

        /// <summary>
        /// Gets LC data as bytes.
        /// </summary>
        /// <param name="bytes"></param>
        public void GetData(ref byte[] bytes)
        {
            if (bytes == null)
                throw new NullReferenceException("bytes");

            bytes[0U] = FLCO;

            if (PF)
                bytes[0U] |= (byte)0x80U;

            if (R)
                bytes[0U] |= (byte)0x40U;

            bytes[1U] = FID;

            bytes[2U] = (byte)((Emergency ? 0x80U : 0x00U) +                          // Emergency Flag
                (Encrypted ? 0x40U : 0x00U) +                                         // Encrypted Flag
                (Broadcast ? 0x08U : 0x00U) +                                         // Broadcast Flag
                (OVCM ? 0x04U : 0x00U) +                                              // OVCM Flag
                (Priority & 0x03U));                                                  // Priority

            bytes[3U] = (byte)(DstId >> 16);                                          // Destination Address
            bytes[4U] = (byte)(DstId >> 8);                                           // ..
            bytes[5U] = (byte)(DstId >> 0);                                           // ..

            bytes[6U] = (byte)(SrcId >> 16);                                          // Source Address
            bytes[7U] = (byte)(SrcId >> 8);                                           // ..
            bytes[8U] = (byte)(SrcId >> 0);                                           // ..
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="bits"></param>
        public void GetData(ref bool[] bits)
        {
            if (bits == null)
                throw new NullReferenceException("bits");

            byte[] bytes = new byte[9U];
            GetData(ref bytes);

            FneUtils.ByteToBitsBE(bytes[0U], ref bits, 0);
            FneUtils.ByteToBitsBE(bytes[1U], ref bits, 8);
            FneUtils.ByteToBitsBE(bytes[2U], ref bits, 16);
            FneUtils.ByteToBitsBE(bytes[3U], ref bits, 24);
            FneUtils.ByteToBitsBE(bytes[4U], ref bits, 32);
            FneUtils.ByteToBitsBE(bytes[5U], ref bits, 40);
            FneUtils.ByteToBitsBE(bytes[6U], ref bits, 48);
            FneUtils.ByteToBitsBE(bytes[7U], ref bits, 56);
            FneUtils.ByteToBitsBE(bytes[8U], ref bits, 64);
        }
    } // public class LC
} // namespace fnecore.DMR
