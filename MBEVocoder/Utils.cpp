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
#include "Utils.h"

#include <cstdio>
#include <cassert>

// ---------------------------------------------------------------------------
//  Constants/Macros
// ---------------------------------------------------------------------------

const uint8_t BITS_TABLE[] = {
#   define B2(n) n,     n+1,     n+1,     n+2
#   define B4(n) B2(n), B2(n+1), B2(n+1), B2(n+2)
#   define B6(n) B4(n), B4(n+1), B4(n+1), B4(n+2)
    B6(0), B6(1), B6(1), B6(2)
};

// ---------------------------------------------------------------------------
//  Static Class Members
// ---------------------------------------------------------------------------

/// <summary>
///
/// </summary>
/// <param name="byte"></param>
/// <param name="bits"></param>
void Utils::byteToBitsBE(uint8_t byte, bool* bits)
{
    assert(bits != NULL);

    bits[0U] = (byte & 0x80U) == 0x80U;
    bits[1U] = (byte & 0x40U) == 0x40U;
    bits[2U] = (byte & 0x20U) == 0x20U;
    bits[3U] = (byte & 0x10U) == 0x10U;
    bits[4U] = (byte & 0x08U) == 0x08U;
    bits[5U] = (byte & 0x04U) == 0x04U;
    bits[6U] = (byte & 0x02U) == 0x02U;
    bits[7U] = (byte & 0x01U) == 0x01U;
}

/// <summary>
///
/// </summary>
/// <param name="byte"></param>
/// <param name="bits"></param>
void Utils::byteToBitsLE(uint8_t byte, bool* bits)
{
    assert(bits != NULL);

    bits[0U] = (byte & 0x01U) == 0x01U;
    bits[1U] = (byte & 0x02U) == 0x02U;
    bits[2U] = (byte & 0x04U) == 0x04U;
    bits[3U] = (byte & 0x08U) == 0x08U;
    bits[4U] = (byte & 0x10U) == 0x10U;
    bits[5U] = (byte & 0x20U) == 0x20U;
    bits[6U] = (byte & 0x40U) == 0x40U;
    bits[7U] = (byte & 0x80U) == 0x80U;
}

/// <summary>
///
/// </summary>
/// <param name="bits"></param>
/// <param name="byte"></param>
void Utils::bitsToByteBE(const bool* bits, uint8_t& byte)
{
    assert(bits != NULL);

    byte = bits[0U] ? 0x80U : 0x00U;
    byte |= bits[1U] ? 0x40U : 0x00U;
    byte |= bits[2U] ? 0x20U : 0x00U;
    byte |= bits[3U] ? 0x10U : 0x00U;
    byte |= bits[4U] ? 0x08U : 0x00U;
    byte |= bits[5U] ? 0x04U : 0x00U;
    byte |= bits[6U] ? 0x02U : 0x00U;
    byte |= bits[7U] ? 0x01U : 0x00U;
}

/// <summary>
///
/// </summary>
/// <param name="bits"></param>
/// <param name="byte"></param>
void Utils::bitsToByteLE(const bool* bits, uint8_t& byte)
{
    assert(bits != NULL);

    byte = bits[0U] ? 0x01U : 0x00U;
    byte |= bits[1U] ? 0x02U : 0x00U;
    byte |= bits[2U] ? 0x04U : 0x00U;
    byte |= bits[3U] ? 0x08U : 0x00U;
    byte |= bits[4U] ? 0x10U : 0x00U;
    byte |= bits[5U] ? 0x20U : 0x00U;
    byte |= bits[6U] ? 0x40U : 0x00U;
    byte |= bits[7U] ? 0x80U : 0x00U;
}

/// <summary>
///
/// </summary>
/// <param name="in"></param>
/// <param name="out"></param>
/// <param name="start"></param>
/// <param name="stop"></param>
uint32_t Utils::getBits(const uint8_t* in, uint8_t* out, uint32_t start, uint32_t stop)
{
    assert(in != NULL);
    assert(out != NULL);

    uint32_t n = 0U;
    for (uint32_t i = start; i < stop; i++, n++) {
        bool b = READ_BIT(in, i);
        WRITE_BIT(out, n, b);
    }

    return n;
}

/// <summary>
///
/// </summary>
/// <param name="in"></param>
/// <param name="out"></param>
/// <param name="start"></param>
/// <param name="length"></param>
uint32_t Utils::getBitRange(const uint8_t* in, uint8_t* out, uint32_t start, uint32_t length)
{
    return getBits(in, out, start, start + length);
}

/// <summary>
///
/// </summary>
/// <param name="in"></param>
/// <param name="out"></param>
/// <param name="start"></param>
/// <param name="stop"></param>
uint32_t Utils::setBits(const uint8_t* in, uint8_t* out, uint32_t start, uint32_t stop)
{
    assert(in != NULL);
    assert(out != NULL);

    uint32_t n = 0U;
    for (uint32_t i = start; i < stop; i++, n++) {
        bool b = READ_BIT(in, n);
        WRITE_BIT(out, i, b);
    }

    return n;
}

/// <summary>
///
/// </summary>
/// <param name="in"></param>
/// <param name="out"></param>
/// <param name="start"></param>
/// <param name="length"></param>
uint32_t Utils::setBitRange(const uint8_t* in, uint8_t* out, uint32_t start, uint32_t length)
{
    return setBits(in, out, start, start + length);
}

/// <summary>
/// Returns the count of bits in the passed 8 byte value.
/// </summary>
/// <param name="bits"></param>
/// <returns></returns>
uint8_t Utils::countBits8(uint8_t bits)
{
    return BITS_TABLE[bits];
}

/// <summary>
/// Returns the count of bits in the passed 32 byte value.
/// </summary>
/// <param name="bits"></param>
/// <returns></returns>
uint8_t Utils::countBits32(uint32_t bits)
{
    uint8_t* p = (uint8_t*)&bits;
    uint8_t n = 0U;
    n += BITS_TABLE[p[0U]];
    n += BITS_TABLE[p[1U]];
    n += BITS_TABLE[p[2U]];
    n += BITS_TABLE[p[3U]];
    return n;
}

/// <summary>
/// Returns the count of bits in the passed 64 byte value.
/// </summary>
/// <param name="bits"></param>
/// <returns></returns>
uint8_t Utils::countBits64(ulong64_t bits)
{
    uint8_t* p = (uint8_t*)&bits;
    uint8_t n = 0U;
    n += BITS_TABLE[p[0U]];
    n += BITS_TABLE[p[1U]];
    n += BITS_TABLE[p[2U]];
    n += BITS_TABLE[p[3U]];
    n += BITS_TABLE[p[4U]];
    n += BITS_TABLE[p[5U]];
    n += BITS_TABLE[p[6U]];
    n += BITS_TABLE[p[7U]];
    return n;
}
