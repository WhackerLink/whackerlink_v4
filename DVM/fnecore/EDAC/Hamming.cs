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
    /// Implements Hamming (15,11,3), (13,9,3), (10,6,3), (16,11,4) and
    //  (17,12,3) forward error correction.
    /// </summary>
    public sealed class Hamming
    {
        /*
        ** Methods
        */

        /// <summary>
        /// Decode Hamming (15,11,3).
        /// </summary>
        /// <param name="d">Boolean bit array.</param>
        /// <param name="offset"></param>
        /// <returns>True, if bit errors are detected, otherwise false.</returns>
        public static bool decode15113_1(bool[] d, int offset = 0)
        {
            if (d == null)
                throw new NullReferenceException("d");

            // Calculate the parity it should have
            bool c0 = d[0 + offset] ^ d[1 + offset] ^ d[2 + offset] ^ d[3 + offset] ^ d[4 + offset] ^ d[5 + offset] ^ d[6 + offset];
            bool c1 = d[0 + offset] ^ d[1 + offset] ^ d[2 + offset] ^ d[3 + offset] ^ d[7 + offset] ^ d[8 + offset] ^ d[9 + offset];
            bool c2 = d[0 + offset] ^ d[1 + offset] ^ d[4 + offset] ^ d[5 + offset] ^ d[7 + offset] ^ d[8 + offset] ^ d[10 + offset];
            bool c3 = d[0 + offset] ^ d[2 + offset] ^ d[4 + offset] ^ d[6 + offset] ^ d[7 + offset] ^ d[9 + offset] ^ d[10 + offset];

            byte n = 0;
            n |= (byte)((c0 != d[11 + offset]) ? 0x01U : 0x00U);
            n |= (byte)((c1 != d[12 + offset]) ? 0x02U : 0x00U);
            n |= (byte)((c2 != d[13 + offset]) ? 0x04U : 0x00U);
            n |= (byte)((c3 != d[14 + offset]) ? 0x08U : 0x00U);

            switch (n)
            {
                // Parity bit errors
                case 0x01: d[11 + offset] = !d[11 + offset]; return true;
                case 0x02: d[12 + offset] = !d[12 + offset]; return true;
                case 0x04: d[13 + offset] = !d[13 + offset]; return true;
                case 0x08: d[14 + offset] = !d[14 + offset]; return true;

                // Data bit errors
                case 0x0F: d[0 + offset] = !d[0 + offset]; return true;
                case 0x07: d[1 + offset] = !d[1 + offset]; return true;
                case 0x0B: d[2 + offset] = !d[2 + offset]; return true;
                case 0x03: d[3 + offset] = !d[3 + offset]; return true;
                case 0x0D: d[4 + offset] = !d[4 + offset]; return true;
                case 0x05: d[5 + offset] = !d[5 + offset]; return true;
                case 0x09: d[6 + offset] = !d[6 + offset]; return true;
                case 0x0E: d[7 + offset] = !d[7 + offset]; return true;
                case 0x06: d[8 + offset] = !d[8 + offset]; return true;
                case 0x0A: d[9 + offset] = !d[9 + offset]; return true;
                case 0x0C: d[10 + offset] = !d[10 + offset]; return true;

                // No bit errors
                default: return false;
            }
        }

        /// <summary>
        /// Encode Hamming (15,11,3).
        /// </summary>
        /// <param name="d">Boolean bit array.</param>
        /// <param name="offset"></param>
        public static void encode15113_1(ref bool[] d, int offset = 0)
        {
            if (d == null)
                throw new NullReferenceException("d");

            // Calculate the checksum this row should have
            d[11 + offset] = d[0 + offset] ^ d[1 + offset] ^ d[2 + offset] ^ d[3 + offset] ^ d[4 + offset] ^ d[5 + offset] ^ d[6 + offset];
            d[12 + offset] = d[0 + offset] ^ d[1 + offset] ^ d[2 + offset] ^ d[3 + offset] ^ d[7 + offset] ^ d[8 + offset] ^ d[9 + offset];
            d[13 + offset] = d[0 + offset] ^ d[1 + offset] ^ d[4 + offset] ^ d[5 + offset] ^ d[7 + offset] ^ d[8 + offset] ^ d[10 + offset];
            d[14 + offset] = d[0 + offset] ^ d[2 + offset] ^ d[4 + offset] ^ d[6 + offset] ^ d[7 + offset] ^ d[9 + offset] ^ d[10 + offset];
        }

        /// <summary>
        /// Decode Hamming (15,11,3).
        /// </summary>
        /// <param name="d">Boolean bit array.</param>
        /// <param name="offset"></param>
        /// <returns>True, if bit errors are detected, otherwise false.</returns>
        public static bool decode15113_2(bool[] d, int offset = 0)
        {
            if (d == null)
                throw new NullReferenceException("d");

            // Calculate the checksum this row should have
            bool c0 = d[0 + offset] ^ d[1 + offset] ^ d[2 + offset] ^ d[3 + offset] ^ d[5 + offset] ^ d[7 + offset] ^ d[8 + offset];
            bool c1 = d[1 + offset] ^ d[2 + offset] ^ d[3 + offset] ^ d[4 + offset] ^ d[6 + offset] ^ d[8 + offset] ^ d[9 + offset];
            bool c2 = d[2 + offset] ^ d[3 + offset] ^ d[4 + offset] ^ d[5 + offset] ^ d[7 + offset] ^ d[9 + offset] ^ d[10 + offset];
            bool c3 = d[0 + offset] ^ d[1 + offset] ^ d[2 + offset] ^ d[4 + offset] ^ d[6 + offset] ^ d[7 + offset] ^ d[10 + offset];

            byte n = 0x00;
            n |= (byte)((c0 != d[11 + offset]) ? 0x01U : 0x00U);
            n |= (byte)((c1 != d[12 + offset]) ? 0x02U : 0x00U);
            n |= (byte)((c2 != d[13 + offset]) ? 0x04U : 0x00U);
            n |= (byte)((c3 != d[14 + offset]) ? 0x08U : 0x00U);

            switch (n)
            {
                // Parity bit errors
                case 0x01: d[11 + offset] = !d[11 + offset]; return true;
                case 0x02: d[12 + offset] = !d[12 + offset]; return true;
                case 0x04: d[13 + offset] = !d[13 + offset]; return true;
                case 0x08: d[14 + offset] = !d[14 + offset]; return true;

                // Data bit errors
                case 0x09: d[0 + offset] = !d[0 + offset]; return true;
                case 0x0B: d[1 + offset] = !d[1 + offset]; return true;
                case 0x0F: d[2 + offset] = !d[2 + offset]; return true;
                case 0x07: d[3 + offset] = !d[3 + offset]; return true;
                case 0x0E: d[4 + offset] = !d[4 + offset]; return true;
                case 0x05: d[5 + offset] = !d[5 + offset]; return true;
                case 0x0A: d[6 + offset] = !d[6 + offset]; return true;
                case 0x0D: d[7 + offset] = !d[7 + offset]; return true;
                case 0x03: d[8 + offset] = !d[8 + offset]; return true;
                case 0x06: d[9 + offset] = !d[9 + offset]; return true;
                case 0x0C: d[10 + offset] = !d[10 + offset]; return true;

                // No bit errors
                default: return false;
            }
        }

        /// <summary>
        /// Encode Hamming (15,11,3).
        /// </summary>
        /// <param name="d">Boolean bit array.</param>
        /// <param name="offset"></param>
        public static void encode15113_2(ref bool[] d, int offset = 0)
        {
            if (d == null)
                throw new NullReferenceException("d");

            // Calculate the checksum this row should have
            d[11 + offset] = d[0 + offset] ^ d[1 + offset] ^ d[2 + offset] ^ d[3 + offset] ^ d[5 + offset] ^ d[7 + offset] ^ d[8 + offset];
            d[12 + offset] = d[1 + offset] ^ d[2 + offset] ^ d[3 + offset] ^ d[4 + offset] ^ d[6 + offset] ^ d[8 + offset] ^ d[9 + offset];
            d[13 + offset] = d[2 + offset] ^ d[3 + offset] ^ d[4 + offset] ^ d[5 + offset] ^ d[7 + offset] ^ d[9 + offset] ^ d[10 + offset];
            d[14 + offset] = d[0 + offset] ^ d[1 + offset] ^ d[2 + offset] ^ d[4 + offset] ^ d[6 + offset] ^ d[7 + offset] ^ d[10 + offset];
        }

        /// <summary>
        /// Decode Hamming (13,9,3).
        /// </summary>
        /// <param name="d">Boolean bit array.</param>
        /// <returns>True, if bit errors are detected, otherwise false.</returns>
        public static bool decode1393(bool[] d, int offset = 0)
        {
            if (d == null)
                throw new NullReferenceException("d");

            // Calculate the checksum this column should have
            bool c0 = d[0 + offset] ^ d[1 + offset] ^ d[3 + offset] ^ d[5 + offset] ^ d[6 + offset];
            bool c1 = d[0 + offset] ^ d[1 + offset] ^ d[2 + offset] ^ d[4 + offset] ^ d[6 + offset] ^ d[7 + offset];
            bool c2 = d[0 + offset] ^ d[1 + offset] ^ d[2 + offset] ^ d[3 + offset] ^ d[5 + offset] ^ d[7 + offset] ^ d[8 + offset];
            bool c3 = d[0 + offset] ^ d[2 + offset] ^ d[4 + offset] ^ d[5 + offset] ^ d[8 + offset];

            byte n = 0x00;
            n |= (byte)((c0 != d[9 + offset]) ? 0x01U : 0x00U);
            n |= (byte)((c1 != d[10 + offset]) ? 0x02U : 0x00U);
            n |= (byte)((c2 != d[11 + offset]) ? 0x04U : 0x00U);
            n |= (byte)((c3 != d[12 + offset]) ? 0x08U : 0x00U);

            switch (n)
            {
                // Parity bit errors
                case 0x01: d[9 + offset] = !d[9 + offset]; return true;
                case 0x02: d[10 + offset] = !d[10 + offset]; return true;
                case 0x04: d[11 + offset] = !d[11 + offset]; return true;
                case 0x08: d[12 + offset] = !d[12 + offset]; return true;

                // Data bit erros
                case 0x0F: d[0 + offset] = !d[0 + offset]; return true;
                case 0x07: d[1 + offset] = !d[1 + offset]; return true;
                case 0x0E: d[2 + offset] = !d[2 + offset]; return true;
                case 0x05: d[3 + offset] = !d[3 + offset]; return true;
                case 0x0A: d[4 + offset] = !d[4 + offset]; return true;
                case 0x0D: d[5 + offset] = !d[5 + offset]; return true;
                case 0x03: d[6 + offset] = !d[6 + offset]; return true;
                case 0x06: d[7 + offset] = !d[7 + offset]; return true;
                case 0x0C: d[8 + offset] = !d[8 + offset]; return true;

                // No bit errors
                default: return false;
            }
        }

        /// <summary>
        /// Encode Hamming (13,9,3).
        /// </summary>
        /// <param name="d">Boolean bit array.</param>
        /// <param name="offset"></param>
        public static void encode1393(ref bool[] d, int offset = 0)
        {
            if (d == null)
                throw new NullReferenceException("d");

            // Calculate the checksum this column should have
            d[9 + offset] = d[0 + offset] ^ d[1 + offset] ^ d[3 + offset] ^ d[5 + offset] ^ d[6 + offset];
            d[10 + offset] = d[0 + offset] ^ d[1 + offset] ^ d[2 + offset] ^ d[4 + offset] ^ d[6 + offset] ^ d[7 + offset];
            d[11 + offset] = d[0 + offset] ^ d[1 + offset] ^ d[2 + offset] ^ d[3 + offset] ^ d[5 + offset] ^ d[7 + offset] ^ d[8 + offset];
            d[12 + offset] = d[0 + offset] ^ d[2 + offset] ^ d[4 + offset] ^ d[5 + offset] ^ d[8 + offset];
        }

        /// <summary>
        /// Decode Hamming (10,6,3).
        /// </summary>
        /// <param name="d">Boolean bit array.</param>
        /// <param name="offset"></param>
        /// <returns>True, if bit errors are detected, otherwise false.</returns>
        public static bool decode1063(bool[] d, int offset = 0)
        {
            if (d == null)
                throw new NullReferenceException("d");

            // Calculate the checksum this column should have
            bool c0 = d[0 + offset] ^ d[1 + offset] ^ d[2 + offset] ^ d[5 + offset];
            bool c1 = d[0 + offset] ^ d[1 + offset] ^ d[3 + offset] ^ d[5 + offset];
            bool c2 = d[0 + offset] ^ d[2 + offset] ^ d[3 + offset] ^ d[4 + offset];
            bool c3 = d[1 + offset] ^ d[2 + offset] ^ d[3 + offset] ^ d[4 + offset];

            byte n = 0x00;
            n |= (byte)((c0 != d[6 + offset]) ? 0x01U : 0x00U);
            n |= (byte)((c1 != d[7 + offset]) ? 0x02U : 0x00U);
            n |= (byte)((c2 != d[8 + offset]) ? 0x04U : 0x00U);
            n |= (byte)((c3 != d[9 + offset]) ? 0x08U : 0x00U);

            switch (n)
            {
                // Parity bit errors
                case 0x01: d[6 + offset] = !d[6 + offset]; return true;
                case 0x02: d[7 + offset] = !d[7 + offset]; return true;
                case 0x04: d[8 + offset] = !d[8 + offset]; return true;
                case 0x08: d[9 + offset] = !d[9 + offset]; return true;

                // Data bit erros
                case 0x07: d[0 + offset] = !d[0 + offset]; return true;
                case 0x0B: d[1 + offset] = !d[1 + offset]; return true;
                case 0x0D: d[2 + offset] = !d[2 + offset]; return true;
                case 0x0E: d[3 + offset] = !d[3 + offset]; return true;
                case 0x0C: d[4 + offset] = !d[4 + offset]; return true;
                case 0x03: d[5 + offset] = !d[5 + offset]; return true;

                // No bit errors
                default: return false;
            }
        }

        /// <summary>
        /// Encode Hamming (10,6,3).
        /// </summary>
        /// <param name="d">Boolean bit array.</param>
        /// <param name="offset"></param>
        public static void encode1063(ref bool[] d, int offset = 0)
        {
            if (d == null)
                throw new NullReferenceException("d");

            // Calculate the checksum this column should have
            d[6 + offset] = d[0 + offset] ^ d[1 + offset] ^ d[2 + offset] ^ d[5 + offset];
            d[7 + offset] = d[0 + offset] ^ d[1 + offset] ^ d[3 + offset] ^ d[5 + offset];
            d[8 + offset] = d[0 + offset] ^ d[2 + offset] ^ d[3 + offset] ^ d[4 + offset];
            d[9 + offset] = d[1 + offset] ^ d[2 + offset] ^ d[3 + offset] ^ d[4 + offset];
        }

        /// <summary>
        /// Decode Hamming (16,11,4).
        /// </summary>
        /// <param name="d">Boolean bit array.</param>
        /// <param name="offset"></param>
        /// <returns>True, if bit errors are detected or no bit errors, otherwise false if unrecoverable errors are detected.</returns>
        public static bool decode16114(bool[] d, int offset = 0)
        {
            if (d == null)
                throw new NullReferenceException("d");

            // Calculate the checksum this column should have
            bool c0 = d[0 + offset] ^ d[1 + offset] ^ d[2 + offset] ^ d[3 + offset] ^ d[5 + offset] ^ d[7 + offset] ^ d[8 + offset];
            bool c1 = d[1 + offset] ^ d[2 + offset] ^ d[3 + offset] ^ d[4 + offset] ^ d[6 + offset] ^ d[8 + offset] ^ d[9 + offset];
            bool c2 = d[2 + offset] ^ d[3 + offset] ^ d[4 + offset] ^ d[5 + offset] ^ d[7 + offset] ^ d[9 + offset] ^ d[10 + offset];
            bool c3 = d[0 + offset] ^ d[1 + offset] ^ d[2 + offset] ^ d[4 + offset] ^ d[6 + offset] ^ d[7 + offset] ^ d[10 + offset];
            bool c4 = d[0 + offset] ^ d[2 + offset] ^ d[5 + offset] ^ d[6 + offset] ^ d[8 + offset] ^ d[9 + offset] ^ d[10 + offset];

            // Compare these with the actual bits
            byte n = 0x00;
            n |= (byte)((c0 != d[11 + offset]) ? 0x01U : 0x00U);
            n |= (byte)((c1 != d[12 + offset]) ? 0x02U : 0x00U);
            n |= (byte)((c2 != d[13 + offset]) ? 0x04U : 0x00U);
            n |= (byte)((c3 != d[14 + offset]) ? 0x08U : 0x00U);
            n |= (byte)((c4 != d[15 + offset]) ? 0x10U : 0x00U);

            switch (n)
            {
                // Parity bit errors
                case 0x01: d[11 + offset] = !d[11 + offset]; return true;
                case 0x02: d[12 + offset] = !d[12 + offset]; return true;
                case 0x04: d[13 + offset] = !d[13 + offset]; return true;
                case 0x08: d[14 + offset] = !d[14 + offset]; return true;
                case 0x10: d[15 + offset] = !d[15 + offset]; return true;

                // Data bit errors
                case 0x19: d[0 + offset] = !d[0 + offset]; return true;
                case 0x0B: d[1 + offset] = !d[1 + offset]; return true;
                case 0x1F: d[2 + offset] = !d[2 + offset]; return true;
                case 0x07: d[3 + offset] = !d[3 + offset]; return true;
                case 0x0E: d[4 + offset] = !d[4 + offset]; return true;
                case 0x15: d[5 + offset] = !d[5 + offset]; return true;
                case 0x1A: d[6 + offset] = !d[6 + offset]; return true;
                case 0x0D: d[7 + offset] = !d[7 + offset]; return true;
                case 0x13: d[8 + offset] = !d[8 + offset]; return true;
                case 0x16: d[9 + offset] = !d[9 + offset]; return true;
                case 0x1C: d[10 + offset] = !d[10 + offset]; return true;

                // No bit errors
                case 0x00: return true;

                // Unrecoverable errors
                default: return false;
            }
        }

        /// <summary>
        /// Encode Hamming (10,6,3).
        /// </summary>
        /// <param name="d">Boolean bit array.</param>
        /// <param name="offset"></param>
        public static void encode16114(ref bool[] d, int offset = 0)
        {
            if (d == null)
                throw new NullReferenceException("d");

            d[11 + offset] = d[0 + offset] ^ d[1 + offset] ^ d[2 + offset] ^ d[3 + offset] ^ d[5 + offset] ^ d[7 + offset] ^ d[8 + offset];
            d[12 + offset] = d[1 + offset] ^ d[2 + offset] ^ d[3 + offset] ^ d[4 + offset] ^ d[6 + offset] ^ d[8 + offset] ^ d[9 + offset];
            d[13 + offset] = d[2 + offset] ^ d[3 + offset] ^ d[4 + offset] ^ d[5 + offset] ^ d[7 + offset] ^ d[9 + offset] ^ d[10 + offset];
            d[14 + offset] = d[0 + offset] ^ d[1 + offset] ^ d[2 + offset] ^ d[4 + offset] ^ d[6 + offset] ^ d[7 + offset] ^ d[10 + offset];
            d[15 + offset] = d[0 + offset] ^ d[2 + offset] ^ d[5 + offset] ^ d[6 + offset] ^ d[8 + offset] ^ d[9 + offset] ^ d[10 + offset];
        }

        /// <summary>
        /// Decode Hamming (17,12,3).
        /// </summary>
        /// <param name="d">Boolean bit array.</param>
        /// <param name="offset"></param>
        /// <returns>True, if bit errors are detected or no bit errors, otherwise false if unrecoverable errors are detected.</returns>
        public static bool decode17123(bool[] d, int offset = 0)
        {
            if (d == null)
                throw new NullReferenceException("d");

            // Calculate the checksum this column should have
            bool c0 = d[0 + offset] ^ d[1 + offset] ^ d[2 + offset] ^ d[3 + offset] ^ d[6 + offset] ^ d[7 + offset] ^ d[9 + offset];
            bool c1 = d[0 + offset] ^ d[1 + offset] ^ d[2 + offset] ^ d[3 + offset] ^ d[4 + offset] ^ d[7 + offset] ^ d[8 + offset] ^ d[10 + offset];
            bool c2 = d[1 + offset] ^ d[2 + offset] ^ d[3 + offset] ^ d[4 + offset] ^ d[5 + offset] ^ d[8 + offset] ^ d[9 + offset] ^ d[11 + offset];
            bool c3 = d[0 + offset] ^ d[1 + offset] ^ d[4 + offset] ^ d[5 + offset] ^ d[7 + offset] ^ d[10 + offset];
            bool c4 = d[0 + offset] ^ d[1 + offset] ^ d[2 + offset] ^ d[5 + offset] ^ d[6 + offset] ^ d[8 + offset] ^ d[11 + offset];

            // Compare these with the actual bits
            byte n = 0x00;
            n |= (byte)((c0 != d[12 + offset]) ? 0x01U : 0x00U);
            n |= (byte)((c1 != d[13 + offset]) ? 0x02U : 0x00U);
            n |= (byte)((c2 != d[14 + offset]) ? 0x04U : 0x00U);
            n |= (byte)((c3 != d[15 + offset]) ? 0x08U : 0x00U);
            n |= (byte)((c4 != d[16 + offset]) ? 0x10U : 0x00U);

            switch (n)
            {
                // Parity bit errors
                case 0x01: d[12 + offset] = !d[12 + offset]; return true;
                case 0x02: d[13 + offset] = !d[13 + offset]; return true;
                case 0x04: d[14 + offset] = !d[14 + offset]; return true;
                case 0x08: d[15 + offset] = !d[15 + offset]; return true;
                case 0x10: d[16 + offset] = !d[16 + offset]; return true;

                // Data bit errors
                case 0x1B: d[0 + offset] = !d[0 + offset]; return true;
                case 0x1F: d[1 + offset] = !d[1 + offset]; return true;
                case 0x17: d[2 + offset] = !d[2 + offset]; return true;
                case 0x07: d[3 + offset] = !d[3 + offset]; return true;
                case 0x0E: d[4 + offset] = !d[4 + offset]; return true;
                case 0x1C: d[5 + offset] = !d[5 + offset]; return true;
                case 0x11: d[6 + offset] = !d[6 + offset]; return true;
                case 0x0B: d[7 + offset] = !d[7 + offset]; return true;
                case 0x16: d[8 + offset] = !d[8 + offset]; return true;
                case 0x05: d[9 + offset] = !d[9 + offset]; return true;
                case 0x0A: d[10 + offset] = !d[10 + offset]; return true;
                case 0x14: d[11 + offset] = !d[11 + offset]; return true;

                // No bit errors
                case 0x00: return true;

                // Unrecoverable errors
                default: return false;
            }
        }

        /// <summary>
        /// Encode Hamming (17,12,3).
        /// </summary>
        /// <param name="d">Boolean bit array.</param>
        /// <param name="offset"></param>
        public static void encode17123(ref bool[] d, int offset = 0)
        {
            if (d == null)
                throw new NullReferenceException("d");

            d[12 + offset] = d[0 + offset] ^ d[1 + offset] ^ d[2 + offset] ^ d[3 + offset] ^ d[6 + offset] ^ d[7 + offset] ^ d[9 + offset];
            d[13 + offset] = d[0 + offset] ^ d[1 + offset] ^ d[2 + offset] ^ d[3 + offset] ^ d[4 + offset] ^ d[7 + offset] ^ d[8 + offset] ^ d[10 + offset];
            d[14 + offset] = d[1 + offset] ^ d[2 + offset] ^ d[3 + offset] ^ d[4 + offset] ^ d[5 + offset] ^ d[8 + offset] ^ d[9 + offset] ^ d[11 + offset];
            d[15 + offset] = d[0 + offset] ^ d[1 + offset] ^ d[4 + offset] ^ d[5 + offset] ^ d[7 + offset] ^ d[10 + offset];
            d[16 + offset] = d[0 + offset] ^ d[1 + offset] ^ d[2 + offset] ^ d[5 + offset] ^ d[6 + offset] ^ d[8 + offset] ^ d[11 + offset];
        }
    } // public sealed class Hamming
} // namespace fnecore.EDAC
