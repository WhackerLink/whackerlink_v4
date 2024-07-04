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

namespace fnecore.EDAC
{
    /// <summary>
    /// Implements 1/2 rate and 3/4 rate Trellis for DMR/P25.
    /// </summary>
    public sealed class Trellis
    {
        private static readonly uint[] INTERLEAVE_TABLE = new uint[98] {
            0, 1, 8,   9, 16, 17, 24, 25, 32, 33, 40, 41, 48, 49, 56, 57, 64, 65, 72, 73, 80, 81, 88, 89, 96, 97,
            2, 3, 10, 11, 18, 19, 26, 27, 34, 35, 42, 43, 50, 51, 58, 59, 66, 67, 74, 75, 82, 83, 90, 91,
            4, 5, 12, 13, 20, 21, 28, 29, 36, 37, 44, 45, 52, 53, 60, 61, 68, 69, 76, 77, 84, 85, 92, 93,
            6, 7, 14, 15, 22, 23, 30, 31, 38, 39, 46, 47, 54, 55, 62, 63, 70, 71, 78, 79, 86, 87, 94, 95
        };

        private static readonly byte[] ENCODE_TABLE_34 = new byte[64] {
            0,  8, 4, 12, 2, 10, 6, 14,
            4, 12, 2, 10, 6, 14, 0,  8,
            1,  9, 5, 13, 3, 11, 7, 15,
            5, 13, 3, 11, 7, 15, 1,  9,
            3, 11, 7, 15, 1,  9, 5, 13,
            7, 15, 1,  9, 5, 13, 3, 11,
            2, 10, 6, 14, 0,  8, 4, 12,
            6, 14, 0,  8, 4, 12, 2, 10
        };

        private static readonly byte[] ENCODE_TABLE_12 = new byte[16] {
            0,  15, 12,  3,
            4,  11,  8,  7,
            13,  2,  1, 14,
            9,   6,  5, 10
        };

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="Trellis"/> class.
        /// </summary>
        public Trellis()
        {
            /* stub */
        }

        /// <summary>
        /// Decodes 3/4 rate Trellis.
        /// </summary>
        /// <param name="data">Trellis symbol bytes.</param>
        /// <param name="payload">Output bytes.</param>
        /// <returns>True, if decoded, otherwise false.</returns>
        public bool Decode34(byte[] data, ref byte[] payload)
        {
            if (data == null)
                throw new NullReferenceException("data");
            if (payload == null)
                throw new NullReferenceException("payload");

            int[] dibits = new int[98U];
            Deinterleave(data, ref dibits);

            byte[] points = new byte[49U];
            DibitsToPoints(dibits, ref points);

            // check the original code
            byte[] tribits = new byte[49U];
            uint failPos = CheckCode34(points, ref tribits);
            if (failPos == 999U) {
                TribitsToBits(tribits, ref payload);
                return true;
            }

            byte[] savePoints = new byte[49U];
            for (uint i = 0U; i< 49U; i++)
                savePoints[i] = points[i];

            bool ret = FixCode34(points, failPos, ref payload);
            if (ret)
                return true;

            if (failPos == 0U)
                return false;

            // Backtrack one place for a last go
            return FixCode34(savePoints, failPos - 1U, ref payload);
        }

        /// <summary>
        /// Encodes 3/4 rate Trellis.
        /// </summary>
        /// <param name="payload">Input bytes.</param>
        /// <param name="data">Trellis symbol bytes.</param>
        public void Encode34(byte[] payload, ref byte[] data)
        {
            if (data == null)
                throw new NullReferenceException("data");
            if (payload == null)
                throw new NullReferenceException("payload");

            byte[] tribits = new byte[49U];
            BitsToTribits(payload, ref tribits);

            byte[] points = new byte[49U];
            byte state = 0;

            for (uint i = 0U; i < 49U; i++)
            {
                byte tribit = tribits[i];

                points[i] = ENCODE_TABLE_34[state * 8U + tribit];

                state = tribit;
            }

            int[] dibits = new int[98U];
            PointsToDibits(points, ref dibits);

            Interleave(dibits, ref data);
        }

        /// <summary>
        /// Decodes 1/2 rate Trellis.
        /// </summary>
        /// <param name="data">Trellis symbol bytes.</param>
        /// <param name="payload">Output bytes.</param>
        /// <returns>True, if decoded, otherwise false.</returns>
        public bool Decode12(byte[] data, ref byte[] payload)
        {
            if (data == null)
                throw new NullReferenceException("data");
            if (payload == null)
                throw new NullReferenceException("payload");

            int[] dibits = new int[98U];
            Deinterleave(data, ref dibits);

            byte[] points = new byte[49U];
            DibitsToPoints(dibits, ref points);

            // Check the original code
            byte[] bits = new byte[49U];
            uint failPos = CheckCode12(points, ref bits);
            if (failPos == 999U)
            {
                DibitsToBits(bits, ref payload);
                return true;
            }

            byte[] savePoints = new byte[49U];
            for (uint i = 0U; i < 49U; i++)
                savePoints[i] = points[i];

            bool ret = FixCode12(points, failPos, ref payload);
            if (ret)
                return true;

            if (failPos == 0U)
                return false;

            // Backtrack one place for a last go
            return FixCode12(savePoints, failPos - 1U, ref payload);
        }

        /// <summary>
        /// Encodes 1/2 rate Trellis.
        /// </summary>
        /// <param name="payload">Input bytes.</param>
        /// <param name="data">Trellis symbol bytes.</param>
        public void Encode12(byte[] payload, ref byte[] data)
        {
            if (data == null)
                throw new NullReferenceException("data");
            if (payload == null)
                throw new NullReferenceException("payload");

            byte[] bits = new byte[49U];
            BitsToDibits(payload, ref bits);

            byte[] points = new byte[49U];
            byte state = 0;

            for (uint i = 0U; i < 49U; i++)
            {
                byte bit = bits[i];

                points[i] = ENCODE_TABLE_12[state * 4U + bit];

                state = bit;
            }

            int[] dibits = new int[98U];
            PointsToDibits(points, ref dibits);

            Interleave(dibits, ref data);
        }

        /// <summary>
        /// Helper to deinterleave the input symbols into dibits.
        /// </summary>
        /// <param name="data">Trellis symbol bytes.</param>
        /// <param name="dibits">Dibits.</param>
        private void Deinterleave(byte[] data, ref int[] dibits)
        {
            for (uint i = 0U; i < 98U; i++) {
                uint n = i * 2U + 0U;
                bool b1 = FneUtils.ReadBit(data, n) != false;

                n = i * 2U + 1U;
                bool b2 = FneUtils.ReadBit(data, n) != false;

                int dibit;
                if (!b1 && b2)
                    dibit = +3;
                else if (!b1 && !b2)
                    dibit = +1;
                else if (b1 && !b2)
                    dibit = -1;
                else
                    dibit = -3;

                n = INTERLEAVE_TABLE[i];
                dibits[n] = dibit;
            }
        }

        /// <summary>
        /// Helper to interleave the input dibits into symbols.
        /// </summary>
        /// <param name="dibits">Dibits.</param>
        /// <param name="data">Trellis symbol bytes.</param>
        private void Interleave(int[] dibits, ref byte[] data)
        {
            for (uint i = 0U; i < 98U; i++) {
                uint n = INTERLEAVE_TABLE[i];

                bool b1, b2;
                switch (dibits[n])
                {
                    case +3:
                        b1 = false;
                        b2 = true;
                        break;
                    case +1:
                        b1 = false;
                        b2 = false;
                        break;
                    case -1:
                        b1 = true;
                        b2 = false;
                        break;
                    default:
                        b1 = true;
                        b2 = true;
                        break;
                }

                n = i * 2U + 0U;
                FneUtils.WriteBit(ref data, n, b1);

                n = i * 2U + 1U;
                FneUtils.WriteBit(ref data, n, b2);
            }
        }

        /// <summary>
        /// Helper to map dibits to 4FSK constellation points.
        /// </summary>
        /// <param name="dibits">Dibits.</param>
        /// <param name="points">4FSK constellation points.</param>
        private void DibitsToPoints(int[] dibits, ref byte[] points)
        {
            for (uint i = 0U; i < 49U; i++) {
                if (dibits[i * 2U + 0U] == +1 && dibits[i * 2U + 1U] == -1)
                    points[i] = 0;
                else if (dibits[i * 2U + 0U] == -1 && dibits[i * 2U + 1U] == -1)
                    points[i] = 1;
                else if (dibits[i * 2U + 0U] == +3 && dibits[i * 2U + 1U] == -3)
                    points[i] = 2;
                else if (dibits[i * 2U + 0U] == -3 && dibits[i * 2U + 1U] == -3)
                    points[i] = 3;
                else if (dibits[i * 2U + 0U] == -3 && dibits[i * 2U + 1U] == -1)
                    points[i] = 4;
                else if (dibits[i * 2U + 0U] == +3 && dibits[i * 2U + 1U] == -1)
                    points[i] = 5;
                else if (dibits[i * 2U + 0U] == -1 && dibits[i * 2U + 1U] == -3)
                    points[i] = 6;
                else if (dibits[i * 2U + 0U] == +1 && dibits[i * 2U + 1U] == -3)
                    points[i] = 7;
                else if (dibits[i * 2U + 0U] == -3 && dibits[i * 2U + 1U] == +3)
                    points[i] = 8;
                else if (dibits[i * 2U + 0U] == +3 && dibits[i * 2U + 1U] == +3)
                    points[i] = 9;
                else if (dibits[i * 2U + 0U] == -1 && dibits[i * 2U + 1U] == +1)
                    points[i] = 10;
                else if (dibits[i * 2U + 0U] == +1 && dibits[i * 2U + 1U] == +1)
                    points[i] = 11;
                else if (dibits[i * 2U + 0U] == +1 && dibits[i * 2U + 1U] == +3)
                    points[i] = 12;
                else if (dibits[i * 2U + 0U] == -1 && dibits[i * 2U + 1U] == +3)
                    points[i] = 13;
                else if (dibits[i * 2U + 0U] == +3 && dibits[i * 2U + 1U] == +1)
                    points[i] = 14;
                else if (dibits[i * 2U + 0U] == -3 && dibits[i * 2U + 1U] == +1)
                    points[i] = 15;
            }
        }

        /// <summary>
        /// Helper to map 4FSK constellation points to dibits.
        /// </summary>
        /// <param name="points">4FSK Constellation points.</param>
        /// <param name="dibits">Dibits.</param>
        private void PointsToDibits(byte[] points, ref int[] dibits)
        {
            for (uint i = 0U; i < 49U; i++) {
                switch (points[i])
                {
                    case 0:
                        dibits[i * 2U + 0U] = +1;
                        dibits[i * 2U + 1U] = -1;
                        break;
                    case 1:
                        dibits[i * 2U + 0U] = -1;
                        dibits[i * 2U + 1U] = -1;
                        break;
                    case 2:
                        dibits[i * 2U + 0U] = +3;
                        dibits[i * 2U + 1U] = -3;
                        break;
                    case 3:
                        dibits[i * 2U + 0U] = -3;
                        dibits[i * 2U + 1U] = -3;
                        break;
                    case 4:
                        dibits[i * 2U + 0U] = -3;
                        dibits[i * 2U + 1U] = -1;
                        break;
                    case 5:
                        dibits[i * 2U + 0U] = +3;
                        dibits[i * 2U + 1U] = -1;
                        break;
                    case 6:
                        dibits[i * 2U + 0U] = -1;
                        dibits[i * 2U + 1U] = -3;
                        break;
                    case 7:
                        dibits[i * 2U + 0U] = +1;
                        dibits[i * 2U + 1U] = -3;
                        break;
                    case 8:
                        dibits[i * 2U + 0U] = -3;
                        dibits[i * 2U + 1U] = +3;
                        break;
                    case 9:
                        dibits[i * 2U + 0U] = +3;
                        dibits[i * 2U + 1U] = +3;
                        break;
                    case 10:
                        dibits[i * 2U + 0U] = -1;
                        dibits[i * 2U + 1U] = +1;
                        break;
                    case 11:
                        dibits[i * 2U + 0U] = +1;
                        dibits[i * 2U + 1U] = +1;
                        break;
                    case 12:
                        dibits[i * 2U + 0U] = +1;
                        dibits[i * 2U + 1U] = +3;
                        break;
                    case 13:
                        dibits[i * 2U + 0U] = -1;
                        dibits[i * 2U + 1U] = +3;
                        break;
                    case 14:
                        dibits[i * 2U + 0U] = +3;
                        dibits[i * 2U + 1U] = +1;
                        break;
                    default:
                        dibits[i * 2U + 0U] = -3;
                        dibits[i * 2U + 1U] = +1;
                        break;
                }
            }
        }

        /// <summary>
        /// Helper to convert a byte payload into tribits.
        /// </summary>
        /// <param name="payload">Byte payload.</param>
        /// <param name="tribits">Tribits.</param>
        private void BitsToTribits(byte[] payload, ref byte[] tribits)
        {
            for (uint i = 0U; i < 48U; i++) {
                uint n = i * 3U;

                bool b1 = FneUtils.ReadBit(payload, n) != false;
                n++;
                bool b2 = FneUtils.ReadBit(payload, n) != false;
                n++;
                bool b3 = FneUtils.ReadBit(payload, n) != false;

                byte tribit = 0;
                tribit |= (byte)(b1 ? 4U : 0U);
                tribit |= (byte)(b2 ? 2U : 0U);
                tribit |= (byte)(b3 ? 1U : 0U);

                tribits[i] = tribit;
            }

            tribits[48U] = 0;
        }

        /// <summary>
        /// Helper to convert a byte payload into dibits.
        /// </summary>
        /// <param name="payload">Byte payload.</param>
        /// <param name="dibits">Dibits.</param>
        private void BitsToDibits(byte[] payload, ref byte[] dibits)
        {
            for (uint i = 0U; i < 48U; i++) {
                uint n = i * 2U;

                bool b1 = FneUtils.ReadBit(payload, n) != false;
                n++;
                bool b2 = FneUtils.ReadBit(payload, n) != false;

                byte dibit = 0;
                dibit |= (byte)(b1 ? 2U : 0U);
                dibit |= (byte)(b2 ? 1U : 0U);

                dibits[i] = dibit;
            }

            dibits[48U] = 0;
        }

        /// <summary>
        /// Helper to convert tribits into a byte payload.
        /// </summary>
        /// <param name="tribits">Tribits.</param>
        /// <param name="payload">Byte payload.</param>
        private void TribitsToBits(byte[] tribits, ref byte[] payload)
        {
            for (uint i = 0U; i < 48U; i++) {
                byte tribit = tribits[i];

                bool b1 = (tribit & 0x04U) == 0x04U;
                bool b2 = (tribit & 0x02U) == 0x02U;
                bool b3 = (tribit & 0x01U) == 0x01U;

                uint n = i * 3U;

                FneUtils.WriteBit(ref payload, n, b1);
                n++;
                FneUtils.WriteBit(ref payload, n, b2);
                n++;
                FneUtils.WriteBit(ref payload, n, b3);
            }
        }

        /// <summary>
        /// Helper to convert tribits into a byte payload.
        /// </summary>
        /// <param name="dibits">Dibits.</param>
        /// <param name="payload">Byte payload.</param>
        private void DibitsToBits(byte[] dibits, ref byte[] payload)
        {
            for (uint i = 0U; i < 48U; i++) {
                byte dibit = dibits[i];

                bool b1 = (dibit & 0x02U) == 0x02U;
                bool b2 = (dibit & 0x01U) == 0x01U;

                uint n = i * 2U;

                FneUtils.WriteBit(ref payload, n, b1);
                n++;
                FneUtils.WriteBit(ref payload, n, b2);
            }
        }

        /// <summary>
        /// Helper to fix errors in Trellis coding.
        /// </summary>
        /// <param name="points">4FSK constellation points.</param>
        /// <param name="failPos"></param>
        /// <param name="payload">Byte payload.</param>
        /// <returns>True, if error corrected, otherwise false.</returns>
        private bool FixCode34(byte[] points, uint failPos, ref byte[] payload)
        {
            for (uint j = 0; j < 20; j++) 
            {
                uint bestPos = 0;
                byte bestVal = 0;

                for (byte i = 0; i < 16; i++)
                {
                    points[failPos] = i;

                    byte[] tribits = new byte[49];
                    uint pos = CheckCode34(points, ref tribits);
                    if (pos == 999)
                    {
                        TribitsToBits(tribits, ref payload);
                        return true;
                    }

                    if (pos > bestPos)
                    {
                        bestPos = pos;
                        bestVal = i;
                    }
                }

                points[failPos] = bestVal;
                failPos = bestPos;
            }

            return false;
        }

        /// <summary>
        /// Helper to detect errors in Trellis coding.
        /// </summary>
        /// <param name="points">4FSK constellation points.</param>
        /// <param name="tribits">Tribits.</param>
        /// <returns></returns>
        private uint CheckCode34(byte[] points, ref byte[] tribits)
        {
            byte state = 0;

            for (uint i = 0; i < 49; i++)
            {
                tribits[i] = 9;

                for (byte j = 0; j < 8; j++)
                {
                    if (points[i] == ENCODE_TABLE_34[state * 8 + j])
                    {
                        tribits[i] = j;
                        break;
                    }
                }

                if (tribits[i] == 9)
                    return i;

                state = tribits[i];
            }

            if (tribits[48] != 0)
                return 48;

            return 999;
        }


        /// <summary>
        /// Helper to fix errors in Trellis coding.
        /// </summary>
        /// <param name="points">4FSK constellation points.</param>
        /// <param name="failPos"></param>
        /// <param name="payload">Byte payload.</param>
        /// <returns>True, if error corrected, otherwise false.</returns>
        private bool FixCode12(byte[] points, uint failPos, ref byte[] payload)
        {
            for (uint j = 0; j < 20; j++) {
                uint bestPos = 0;
                byte bestVal = 0;

                for (byte i = 0; i < 16; i++)
                {
                    points[failPos] = i;

                    byte[] dibits = new byte[49];
                    uint pos = CheckCode12(points, ref dibits);
                    if (pos == 999)
                    {
                        DibitsToBits(dibits, ref payload);
                        return true;
                    }

                    if (pos > bestPos)
                    {
                        bestPos = pos;
                        bestVal = i;
                    }
                }

                points[failPos] = bestVal;
                failPos = bestPos;
            }

            return false;
        }

        /// <summary>
        /// Helper to detect errors in Trellis coding.
        /// </summary>
        /// <param name="points">4FSK constellation points.</param>
        /// <param name="dibits">Dibits.</param>
        /// <returns></returns>
        private uint CheckCode12(byte[] points, ref byte[] dibits)
        {
            byte state = 0;

            for (uint i = 0; i < 49; i++)
            {
                dibits[i] = 5;

                for (byte j = 0; j < 4; j++)
                {
                    if (points[i] == ENCODE_TABLE_12[state * 4 + j])
                    {
                        dibits[i] = j;
                        break;
                    }
                }

                if (dibits[i] == 5)
                    return i;

                state = dibits[i];
            }

            if (dibits[48] != 0)
                return 48;

            return 999;
        }
    } // public sealed class Trellis
} // namespace fnecore.EDAC
