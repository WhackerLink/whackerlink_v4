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
#define WIN32_LEAN_AND_MEAN
#include <windows.h>

/// <summary>
/// 
/// </summary>
/// <param name="hModule"></param>
/// <param name="reasonForCall"></param>
/// <param name="lpReserved"></param>
/// <returns></returns>
BOOL APIENTRY DllMain(HMODULE hModule, DWORD  reasonForCall, LPVOID lpReserved)
{
    switch (reasonForCall)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}
