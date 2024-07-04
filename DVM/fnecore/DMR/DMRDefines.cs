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
    /// DMR Data Types
    /// </summary>
    public enum DMRDataType : byte
    {
        /// <summary>
        /// Voice Privacy Indicator Header
        /// </summary>
        VOICE_PI_HEADER = 0x00,
        /// <summary>
        /// Voice Link Control Header
        /// </summary>
        VOICE_LC_HEADER = 0x01,
        /// <summary>
        /// Terminator with Link Control
        /// </summary>
        TERMINATOR_WITH_LC = 0x02,
        /// <summary>
        /// Control Signalling Block
        /// </summary>
        CSBK = 0x03,
        /// <summary>
        /// Data Header
        /// </summary>
        DATA_HEADER = 0x06,
        /// <summary>
        /// 1/2 Rate Data
        /// </summary>
        RATE_12_DATA = 0x07,
        /// <summary>
        /// 3/4 Rate Data
        /// </summary>
        RATE_34_DATA = 0x08,
        /// <summary>
        /// Idle Burst
        /// </summary>
        IDLE = 0x09,
        /// <summary>
        /// 1 Rate Data
        /// </summary>
        RATE_1_DATA = 0x0A,
    } // public enum DMRDataType : byte

    /// <summary>
    /// DMR Full-Link Opcodes
    /// </summary>
    public enum DMRFLCO : byte
    {
        /// <summary>
        /// GRP VCH USER - Group Voice Channel User
        /// </summary>
        FLCO_GROUP = 0x00,
        /// <summary>
        /// UU VCH USER - Unit-to-Unit Voice Channel User
        /// </summary>
        FLCO_PRIVATE = 0x01,
    } // public enum DMRFLCO : byte
} // namespace fnecore.DMR
