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
*   Copyright (C) 2015,2016,2017 Jonathan Naylor, G4KLX
*   Copyright (C) 2018-2021 Bryan Biedenkapp, N2PLL
*
*/
#if !defined(__DEFINES_H__)
#define __DEFINES_H__

#include <stdint.h>

#ifdef __cplusplus
#include <memory>
#include <string>
#include <sstream>
#include <ios>
#endif

// ---------------------------------------------------------------------------
//  Types
// ---------------------------------------------------------------------------

#ifndef _INT8_T_DECLARED
#ifndef __INT8_TYPE__
typedef signed char         int8_t;
#endif // __INT8_TYPE__
#endif // _INT8_T_DECLARED
#ifndef _INT16_T_DECLARED
#ifndef __INT16_TYPE__
typedef short               int16_t;
#endif // __INT16_TYPE__
#endif // _INT16_T_DECLARED
#ifndef _INT32_T_DECLARED
#ifndef __INT32_TYPE__
typedef int                 int32_t;
#endif // __INT32_TYPE__
#endif // _INT32_T_DECLARED
#ifndef _INT64_T_DECLARED
#ifndef __INT64_TYPE__
typedef long long           int64_t;
#endif // __INT64_TYPE__
#endif // _INT64_T_DECLARED
#ifndef _UINT8_T_DECLARED
#ifndef __UINT8_TYPE__
typedef unsigned char       uint8_t;
#endif // __UINT8_TYPE__
#endif // _UINT8_T_DECLARED
#ifndef _UINT16_T_DECLARED
#ifndef __UINT16_TYPE__
typedef unsigned short      uint16_t;
#endif // __UINT16_TYPE__
#endif // _UINT16_T_DECLARED
#ifndef _UINT32_T_DECLARED
#ifndef __UINT32_TYPE__
typedef unsigned int        uint32_t;
#endif // __UINT32_TYPE__
#endif // _UINT32_T_DECLARED
#ifndef _UINT64_T_DECLARED
#ifndef __UINT64_TYPE__
typedef unsigned long long  uint64_t;
#endif // __UINT64_TYPE__
#endif // _UINT64_T_DECLARED

#ifndef __LONG64_TYPE__
typedef long long           long64_t;
#endif // __LONG64_TYPE__
#ifndef __ULONG64_TYPE__
typedef unsigned long long  ulong64_t;
#endif // __ULONG64_TYPE__

// ---------------------------------------------------------------------------
//  Constants
// ---------------------------------------------------------------------------

#if defined(_COMPILE_DLL)
#define HOST_SW_API     __declspec(dllexport)
#else
#define HOST_SW_API     __declspec(dllimport)
#endif

#if defined(__GNUC__) || defined(__GNUG__)
#define __forceinline __attribute__((always_inline))
#endif

const uint8_t   BIT_MASK_TABLE[] = { 0x80U, 0x40U, 0x20U, 0x10U, 0x08U, 0x04U, 0x02U, 0x01U };

// ---------------------------------------------------------------------------
//  Inlines
// ---------------------------------------------------------------------------

#ifdef __cplusplus
inline std::string __BOOL_STR(const bool& value) {
    std::stringstream ss;
    ss << std::boolalpha << value;
    return ss.str();
}

inline std::string __INT_STR(const int& value) {
    std::stringstream ss;
    ss << value;
    return ss.str();
}

inline std::string __FLOAT_STR(const float& value) {
    std::stringstream ss;
    ss << value;
    return ss.str();
}
#endif

// ---------------------------------------------------------------------------
//  Macros
// ---------------------------------------------------------------------------

#define __FLOAT_ADDR(x)  (*(uint32_t*)& x)
#define __DOUBLE_ADDR(x) (*(uint64_t*)& x)

#define WRITE_BIT(p, i, b) p[(i) >> 3] = (b) ? (p[(i) >> 3] | BIT_MASK_TABLE[(i) & 7]) : (p[(i) >> 3] & ~BIT_MASK_TABLE[(i) & 7])
#define READ_BIT(p, i)     (p[(i) >> 3] & BIT_MASK_TABLE[(i) & 7])

#define __SET_UINT32(val, buffer, offset)           \
            buffer[0U + offset] = val >> 24;        \
            buffer[1U + offset] = val >> 16;        \
            buffer[2U + offset] = val >> 8;         \
            buffer[3U + offset] = val >> 0;          
#define __GET_UINT32(buffer, offset)                \
            (buffer[offset + 0U] << 24)     |       \
                (buffer[offset + 1U] << 16) |       \
                (buffer[offset + 2U] << 8)  |       \
                (buffer[offset + 3U] << 0);
#define __SET_UINT16(val, buffer, offset)           \
            buffer[0U + offset] = val >> 16;        \
            buffer[1U + offset] = val >> 8;         \
            buffer[2U + offset] = val >> 0;          
#define __GET_UINT16(buffer, offset)                \
            (buffer[offset + 0U] << 16)     |       \
                (buffer[offset + 1U] << 8)  |       \
                (buffer[offset + 2U] << 0);

#define new_unique(type, ...) std::unique_ptr<type>(new type(__VA_ARGS__))

/**
 * Property Creation
 *  These macros should always be used LAST in the "public" section of a class definition.
 */
#ifdef __cplusplus
/// <summary>Creates a read-only get property.</summary>
#define __READONLY_PROPERTY(type, variableName, propName)                               \
        private: type m_##variableName;                                                 \
        public: __forceinline type get##propName(void) const { return m_##variableName; }
/// <summary>Creates a read-only get property, does not use "get".</summary>
#define __READONLY_PROPERTY_PLAIN(type, variableName, propName)                         \
        private: type m_##variableName;                                                 \
        public: __forceinline type propName(void) const { return m_##variableName; }
/// <summary>Creates a read-only get property by reference.</summary>
#define __READONLY_PROPERTY_BYREF(type, variableName, propName)                         \
        private: type m_##variableName;                                                 \
        public: __forceinline type& get##propName(void) const { return m_##variableName; }

/// <summary>Creates a get and set property.</summary>
#define __PROPERTY(type, variableName, propName)                                        \
        private: type m_##variableName;                                                 \
        public: __forceinline type get##propName(void) const { return m_##variableName; } \
                __forceinline void set##propName(type val) { m_##variableName = val; }
/// <summary>Creates a get and set property, does not use "get"/"set".</summary>
#define __PROPERTY_PLAIN(type, variableName, propName)                                  \
        private: type m_##variableName;                                                 \
        public: __forceinline type propName(void) const { return m_##variableName; }    \
                __forceinline void propName(type val) { m_##variableName = val; }
/// <summary>Creates a get and set property by reference.</summary>
#define __PROPERTY_BYREF(type, variableName, propName)                                  \
        private: type m_##variableName;                                                 \
        public: __forceinline type& get##propName(void) const { return m_##variableName; } \
                __forceinline void set##propName(type& val) { m_##variableName = val; }
#endif
#endif // __DEFINES_H__
