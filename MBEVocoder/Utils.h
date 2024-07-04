// SPDX-License-Identifier: GPL-2.0-only
/**
* Digital Voice Modem - MBE Vocoder
* GPLv2 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / MBE Vocoder
* @derivedfrom MMDVMHost (https://github.com/g4klx/MMDVMHost)
* @license GPLv2 License (https://opensource.org/licenses/GPL-2.0)
*
*   Copyright (C) 2009,2014,2015 Jonathan Naylor, G4KLX
*   Copyright (C) 2018-2019 Bryan Biedenkapp, N2PLL
*
*/
#if !defined(__UTILS_H__)
#define __UTILS_H__

#include "Defines.h"

#include <string>

#if !defined(_UTILS_NO_INCLUDE_CPP)
// ---------------------------------------------------------------------------
//  Class Declaration
//      Implements various helper utilities.
// ---------------------------------------------------------------------------

class HOST_SW_API Utils {
public:
    /// <summary></summary>
    static void byteToBitsBE(uint8_t byte, bool* bits);
    /// <summary></summary>
    static void byteToBitsLE(uint8_t byte, bool* bits);

    /// <summary></summary>
    static void bitsToByteBE(const bool* bits, uint8_t& byte);
    /// <summary></summary>
    static void bitsToByteLE(const bool* bits, uint8_t& byte);

    /// <summary></summary>
    static uint32_t getBits(const uint8_t* in, uint8_t* out, uint32_t start, uint32_t stop);
    /// <summary></summary>
    static uint32_t getBitRange(const uint8_t* in, uint8_t* out, uint32_t start, uint32_t length);
    /// <summary></summary>
    static uint32_t setBits(const uint8_t* in, uint8_t* out, uint32_t start, uint32_t stop);
    /// <summary></summary>
    static uint32_t setBitRange(const uint8_t* in, uint8_t* out, uint32_t start, uint32_t length);

    /// <summary>Returns the count of bits in the passed 8 byte value.</summary>
    static uint8_t countBits8(uint8_t bits);
    /// <summary>Returns the count of bits in the passed 32 byte value.</summary>
    static uint8_t countBits32(uint32_t bits);
    /// <summary>Returns the count of bits in the passed 64 byte value.</summary>
    static uint8_t countBits64(ulong64_t bits);
};
#endif

#endif // __UTILS_H__
