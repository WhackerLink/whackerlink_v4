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

namespace fnecore.EDAC.RS
{
    /// <summary>
    /// Implements Reed-Solomon encoding, as the name implies.
    /// </summary>
    internal sealed class ReedSolomonEncoder
    {
        private static readonly byte[][] ENCODE_MATRIX = new byte[12][] {
            new byte[24] {1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 50, 36, 3, 21, 12, 14, 23, 3, 43, 4, 30, 39 },
            new byte[24] {0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 9, 10, 9, 9, 14, 52, 55, 45, 1, 62, 22, 59 },
            new byte[24] {0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 1, 5, 61, 12, 6, 16, 36, 54, 6, 56, 54 },
            new byte[24] {0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 17, 56, 23, 37, 14, 55, 19, 52, 59, 27, 36, 17 },
            new byte[24] {0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 24, 18, 3, 61, 13, 13, 27, 13, 41, 3, 43, 40 },
            new byte[24] {0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 33, 23, 46, 62, 52, 17, 43, 4, 21, 1, 10 },
            new byte[24] {0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 49, 62, 17, 45, 62, 1, 51, 29, 24, 11, 52, 56 },
            new byte[24] {0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 20, 18, 57, 46, 17, 29, 59, 34, 47, 60, 35, 62 },
            new byte[24] {0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 58, 34, 5, 16, 35, 39, 27, 46, 1, 14, 11, 62 },
            new byte[24] {0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 58, 12, 53, 44, 29, 21, 33, 14, 13, 32, 57, 22 },
            new byte[24] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 59, 53, 30, 49, 34, 18, 15, 4, 36, 16, 21, 5 },
            new byte[24] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 57, 5, 45, 3, 57, 28, 48, 9, 60, 2, 33, 40 }
        };

        private static readonly byte[][] ENCODE_MATRIX_24169 = new byte[16][] {
            new byte[24] {1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 41, 37, 55, 13, 52, 55, 42, 10 },
            new byte[24] {0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 47, 21, 51, 59, 57, 18, 32, 13 },
            new byte[24] {0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 5, 1, 25, 4, 14, 44, 21, 62 },
            new byte[24] {0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 59, 7, 39, 12, 33, 63, 39, 9 },
            new byte[24] {0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 61, 13, 41, 41, 15, 55, 15, 47 },
            new byte[24] {0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 26, 12, 34, 61, 34, 56, 44 },
            new byte[24] {0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 61, 35, 5, 1, 32, 10, 52 },
            new byte[24] {0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 20, 60, 13, 58, 20, 22, 60, 49 },
            new byte[24] {0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 34, 52, 7, 18, 49, 16, 32, 53 },
            new byte[24] {0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 26, 26, 45, 33, 47, 54, 17, 63 },
            new byte[24] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 53, 30, 21, 7, 40, 14, 32, 41 },
            new byte[24] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 52, 6, 44, 26, 62, 38, 12, 30 },
            new byte[24] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 50, 51, 60, 56, 5, 23, 31, 38 },
            new byte[24] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 45, 35, 28, 57, 47, 62, 40, 52 },
            new byte[24] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 20, 19, 19, 5, 40, 56, 34, 19 },
            new byte[24] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 55, 61, 37, 48, 47, 20, 6, 22 }
        };

        private static readonly byte[][] ENCODE_MATRIX_362017 = new byte[20][] {
            new byte[36] {1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 60, 31, 28, 6, 2, 7, 36, 52, 22, 12, 22, 36, 44, 11, 63, 5 },
            new byte[36] {0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4, 15, 40, 20, 9, 5, 24, 47, 27, 3, 2, 2, 13, 14, 21, 22 },
            new byte[36] {0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 7, 19, 31, 38, 46, 61, 35, 37, 45, 17, 40, 25, 37, 23, 57, 50 },
            new byte[36] {0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 22, 5, 7, 51, 51, 23, 51, 32, 6, 4, 32, 37, 39, 24, 61, 7 },
            new byte[36] {0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 19, 59, 59, 33, 58, 28, 17, 41, 55, 14, 25, 60, 9, 17, 10, 17 },
            new byte[36] {0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 20, 41, 21, 19, 18, 33, 60, 54, 60, 53, 56, 30, 55, 37, 52, 1 },
            new byte[36] {0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 42, 27, 12, 2, 16, 6, 12, 21, 42, 19, 29, 60, 61, 61, 35, 23 },
            new byte[36] {0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 45, 50, 46, 21, 59, 48, 13, 24, 11, 15, 16, 2, 56, 45, 12, 39 },
            new byte[36] {0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 44, 41, 26, 53, 63, 10, 44, 11, 29, 26, 46, 10, 61, 1, 58, 51 },
            new byte[36] {0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 60, 33, 24, 33, 35, 18, 41, 6, 52, 27, 3, 39, 23, 10, 45, 39 },
            new byte[36] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 44, 56, 9, 3, 11, 18, 14, 47, 3, 37, 58, 25, 24, 46, 29, 18 },
            new byte[36] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 41, 7, 58, 24, 53, 44, 6, 17, 30, 51, 40, 49, 52, 42, 1, 48 },
            new byte[36] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 53, 26, 56, 11, 36, 59, 20, 10, 42, 17, 45, 10, 29, 12, 58 },
            new byte[36] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 9, 56, 5, 8, 53, 20, 13, 63, 18, 20, 20, 60, 7, 36, 7, 38 },
            new byte[36] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 6, 2, 53, 9, 33, 16, 37, 34, 38, 44, 29, 10, 32, 52, 53, 27 },
            new byte[36] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 28, 25, 1, 13, 36, 52, 14, 20, 42, 14, 6, 50, 16, 11, 45, 47 },
            new byte[36] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 51, 35, 21, 36, 63, 51, 15, 15, 52, 12, 32, 60, 25, 58, 44, 6 },
            new byte[36] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 57, 17, 56, 36, 46, 4, 24, 60, 4, 19, 57, 56, 51, 37, 46, 35 },
            new byte[36] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 2, 1, 43, 60, 2, 12, 42, 60, 10, 47, 20, 51, 13, 34, 42, 27 },
            new byte[36] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 28, 29, 2, 19, 17, 23, 18, 27, 52, 34, 5, 59, 41, 38, 59, 48 }
        };

        /*
        ** Methods
        */

        /// <summary>
        /// Encode RS (24,12,13) FEC.
        /// </summary>
        /// <param name="data">Raw data to encode with Reed-Solomon FEC.</param>
        public void encode241213(ref byte[] data)
        {
            if (data == null)
                throw new NullReferenceException();

            byte[] codeword = new byte[24U];

            for (uint i = 0U; i < 24U; i++)
            {
                codeword[i] = 0x00;

                uint offs = 0U;
                for (uint j = 0U; j < 12U; j++, offs += 6U)
                {
                    byte hexbit = FneUtils.BIN2HEX(data, offs);
                    codeword[i] ^= gf6Mult(hexbit, ENCODE_MATRIX[j][i]);
                }
            }

            uint offset = 0U;
            for (uint i = 0U; i < 24U; i++, offset += 6U)
                FneUtils.HEX2BIN(codeword[i], ref data, offset);
        }

        /// <summary>
        /// Encode RS (24,16,9) FEC.
        /// </summary>
        /// <param name="data">Raw data to encode with Reed-Solomon FEC.</param>
        public void encode24169(ref byte[] data)
        {
            if (data == null)
                throw new NullReferenceException();

            byte[] codeword = new byte[24U];

            for (uint i = 0U; i < 24U; i++)
            {
                codeword[i] = 0x00;

                uint offs = 0U;
                for (uint j = 0U; j < 16U; j++, offs += 6U)
                {
                    byte hexbit = FneUtils.BIN2HEX(data, offs);
                    codeword[i] ^= gf6Mult(hexbit, ENCODE_MATRIX_24169[j][i]);
                }
            }

            uint offset = 0U;
            for (uint i = 0U; i < 24U; i++, offset += 6U)
                FneUtils.HEX2BIN(codeword[i], ref data, offset);
        }

        /// <summary>
        /// Encode RS (36,20,17) FEC.
        /// </summary>
        /// <param name="data">Raw data to encode with Reed-Solomon FEC.</param>
        public void encode362017(ref byte[] data)
        {
            if (data == null)
                throw new NullReferenceException();

            byte[] codeword = new byte[36U];

            for (uint i = 0U; i < 36U; i++)
            {
                codeword[i] = 0x00;

                uint offs = 0U;
                for (uint j = 0U; j < 20U; j++, offs += 6U)
                {
                    byte hexbit = FneUtils.BIN2HEX(data, offs);
                    codeword[i] ^= gf6Mult(hexbit, ENCODE_MATRIX_362017[j][i]);
                }
            }

            uint offset = 0U;
            for (uint i = 0U; i < 36U; i++, offset += 6U)
                FneUtils.HEX2BIN(codeword[i], ref data, offset);
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks>GF(2 ^ 6) multiply (for Reed-Solomon encoder).</remarks>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private byte gf6Mult(byte a, byte b)
        {
            byte p = 0x00;

            for (uint i = 0U; i < 6U; i++) {
                if ((b & 0x01) == 0x01)
                    p ^= a;

                a <<= 1;

                if ((a & 0x40) == 0x40)
                    a ^= 0x43;              // primitive polynomial : x ^ 6 + x + 1

                b >>= 1;
            }

            return p;
        }
    } // internal sealed class ReedSolomonEncoder
} // namespace fnecore.EDAC.RS
