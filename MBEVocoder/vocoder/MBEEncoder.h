// SPDX-License-Identifier: GPL-2.0-only
/**
* Digital Voice Modem - MBE Vocoder
* GPLv2 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / MBE Vocoder
* @license GPLv2 License (https://opensource.org/licenses/GPL-2.0)
*
*   Copyright (C) 2019-2021 Doug McLain
*   Copyright (C) 2021 Bryan Biedenkapp, N2PLL
*
*/
#if !defined(__MBE_ENCODER_H__)
#define __MBE_ENCODER_H__

#include "Defines.h"
#include "mbe.h"
#include "imbe/imbe_vocoder.h"

#include <stdint.h>

namespace vocoder
{
    // ---------------------------------------------------------------------------
    //  Constants
    // ---------------------------------------------------------------------------

    enum MBE_ENCODER_MODE {
        ENCODE_DMR_AMBE,
        ENCODE_88BIT_IMBE,  // e.g. IMBE used by P25
    };

    // ---------------------------------------------------------------------------
    //  Class Declaration
    //      Implements MBE audio encoding.
    // ---------------------------------------------------------------------------

    class HOST_SW_API MBEEncoder {
    public:
        /// <summary>Initializes a new instance of the MBEEncoder class.</summary>
        MBEEncoder(MBE_ENCODER_MODE mode);

        /// <summary>Encodes the given MBE bits to deinterleaved MBE bits using the encoder mode.</summary>
        void encodeBits(uint8_t bits[], uint8_t codeword[]);

        /// <summary>Encodes the given PCM samples using the encoder mode to MBE codewords.</summary>
        void encode(int16_t samples[], uint8_t codeword[]);

    private:
        imbe_vocoder m_vocoder;
        mbe_parms m_curMBEParms;
        mbe_parms m_prevMBEParms;

        MBE_ENCODER_MODE m_mbeMode;

    public:
        /// <summary></summary>
        __PROPERTY(float, gainAdjust, GainAdjust);
    };
} // namespace vocoder

#endif // __MBE_ENCODER_H__
