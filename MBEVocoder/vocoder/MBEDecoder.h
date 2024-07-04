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
#if !defined(__MBE_DECODER_H__)
#define __MBE_DECODER_H__

extern "C" {
#include "mbe.h"
}

#include "Defines.h"

#include <stdlib.h>
#include <queue>

namespace vocoder
{
    // ---------------------------------------------------------------------------
    //  Structure Declaration
    //      
    // ---------------------------------------------------------------------------

    struct mbelibParms
    {
        mbe_parms* m_cur_mp;
        mbe_parms* m_prev_mp;
        mbe_parms* m_prev_mp_enhanced;

        /// <summary></summary>
        mbelibParms()
        {
            m_cur_mp = (mbe_parms*)malloc(sizeof(mbe_parms));
            m_prev_mp = (mbe_parms*)malloc(sizeof(mbe_parms));
            m_prev_mp_enhanced = (mbe_parms*)malloc(sizeof(mbe_parms));
        }

        /// <summary></summary>
        ~mbelibParms()
        {
            free(m_prev_mp_enhanced);
            free(m_prev_mp);
            free(m_cur_mp);
        }
    };

    // ---------------------------------------------------------------------------
    //  Constants
    // ---------------------------------------------------------------------------

    enum MBE_DECODER_MODE {
        DECODE_DMR_AMBE,
        DECODE_88BIT_IMBE   // e.g. IMBE used by P25
    };

    // ---------------------------------------------------------------------------
    //  Class Declaration
    //      Implements MBE audio decoding.
    // ---------------------------------------------------------------------------

    class HOST_SW_API MBEDecoder
    {
    public:
        /// <summary>Initializes a new instance of the MBEDecoder class.</summary>
        MBEDecoder(MBE_DECODER_MODE mode);
        /// <summary>Finalizes a instance of the MBEDecoder class.</summary>
        ~MBEDecoder();

        /// <summary>Decodes the given MBE codewords to deinterleaved MBE bits using the decoder mode.</summary>
        int32_t decodeBits(uint8_t* codeword, char* mbeBits);

        /// <summary>Decodes the given MBE codewords to PCM samples using the decoder mode.</summary>
        int32_t decodeF(uint8_t* codeword, float samples[]);
        /// <summary>Decodes the given MBE codewords to PCM samples using the decoder mode.</summary>
        int32_t decode(uint8_t* codeword, int16_t samples[]);

    private:
        mbelibParms* m_mbelibParms;

        MBE_DECODER_MODE m_mbeMode;

        static const int dW[72];
        static const int dX[72];
        static const int rW[36];
        static const int rX[36];
        static const int rY[36];
        static const int rZ[36];

        float gainMaxBuf[200];
        float* gainMaxBufPtr;
        int gainMaxIdx;

    public:
        /// <summary></summary>
        __PROPERTY(float, gainAdjust, GainAdjust);
        /// <summary></summary>
        __PROPERTY(bool, autoGain, AutoGain);
    };
} // namespace vocoder

#endif // __MBE_DECODER_H__
