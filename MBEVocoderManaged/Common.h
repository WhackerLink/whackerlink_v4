// SPDX-License-Identifier: GPL-2.0-only
/**
* Digital Voice Modem - MBE Vocoder
* GPLv2 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / MBE Vocoder
* @license GPLv2 License (https://opensource.org/licenses/GPL-2.0)
*
*   Copyright (C) 2023 Bryan Biedenkapp, N2PLL
*
*/
#if !defined(__COMMON_H__)
#define __COMMON_H__

namespace vocoder
{
    // ---------------------------------------------------------------------------
    //  Constants
    // ---------------------------------------------------------------------------

    public enum class MBEMode {
        DMRAMBE,
        IMBE,                   // e.g. IMBE used by P25
    };
} // namespace vocoder

#endif // __COMMON_H__
