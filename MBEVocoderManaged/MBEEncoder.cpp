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
#include "vocoder/MBEEncoder.h"
#include "Common.h"

using namespace System;
using namespace System::Runtime::InteropServices;

namespace vocoder
{
    // ---------------------------------------------------------------------------
    //  Class Declaration
    //      Implements MBE audio encoding.
    // ---------------------------------------------------------------------------

    public ref class MBEEncoderManaged
    {
    public:
        static const int PCM_SAMPLES = 160;
        static const int AMBE_CODEWORD_SAMPLES = 9;
        static const int IMBE_CODEWORD_SAMPLES = 11;

        /// <summary>Initializes a new instance of the MBEEncoderManaged class.</summary>
        MBEEncoderManaged(MBEMode mode) :
            m_mode(mode)
        {
            switch (mode) {
            case MBEMode::DMRAMBE:
                m_encoder = new vocoder::MBEEncoder(vocoder::ENCODE_DMR_AMBE);
                break;
            case MBEMode::IMBE:
            default:
                m_encoder = new vocoder::MBEEncoder(vocoder::ENCODE_88BIT_IMBE);
                break;
            }
        }
        /// <summary>Finalizes a instance of the MBEEncoderManaged class.</summary>
        ~MBEEncoderManaged()
        {
            delete m_encoder;
        }

        /// <summary>Gets/sets the gain adjust for the MBE encoder.</summary>
        property float GainAdjust
        {
            float get() { return m_encoder->getGainAdjust(); }
            void set(float value) { m_encoder->setGainAdjust(value); }
        }

        /// <summary>Encodes the given PCM samples using the encoder mode to MBE codewords.</summary>
        void encode(array<Int16>^ samples, [Out] array<Byte>^% codeword)
        {
            codeword = nullptr;

            if (samples == nullptr) {
                throw gcnew System::NullReferenceException("samples");
            }

            // error check samples length
            if (samples->Length > PCM_SAMPLES) {
                throw gcnew System::ArgumentOutOfRangeException("samples length is > 160");
            }

            if (samples->Length < PCM_SAMPLES) {
                throw gcnew System::ArgumentOutOfRangeException("samples length is < 160");
            }

            // pin samples array and encode into codewords
            int16_t pcmSamples[PCM_SAMPLES];
            ::memset(pcmSamples, 0x00U, PCM_SAMPLES);
            pin_ptr<Int16> ppSamples = &samples[0];
            for (int n = 0; n < PCM_SAMPLES; n++) {
                pcmSamples[n] = samples[n];
            }

            // encode samples
            switch (m_mode) {
            case MBEMode::DMRAMBE:
            {
                uint8_t codewords[AMBE_CODEWORD_SAMPLES];
                m_encoder->encode(pcmSamples, codewords);
            
                // copy encoded codewords into the managed array
                codeword = gcnew array<Byte>(AMBE_CODEWORD_SAMPLES);
                pin_ptr<Byte> ppCodeword = &codeword[0];
                for (int n = 0; n < AMBE_CODEWORD_SAMPLES; n++) {
                    *ppCodeword = codewords[n];
                    ppCodeword++;
                }
            }
            break;
            case MBEMode::IMBE:
            default:
            {
                uint8_t codewords[IMBE_CODEWORD_SAMPLES];
                m_encoder->encode(pcmSamples, codewords);

                // copy encoded codewords into the managed array
                codeword = gcnew array<Byte>(IMBE_CODEWORD_SAMPLES);
                pin_ptr<Byte> ppCodeword = &codeword[0];
                for (int n = 0; n < IMBE_CODEWORD_SAMPLES; n++) {
                    *ppCodeword = codewords[n];
                    ppCodeword++;
                }
            }
            break;
            }
        }

    private:
        vocoder::MBEEncoder* m_encoder;
        MBEMode m_mode;
    };
} // namespace vocoder
