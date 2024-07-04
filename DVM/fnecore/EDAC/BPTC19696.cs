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

namespace fnecore.EDAC
{
    /// <summary>
    /// Implements Block Product Turbo Code (196,96) FEC.
    /// </summary>
    public sealed class BPTC19696
    {
        private bool[] rawData;
        private bool[] deInterData;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="BPTC19696"/> class.
        /// </summary>
        public BPTC19696()
        {
            rawData = new bool[196];
            deInterData = new bool[196];
        }

        /// <summary>
        /// Decode BPTC (196,96) FEC.
        /// </summary>
        /// <param name="_in"></param>
        /// <param name="_out"></param>
        public void Decode(byte[] _in, out byte[] _out)
        {
            _out = null;
            if (_in == null)
                throw new NullReferenceException("_in");

            // Get the raw binary
            DecodeExtractBinary(_in);

            // Deinterleave
            DecodeDeInterleave();

            // Error check
            DecodeErrorCheck();

            // Extract Data
            _out = DecodeExtractData();
        }

        /// <summary>
        /// Encode BPTC (196,96) FEC.
        /// </summary>
        /// <param name="_in"></param>
        /// <param name="_out"></param>
        public void Encode(byte[] _in, out byte[] _out)
        {
            _out = null;
            if (_in == null)
                throw new NullReferenceException("_in");

            // Extract Data
            EncodeExtractData(_in);

            // Error check
            EncodeErrorCheck();

            // Interleave
            EncodeInterleave();

            // Get the raw binary
            _out = EncodeExtractBinary();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        private void DecodeExtractBinary(byte[] data)
        {
            // First block
            FneUtils.ByteToBitsBE(data[0U], ref rawData, 0);
            FneUtils.ByteToBitsBE(data[1U], ref rawData, 8);
            FneUtils.ByteToBitsBE(data[2U], ref rawData, 16);
            FneUtils.ByteToBitsBE(data[3U], ref rawData, 24);
            FneUtils.ByteToBitsBE(data[4U], ref rawData, 32);
            FneUtils.ByteToBitsBE(data[5U], ref rawData, 40);
            FneUtils.ByteToBitsBE(data[6U], ref rawData, 48);
            FneUtils.ByteToBitsBE(data[7U], ref rawData, 56);
            FneUtils.ByteToBitsBE(data[8U], ref rawData, 64);
            FneUtils.ByteToBitsBE(data[9U], ref rawData, 72);
            FneUtils.ByteToBitsBE(data[10U], ref rawData, 80);
            FneUtils.ByteToBitsBE(data[11U], ref rawData, 88);
            FneUtils.ByteToBitsBE(data[12U], ref rawData, 96);

            // Handle the two bits
            bool[] bits = new bool[8];
            FneUtils.ByteToBitsBE(data[20U], ref bits, 0);
            rawData[98U] = bits[6U];
            rawData[99U] = bits[7U];

            // Second block
            FneUtils.ByteToBitsBE(data[21U], ref rawData, 100);
            FneUtils.ByteToBitsBE(data[22U], ref rawData, 108);
            FneUtils.ByteToBitsBE(data[23U], ref rawData, 116);
            FneUtils.ByteToBitsBE(data[24U], ref rawData, 124);
            FneUtils.ByteToBitsBE(data[25U], ref rawData, 132);
            FneUtils.ByteToBitsBE(data[26U], ref rawData, 140);
            FneUtils.ByteToBitsBE(data[27U], ref rawData, 148);
            FneUtils.ByteToBitsBE(data[28U], ref rawData, 156);
            FneUtils.ByteToBitsBE(data[29U], ref rawData, 164);
            FneUtils.ByteToBitsBE(data[30U], ref rawData, 172);
            FneUtils.ByteToBitsBE(data[31U], ref rawData, 180);
            FneUtils.ByteToBitsBE(data[32U], ref rawData, 188);
        }

        /// <summary>
        /// 
        /// </summary>
        private void DecodeErrorCheck()
        {
            bool fixing;
            uint count = 0U;
            do
            {
                fixing = false;

                // Run through each of the 15 columns
                bool[] col = new bool[13];
                for (uint c = 0U; c < 15U; c++)
                {
                    uint pos = c + 1U;
                    for (uint a = 0U; a < 13U; a++)
                    {
                        col[a] = deInterData[pos];
                        pos = pos + 15U;
                    }

                    if (Hamming.decode1393(col))
                    {
                        //uint pos = c + 1U;
                        pos = c + 1U; // bryanb: this may be a bad port...
                        for (uint a = 0U; a < 13U; a++)
                        {
                            deInterData[pos] = col[a];
                            pos = pos + 15U;
                        }

                        fixing = true;
                    }
                }

                // Run through each of the 9 rows containing data
                for (uint r = 0U; r < 9U; r++)
                {
                    uint pos = (r * 15U) + 1U;
                    if (Hamming.decode15113_2(deInterData, (int)pos))
                        fixing = true;
                }

                count++;
            } while (fixing && count < 5U);
        }

        /// <summary>
        /// 
        /// </summary>
        private void DecodeDeInterleave()
        {
            for (uint i = 0U; i < 196U; i++)
                deInterData[i] = false;

            // The first bit is R(3) which is not used so can be ignored
            for (uint a = 0U; a < 196U; a++)
            {
                // Calculate the interleave sequence
                uint interleaveSequence = (a * 181U) % 196U;
                // Shuffle the data
                deInterData[a] = rawData[interleaveSequence];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private byte[] DecodeExtractData()
        {
            bool[] bData = new bool[96];
            uint pos = 0U;
            for (uint a = 4U; a <= 11U; a++, pos++)
                bData[pos] = deInterData[a];

            for (uint a = 16U; a <= 26U; a++, pos++)
                bData[pos] = deInterData[a];

            for (uint a = 31U; a <= 41U; a++, pos++)
                bData[pos] = deInterData[a];

            for (uint a = 46U; a <= 56U; a++, pos++)
                bData[pos] = deInterData[a];

            for (uint a = 61U; a <= 71U; a++, pos++)
                bData[pos] = deInterData[a];

            for (uint a = 76U; a <= 86U; a++, pos++)
                bData[pos] = deInterData[a];

            for (uint a = 91U; a <= 101U; a++, pos++)
                bData[pos] = deInterData[a];

            for (uint a = 106U; a <= 116U; a++, pos++)
                bData[pos] = deInterData[a];

            for (uint a = 121U; a <= 131U; a++, pos++)
                bData[pos] = deInterData[a];

            byte[] data = new byte[12];
            FneUtils.BitsToByteBE(bData, 0, ref data[0]);
            FneUtils.BitsToByteBE(bData, 8, ref data[1]);
            FneUtils.BitsToByteBE(bData, 16, ref data[2]);
            FneUtils.BitsToByteBE(bData, 24, ref data[3]);
            FneUtils.BitsToByteBE(bData, 32, ref data[4]);
            FneUtils.BitsToByteBE(bData, 40, ref data[5]);
            FneUtils.BitsToByteBE(bData, 48, ref data[6]);
            FneUtils.BitsToByteBE(bData, 56, ref data[7]);
            FneUtils.BitsToByteBE(bData, 64, ref data[8]);
            FneUtils.BitsToByteBE(bData, 72, ref data[9]);
            FneUtils.BitsToByteBE(bData, 80, ref data[10]);
            FneUtils.BitsToByteBE(bData, 88, ref data[11]);

            return data;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        private void EncodeExtractData(byte[] data)
        {
            bool[] bData = new bool[96];
            FneUtils.ByteToBitsBE(data[0U], ref bData, 0);
            FneUtils.ByteToBitsBE(data[1U], ref bData, 8);
            FneUtils.ByteToBitsBE(data[2U], ref bData, 16);
            FneUtils.ByteToBitsBE(data[3U], ref bData, 24);
            FneUtils.ByteToBitsBE(data[4U], ref bData, 32);
            FneUtils.ByteToBitsBE(data[5U], ref bData, 40);
            FneUtils.ByteToBitsBE(data[6U], ref bData, 48);
            FneUtils.ByteToBitsBE(data[7U], ref bData, 56);
            FneUtils.ByteToBitsBE(data[8U], ref bData, 64);
            FneUtils.ByteToBitsBE(data[9U], ref bData, 72);
            FneUtils.ByteToBitsBE(data[10U], ref bData, 80);
            FneUtils.ByteToBitsBE(data[11U], ref bData, 88);

            for (uint i = 0U; i < 196U; i++)
                deInterData[i] = false;

            uint pos = 0U;
            for (uint a = 4U; a <= 11U; a++, pos++)
                deInterData[a] = bData[pos];

            for (uint a = 16U; a <= 26U; a++, pos++)
                deInterData[a] = bData[pos];

            for (uint a = 31U; a <= 41U; a++, pos++)
                deInterData[a] = bData[pos];

            for (uint a = 46U; a <= 56U; a++, pos++)
                deInterData[a] = bData[pos];

            for (uint a = 61U; a <= 71U; a++, pos++)
                deInterData[a] = bData[pos];

            for (uint a = 76U; a <= 86U; a++, pos++)
                deInterData[a] = bData[pos];

            for (uint a = 91U; a <= 101U; a++, pos++)
                deInterData[a] = bData[pos];

            for (uint a = 106U; a <= 116U; a++, pos++)
                deInterData[a] = bData[pos];

            for (uint a = 121U; a <= 131U; a++, pos++)
                deInterData[a] = bData[pos];
        }

        /// <summary>
        /// 
        /// </summary>
        private void EncodeInterleave()
        {
            for (uint i = 0U; i < 196U; i++)
                rawData[i] = false;

            // The first bit is R(3) which is not used so can be ignored
            for (uint a = 0U; a < 196U; a++)
            {
                // Calculate the interleave sequence
                uint interleaveSequence = (a * 181U) % 196U;
                // Unshuffle the data
                rawData[interleaveSequence] = deInterData[a];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void EncodeErrorCheck()
        {
            // Run through each of the 9 rows containing data
            for (uint r = 0U; r < 9U; r++)
            {
                uint pos = (r * 15U) + 1U;
                Hamming.encode15113_2(ref deInterData, (int)pos);
            }

            // Run through each of the 15 columns
            bool[] col = new bool[13];
            for (uint c = 0U; c < 15U; c++)
            {
                uint pos = c + 1U;
                for (uint a = 0U; a < 13U; a++)
                {
                    col[a] = deInterData[pos];
                    pos = pos + 15U;
                }

                Hamming.encode1393(ref col);

                pos = c + 1U;
                for (uint a = 0U; a < 13U; a++)
                {
                    deInterData[pos] = col[a];
                    pos = pos + 15U;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private byte[] EncodeExtractBinary()
        {
            byte[] data = new byte[33];

            // First block
            FneUtils.BitsToByteBE(rawData, 0, ref data[0]);
            FneUtils.BitsToByteBE(rawData, 8, ref data[1]);
            FneUtils.BitsToByteBE(rawData, 16, ref data[2]);
            FneUtils.BitsToByteBE(rawData, 24, ref data[3]);
            FneUtils.BitsToByteBE(rawData, 32, ref data[4]);
            FneUtils.BitsToByteBE(rawData, 40, ref data[5]);
            FneUtils.BitsToByteBE(rawData, 48, ref data[6]);
            FneUtils.BitsToByteBE(rawData, 56, ref data[7]);
            FneUtils.BitsToByteBE(rawData, 64, ref data[8]);
            FneUtils.BitsToByteBE(rawData, 72, ref data[9]);
            FneUtils.BitsToByteBE(rawData, 80, ref data[10]);
            FneUtils.BitsToByteBE(rawData, 88, ref data[11]);

            // Handle the two bits
            byte val = 0x00;
            FneUtils.BitsToByteBE(rawData, 96, ref val);
            data[12U] = (byte)((data[12U] & 0x3FU) | ((val >> 0) & 0xC0U));
            data[20U] = (byte)((data[20U] & 0xFCU) | ((val >> 4) & 0x03U));

            // Second block
            FneUtils.BitsToByteBE(rawData, 100, ref data[21]);
            FneUtils.BitsToByteBE(rawData, 108, ref data[22]);
            FneUtils.BitsToByteBE(rawData, 116, ref data[23]);
            FneUtils.BitsToByteBE(rawData, 124, ref data[24]);
            FneUtils.BitsToByteBE(rawData, 132, ref data[25]);
            FneUtils.BitsToByteBE(rawData, 140, ref data[26]);
            FneUtils.BitsToByteBE(rawData, 148, ref data[27]);
            FneUtils.BitsToByteBE(rawData, 156, ref data[28]);
            FneUtils.BitsToByteBE(rawData, 164, ref data[29]);
            FneUtils.BitsToByteBE(rawData, 172, ref data[30]);
            FneUtils.BitsToByteBE(rawData, 180, ref data[31]);
            FneUtils.BitsToByteBE(rawData, 188, ref data[32]);

            return data;
        }
    } // public sealed class BPTC19696
} // namespace fnecore.EDAC
