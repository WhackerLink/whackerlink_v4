// SPDX-License-Identifier: AGPL-3.0-only
/**
* Digital Voice Modem - Fixed Network Equipment Core Library
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / Fixed Network Equipment Core Library
* @license AGPLv3 License (https://opensource.org/licenses/AGPL-3.0)
*
*   Copyright (C) 2024 Bryan Biedenkapp, N2PLL
*
*/

using System;

namespace fnecore.P25
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class P25Interleaver
    {
        private const uint P25_SS0_START = 70U;
        private const uint P25_SS1_START = 71U;
        private const uint P25_SS_INCREMENT = 72U;

        /*
        ** Methods
        */

        /// <summary>
        /// Decode bit interleaving.
        /// </summary>
        /// <param name="in"></param>
        /// <param name="out"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <returns></returns>
        public static uint Decode(byte[] _in, ref byte[] _out, uint start, uint stop)
        {
            if (_in == null)
                throw new NullReferenceException("_in");
            if (_out == null)
                throw new NullReferenceException("_out");

            // Move the SSx positions to the range needed
            uint ss0Pos = P25_SS0_START;
            uint ss1Pos = P25_SS1_START;

            while (ss0Pos < start) {
                ss0Pos += P25_SS_INCREMENT;
                ss1Pos += P25_SS_INCREMENT;
            }

            uint n = 0U;
            for (uint i = start; i < stop; i++) {
                if (i == ss0Pos) {
                    ss0Pos += P25_SS_INCREMENT;
                }
                else if (i == ss1Pos) {
                    ss1Pos += P25_SS_INCREMENT;
                }
                else {
                    bool b = FneUtils.ReadBit(_in, i);
                    FneUtils.WriteBit(ref _out, n, b);
                    n++;
                }
            }

            return n;
        }

        /// <summary>
        /// Encode bit interleaving.
        /// </summary>
        /// <param name="in"></param>
        /// <param name="out"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <returns></returns>
        public static uint Encode(byte[] _in, ref byte[] _out, uint start, uint stop)
        {
            if (_in == null)
                throw new NullReferenceException("_in");
            if (_out == null)
                throw new NullReferenceException("_out");

            // Move the SSx positions to the range needed
            uint ss0Pos = P25_SS0_START;
            uint ss1Pos = P25_SS1_START;

            while (ss0Pos < start) {
                ss0Pos += P25_SS_INCREMENT;
                ss1Pos += P25_SS_INCREMENT;
            }

            uint n = 0U;
            for (uint i = start; i < stop; i++) {
                if (i == ss0Pos) {
                    ss0Pos += P25_SS_INCREMENT;
                }
                else if (i == ss1Pos) {
                    ss1Pos += P25_SS_INCREMENT;
                }
                else {
                    bool b = FneUtils.ReadBit(_in, n);
                    FneUtils.WriteBit(ref _out, i, b);
                    n++;
                }
            }

            return n;
        }
    } // public sealed class P25Interleaver
} // namespace fnecore.P25
