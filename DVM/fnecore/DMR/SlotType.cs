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
    /// Represents DMR slot type.
    /// </summary>
    public class SlotType
    {
        /// <summary>
        /// DMR access color code.
        /// </summary>
        public byte ColorCode;

        /// <summary>
        /// Slot data type.
        /// </summary>
        public byte DataType;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="SlotType"/> class.
        /// </summary>
        public SlotType()
        {
            ColorCode = 0;
            DataType = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SlotType"/> class.
        /// </summary>
        /// <param name="bytes"></param>
        public SlotType(byte[] bytes)
        {
            byte[] DMRSlotType = new byte[3U];
            DMRSlotType[0U] = (byte)((bytes[12U] << 2) & 0xFCU);
            DMRSlotType[0U] |= (byte)((bytes[13U] >> 6) & 0x03U);

            DMRSlotType[1U] = (byte)((bytes[13U] << 2) & 0xC0U);
            DMRSlotType[1U] |= (byte)((bytes[19U] << 2) & 0x3CU);
            DMRSlotType[1U] |= (byte)((bytes[20U] >> 6) & 0x03U);

            DMRSlotType[2U] = (byte)((bytes[20U] << 2) & 0xF0U);

            Golay2087 golay2087 = new Golay2087();
            byte code = golay2087.Decode(DMRSlotType);

            ColorCode = (byte)((code >> 4) & 0x0FU);
            DataType = (byte)((code >> 0) & 0x0FU);
        }

        /// <summary>
        /// Gets <see cref="SlotType"/> data as bytes.
        /// </summary>
        /// <param name="bytes"></param>
        public void GetData(ref byte[] bytes)
        {
            if (bytes == null)
                throw new NullReferenceException("bytes");

            byte[] DMRSlotType = new byte[3U];
            DMRSlotType[0U] = (byte)((ColorCode << 4) & 0xF0U);
            DMRSlotType[0U] |= (byte)((DataType << 0) & 0x0FU);
            DMRSlotType[1U] = (byte)0x00U;
            DMRSlotType[2U] = (byte)0x00U;

            Golay2087 golay2087 = new Golay2087();
            golay2087.Encode(ref DMRSlotType);

            bytes[12U] = (byte)((bytes[12U] & 0xC0U) | ((DMRSlotType[0U] >> 2) & 0x3FU));
            bytes[13U] = (byte)((bytes[13U] & 0x0FU) | ((DMRSlotType[0U] << 6) & 0xC0U) | ((DMRSlotType[1U] >> 2) & 0x30U));
            bytes[19U] = (byte)((bytes[19U] & 0xF0U) | ((DMRSlotType[1U] >> 2) & 0x0FU));
            bytes[20U] = (byte)((bytes[20U] & 0x03U) | ((DMRSlotType[1U] << 6) & 0xC0U) | ((DMRSlotType[2U] >> 2) & 0x3CU));
        }
    } // public class SlotType
} // namespace fnecore.DMR
