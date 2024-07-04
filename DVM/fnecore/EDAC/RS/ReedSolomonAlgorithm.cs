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
    /// 
    /// </summary>
    public enum ErrorCorrectionCodeType
    {
        /// <summary>
        /// (63,47,17)
        /// </summary>
        ReedSolomon_362017,
        /// <summary>
        /// (24,12,13)
        /// </summary>
        ReedSolomon_241213,
        /// <summary>
        /// (24,16,9)
        /// </summary>
        ReedSolomon_24169
    } // public enum ErrorCorrectionCodeType

    /// <summary>
    /// 
    /// </summary>
    public static class ReedSolomonAlgorithm
    {
        /*
        ** Methods
        */

        /// <summary>
        /// Produces error correction codewords for a message using the Reed-Solomon algorithm.
        /// </summary>
        /// <param name="message">The message to compute the error correction codewords.</param>
        /// <param name="eccType">The type of Galois field to use to encode error correction codewords.</param>
        /// <returns>Returns the computed message with error correction codewords.</returns>
        public static byte[] Encode(byte[] message, ErrorCorrectionCodeType eccType)
        {
            ReedSolomonEncoder reedSolomonEncoder = new ReedSolomonEncoder();
            byte[] output = new byte[message.Length];
            Buffer.BlockCopy(message, 0, output, 0, message.Length);

            if (eccType == ErrorCorrectionCodeType.ReedSolomon_362017)
            {
                reedSolomonEncoder.encode362017(ref output);
                return output;
            }
            else if (eccType == ErrorCorrectionCodeType.ReedSolomon_241213)
            {
                reedSolomonEncoder.encode241213(ref output);
                return output;
            }
            else if (eccType == ErrorCorrectionCodeType.ReedSolomon_24169)
            {
                reedSolomonEncoder.encode24169(ref output);
                return output;
            }
            else
                throw new ArgumentException($"Invalid '{nameof(eccType)}' argument.", nameof(eccType));
        }

        /// <summary>
        /// Repairs a possibly broken message using the Reed-Solomon algorithm.
        /// </summary>
        /// <param name="message">The possibly broken message to repair.</param>
        /// <param name="eccType">The type of Galois field to use to decode message.</param>
        /// <returns>Returns the repaired message, or null if it cannot be repaired.</returns>
        public static byte[] Decode(byte[] message, ErrorCorrectionCodeType eccType)
        {
            byte[] codeword = new byte[63];

            if (eccType == ErrorCorrectionCodeType.ReedSolomon_362017)
            {
                uint offset = 0U;
                for (uint i = 0U; i < 36U; i++, offset += 6)
                    codeword[27 + i] = FneUtils.BIN2HEX(message, offset);

                ReedSolomonDecoder rs362017 = new ReedSolomonDecoder(64, 47, 16, 0x43);
                byte[] codewordEC = rs362017.DecodeEx(codeword);

                byte[] messageOut = new byte[message.Length];
                offset = 0U;
                for (uint i = 0U; i < 20U; i++, offset += 6)
                    FneUtils.HEX2BIN(codewordEC[27 + i], ref messageOut, offset);
                return messageOut;
            }
            else if (eccType == ErrorCorrectionCodeType.ReedSolomon_241213)
            {
                uint offset = 0U;
                for (uint i = 0U; i < 24U; i++, offset += 6)
                    codeword[39 + i] = FneUtils.BIN2HEX(message, offset);

                ReedSolomonDecoder rs241213 = new ReedSolomonDecoder(64, 51, 12, 0x43);
                byte[] codewordEC = rs241213.DecodeEx(codeword);

                byte[] messageOut = new byte[message.Length];
                offset = 0U;
                for (uint i = 0U; i < 12U; i++, offset += 6)
                    FneUtils.HEX2BIN(codewordEC[39 + i], ref messageOut, offset);
                return messageOut;
            }
            else if (eccType == ErrorCorrectionCodeType.ReedSolomon_24169)
            {
                uint offset = 0U;
                for (uint i = 0U; i < 24U; i++, offset += 6)
                    codeword[39 + i] = FneUtils.BIN2HEX(message, offset);

                ReedSolomonDecoder rs24169 = new ReedSolomonDecoder(64, 55, 8, 0x43);
                byte[] codewordEC = rs24169.DecodeEx(codeword);

                byte[] messageOut = new byte[message.Length];
                offset = 0U;
                for (uint i = 0U; i < 16U; i++, offset += 6)
                    FneUtils.HEX2BIN(codewordEC[39 + i], ref messageOut, offset);
                return messageOut;
            }
            else
                throw new ArgumentException($"Invalid '{nameof(eccType)}' argument.", nameof(eccType));
        }
    } // public static class ReedSolomonAlgorithm
} // namespace fnecore.EDAC.RS
