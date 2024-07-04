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
*   Copyright (C) 2023 Bryan Biedenkapp, N2PLL
*
*/

using System;

using fnecore.EDAC;

namespace fnecore.DMR
{
    /// <summary>
    /// 
    /// </summary>
    public enum EmbeddedLCState
    {
        LCS_NONE,
        LCS_FIRST,
        LCS_SECOND,
        LCS_THIRD
    };

    /// <summary>
    /// Represents DMR embedded data.
    /// </summary>
    public class EmbeddedData
    {
        private EmbeddedLCState state;
        bool[] data;
        bool[] raw;

        /// <summary>
        /// Flag indicating whether or not the embedded data is valid.
        /// </summary>
        public bool IsValid
        {
            get;
            private set;
        }

        /// <summary>
        /// Full-link control opcode
        /// </summary>
        public byte FLCO;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="EmbeddedData"/> class.
        /// </summary>
        public EmbeddedData()
        {
            IsValid = false;
            FLCO = (byte)DMRFLCO.FLCO_GROUP;
            state = EmbeddedLCState.LCS_NONE;
            data = new bool[72];
            raw = new bool[128];
        }

        /// <summary>
        /// Unpack and error check an embedded LC.
        /// </summary>
        private void DecodeEmbeddedData()
        {
            // The data is unpacked downwards in columns
            bool[] data = new bool[128U];

            uint b = 0U;
            for (uint a = 0U; a < 128U; a++) {
                data[b] = this.raw[a];
                b += 16U;
                if (b > 127U)
                    b -= 127U;
            }

            // Hamming (16,11,4) check each row except the last one
            for (uint a = 0U; a < 112U; a += 16U) {
                if (!Hamming.decode16114(data, (int)a))
                    return;
            }

            // Check the parity bits
            for (uint a = 0U; a < 16U; a++) {
                bool parity = data[a + 0U] ^ data[a + 16U] ^ data[a + 32U] ^ data[a + 48U] ^ data[a + 64U] ^ data[a + 80U] ^ data[a + 96U] ^ data[a + 112U];
                if (parity)
                    return;
            }

            // We have passed the Hamming check so extract the actual payload
            b = 0U;
            for (uint a = 0U; a < 11U; a++, b++)
                this.data[b] = data[a];
            for (uint a = 16U; a < 27U; a++, b++)
                this.data[b] = data[a];
            for (uint a = 32U; a < 42U; a++, b++)
                this.data[b] = data[a];
            for (uint a = 48U; a < 58U; a++, b++)
                this.data[b] = data[a];
            for (uint a = 64U; a < 74U; a++, b++)
                this.data[b] = data[a];
            for (uint a = 80U; a < 90U; a++, b++)
                this.data[b] = data[a];
            for (uint a = 96U; a < 106U; a++, b++)
                this.data[b] = data[a];

            // Extract the 5 bit CRC
            uint crc = 0U;
            if (data[42])  crc += 16U;
            if (data[58])  crc += 8U;
            if (data[74])  crc += 4U;
            if (data[90])  crc += 2U;
            if (data[106]) crc += 1U;

            // Now CRC check this
            if (!CRC.CheckFiveBit(this.data, crc))
                return;

            IsValid = true;

            // Extract the FLCO
            byte flco = 0;
            FneUtils.BitsToByteBE(this.data, 0, ref flco);
            FLCO = (byte)(flco & 0x3FU);
        }

        /// <summary>
        /// Pack and FEC for an embedded LC.
        /// </summary>
        private void EncodeEmbeddedData()
        {
            uint crc = 0;
            CRC.EncodeFiveBit(this.data, ref crc);

            bool[] data = new bool[128U];

            data[106U] = (crc & 0x01U) == 0x01U;
            data[90U] = (crc & 0x02U) == 0x02U;
            data[74U] = (crc & 0x04U) == 0x04U;
            data[58U] = (crc & 0x08U) == 0x08U;
            data[42U] = (crc & 0x10U) == 0x10U;

            uint b = 0U;
            for (uint a = 0U; a < 11U; a++, b++)
                data[a] = this.data[b];
            for (uint a = 16U; a < 27U; a++, b++)
                data[a] = this.data[b];
            for (uint a = 32U; a < 42U; a++, b++)
                data[a] = this.data[b];
            for (uint a = 48U; a < 58U; a++, b++)
                data[a] = this.data[b];
            for (uint a = 64U; a < 74U; a++, b++)
                data[a] = this.data[b];
            for (uint a = 80U; a < 90U; a++, b++)
                data[a] = this.data[b];
            for (uint a = 96U; a < 106U; a++, b++)
                data[a] = this.data[b];

            // Hamming (16,11,4) check each row except the last one
            for (uint a = 0U; a < 112U; a += 16U)
                Hamming.encode16114(ref data, (int)a);

            // Add the parity bits for each column
            for (uint a = 0U; a < 16U; a++)
                data[a + 112U] = data[a + 0U] ^ data[a + 16U] ^ data[a + 32U] ^ data[a + 48U] ^ data[a + 64U] ^ data[a + 80U] ^ data[a + 96U];

            // The data is packed downwards in columns
            b = 0U;
            for (uint a = 0U; a < 128U; a++) {
                this.raw[a] = data[b];
                b += 16U;
                if (b > 127U)
                    b -= 127U;
            }
        }

        /// <summary>
        /// Add LC data (which may consist of 4 blocks) to the data store.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="lcss"></param>
        /// <returns></returns>
        public bool AddData(ref byte[] data, byte lcss)
        {
            if (data == null)
                throw new NullReferenceException("data");

            bool[] rawData = new bool[40U];
            FneUtils.ByteToBitsBE(data[14U], ref rawData, 0);
            FneUtils.ByteToBitsBE(data[15U], ref rawData, 8);
            FneUtils.ByteToBitsBE(data[16U], ref rawData, 16);
            FneUtils.ByteToBitsBE(data[17U], ref rawData, 24);
            FneUtils.ByteToBitsBE(data[18U], ref rawData, 32);

            // Is this the first block of a 4 block embedded LC ?
            if (lcss == 1U) {
                for (uint a = 0U; a < 32U; a++)
                    this.raw[a] = rawData[a + 4U];

                // Show we are ready for the next LC block
                state = EmbeddedLCState.LCS_FIRST;
                IsValid = false;

                return false;
            }

            // Is this the 2nd block of a 4 block embedded LC ?
            if (lcss == 3U && state == EmbeddedLCState.LCS_FIRST) {
                for (uint a = 0U; a < 32U; a++)
                    this.raw[a + 32U] = rawData[a + 4U];

                // Show we are ready for the next LC block
                state = EmbeddedLCState.LCS_SECOND;

                return false;
            }

            // Is this the 3rd block of a 4 block embedded LC ?
            if (lcss == 3U && state == EmbeddedLCState.LCS_SECOND) {
                for (uint a = 0U; a < 32U; a++)
                    this.raw[a + 64U] = rawData[a + 4U];

                // Show we are ready for the final LC block
                state = EmbeddedLCState.LCS_THIRD;

                return false;
            }

            // Is this the final block of a 4 block embedded LC ?
            if (lcss == 2U && state == EmbeddedLCState.LCS_THIRD) {
                for (uint a = 0U; a < 32U; a++)
                    this.raw[a + 96U] = rawData[a + 4U];

                // Show that we're not ready for any more data
                state = EmbeddedLCState.LCS_NONE;

                // Process the complete data block
                DecodeEmbeddedData();
                if (IsValid)
                    EncodeEmbeddedData();

                return IsValid;
            }

            return false;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="data"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public byte GetData(ref byte[] data, byte n)
        {
            if (data == null)
                throw new NullReferenceException("data");

            if (n >= 1U && n < 5U) {
                n--;

                bool[] bits = new bool[40U];
                Buffer.BlockCopy(this.raw, n * 32, bits, 4, 32 * sizeof(bool));

                byte[] bytes = new byte[5U];
                FneUtils.BitsToByteBE(bits, 0, ref bytes[0U]);
                FneUtils.BitsToByteBE(bits, 8, ref bytes[1U]);
                FneUtils.BitsToByteBE(bits, 16, ref bytes[2U]);
                FneUtils.BitsToByteBE(bits, 24, ref bytes[3U]);
                FneUtils.BitsToByteBE(bits, 32, ref bytes[4U]);

                data[14U] = (byte)((data[14U] & 0xF0U) | (bytes[0U] & 0x0FU));
                data[15U] = bytes[1U];
                data[16U] = bytes[2U];
                data[17U] = bytes[3U];
                data[18U] = (byte)((data[18U] & 0x0FU) | (bytes[4U] & 0xF0U));

                switch (n) {
                case 0:
                    return 1;
                case 3:
                    return 2;
                default:
                    return 3;
                }
            }
            else {
                data[14U] &= (byte)0xF0U;
                data[15U] = (byte)0x00U;
                data[16U] = (byte)0x00U;
                data[17U] = (byte)0x00U;
                data[18U] &= (byte)0x0FU;

                return 0;
            }
        }

        /// <summary>Sets link control data.</summary>
        /// <param name="lc"></param>
        public void SetLC(LC lc)
        {
            lc.GetData(ref data);

            FLCO = lc.FLCO;
            IsValid = true;

            EncodeEmbeddedData();
        }

        /// <summary>Gets link control data.</summary>
        /// <returns></returns>
        public LC GetLC()
        {
            if (!IsValid)
                return null;

            if (FLCO != (byte)DMRFLCO.FLCO_GROUP && FLCO != (byte)DMRFLCO.FLCO_PRIVATE)
                return null;

            return new LC(data);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool GetRawData(ref byte[] data)
        {
            if (data == null)
                throw new NullReferenceException("data");

            if (!IsValid)
                return false;

            FneUtils.BitsToByteBE(this.data, 0, ref data[0U]);
            FneUtils.BitsToByteBE(this.data, 8, ref data[1U]);
            FneUtils.BitsToByteBE(this.data, 16, ref data[2U]);
            FneUtils.BitsToByteBE(this.data, 24, ref data[3U]);
            FneUtils.BitsToByteBE(this.data, 32, ref data[4U]);
            FneUtils.BitsToByteBE(this.data, 40, ref data[5U]);
            FneUtils.BitsToByteBE(this.data, 48, ref data[6U]);
            FneUtils.BitsToByteBE(this.data, 56, ref data[7U]);
            FneUtils.BitsToByteBE(this.data, 64, ref data[8U]);

            return true;
        }

        /// <summary>
        /// Helper to reset data values to defaults.
        /// </summary>
        public void Reset()
        {
            state = EmbeddedLCState.LCS_NONE;
            IsValid = false;
        }
    } // public class EmbeddedData
} // namespace fnecore.DMR
