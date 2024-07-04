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

using fnecore.EDAC;

namespace fnecore.DMR
{
    /// <summary>
    /// Represents full DMR link control.
    /// </summary>
    public sealed class FullLC
    {
        private static BPTC19696 bptc = new BPTC19696();

        private static readonly byte[] VOICE_LC_HEADER_CRC_MASK = new byte[3] { 0x96, 0x96, 0x96 };
        private static readonly byte[] TERMINATOR_WITH_LC_CRC_MASK = new byte[3] { 0x99, 0x99, 0x99 };
        private static readonly byte[] PI_HEADER_CRC_MASK = new byte[2] { 0x69, 0x69 };

        /*
        ** Methods
        */

        /// <summary>
        /// Decode DMR full-link control data.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static LC Decode(byte[] data, DMRDataType type)
        {
            if (data == null)
                throw new NullReferenceException("data");

            // decode BPTC (196,96) FEC
            byte[] lcData = new byte[12];
            bptc.Decode(data, out lcData);

            switch (type) {
                case DMRDataType.VOICE_LC_HEADER:
                    lcData[9U] ^= VOICE_LC_HEADER_CRC_MASK[0U];
                    lcData[10U] ^= VOICE_LC_HEADER_CRC_MASK[1U];
                    lcData[11U] ^= VOICE_LC_HEADER_CRC_MASK[2U];
                    break;

                case DMRDataType.TERMINATOR_WITH_LC:
                    lcData[9U] ^= TERMINATOR_WITH_LC_CRC_MASK[0U];
                    lcData[10U] ^= TERMINATOR_WITH_LC_CRC_MASK[1U];
                    lcData[11U] ^= TERMINATOR_WITH_LC_CRC_MASK[2U];
                    break;

                default:
                    // unsupported LC type
                    return null;
            }

            // check RS (12,9) FEC
            if (!RS129.Check(lcData))
                return null;

            return new LC(lcData);
        }

        /// <summary>
        /// Encode DMR full-link control data.
        /// </summary>
        /// <param name="lc"></param>
        /// <param name="data"></param>
        /// <param name="type"></param>
        public static void Encode(LC lc, ref byte[] data, DMRDataType type)
        {
            if (lc == null)
                throw new NullReferenceException("lc");
            if (data == null)
                throw new NullReferenceException("data");

            byte[] lcData = new byte[12];
            lc.GetData(ref lcData);

            // encode RS (12,9) FEC
            byte[] parity = new byte[4];
            RS129.Encode(lcData, 9, ref parity);

            switch (type) {
                case DMRDataType.VOICE_LC_HEADER:
                    lcData[9U] = (byte)(parity[2U] ^ VOICE_LC_HEADER_CRC_MASK[0U]);
                    lcData[10U] = (byte)(parity[1U] ^ VOICE_LC_HEADER_CRC_MASK[1U]);
                    lcData[11U] = (byte)(parity[0U] ^ VOICE_LC_HEADER_CRC_MASK[2U]);
                    break;

                case DMRDataType.TERMINATOR_WITH_LC:
                    lcData[9U] = (byte)(parity[2U] ^ TERMINATOR_WITH_LC_CRC_MASK[0U]);
                    lcData[10U] = (byte)(parity[1U] ^ TERMINATOR_WITH_LC_CRC_MASK[1U]);
                    lcData[11U] = (byte)(parity[0U] ^ TERMINATOR_WITH_LC_CRC_MASK[2U]);
                    break;

                default:
                    // unsupported LC type
                    return;
            }

            // encode BPTC (196,96) FEC
            bptc.Encode(lcData, out data);
        }

        /// <summary>
        /// Decode DMR privacy control data.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static PrivacyLC DecodePI(byte[] data)
        {
            if (data == null)
                throw new NullReferenceException("data");

            // decode BPTC (196,96) FEC
            byte[] lcData = new byte[12];
            bptc.Decode(data, out lcData);

            // make sure the CRC-CCITT 16 was actually included (the network tends to zero the CRC)
            if (lcData[10U] != 0x00U && lcData[11U] != 0x00U) {
                // validate the CRC-CCITT 16
                lcData[10U] ^= PI_HEADER_CRC_MASK[0U];
                lcData[11U] ^= PI_HEADER_CRC_MASK[1U];

                if (!CRC.CheckCCITT162(lcData, 12))
                    return null;

                // restore the checksum
                lcData[10U] ^= PI_HEADER_CRC_MASK[0U];
                lcData[11U] ^= PI_HEADER_CRC_MASK[1U];
            }

            return new PrivacyLC(lcData);
        }

        /// <summary>
        /// Encode DMR privacy control data.
        /// </summary>
        /// <param name="lc"></param>
        /// <param name="data"></param>
        /// <param name="type"></param>
        public static void EncodePI(PrivacyLC lc, ref byte[] data)
        {
            if (lc == null)
                throw new NullReferenceException("lc");
            if (data == null)
                throw new NullReferenceException("data");

            byte[] lcData = new byte[12];
            lc.GetData(ref lcData);

            // compute CRC-CCITT 16
            lcData[10U] ^= PI_HEADER_CRC_MASK[0U];
            lcData[11U] ^= PI_HEADER_CRC_MASK[1U];

            CRC.AddCCITT162(ref lcData, 12);

            // restore the checksum
            lcData[10U] ^= PI_HEADER_CRC_MASK[0U];
            lcData[11U] ^= PI_HEADER_CRC_MASK[1U];

            // encode BPTC (196,96) FEC
            bptc.Encode(lcData, out data);
        }
    } // public class LC
} // namespace fnecore.DMR
