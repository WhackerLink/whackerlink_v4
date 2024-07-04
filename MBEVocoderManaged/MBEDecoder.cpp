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
#include "vocoder/MBEDecoder.h"
#include "Common.h"

using namespace System;
using namespace System::Runtime::InteropServices;

namespace vocoder
{
    // ---------------------------------------------------------------------------
    //  Class Declaration
    //      Implements MBE audio decoding.
    // ---------------------------------------------------------------------------

    public ref class MBEDecoderManaged
    {
    public:
        static const int PCM_SAMPLES = 160;
        static const int AMBE_CODEWORD_SAMPLES = 9;
        static const int IMBE_CODEWORD_SAMPLES = 11;

        /// <summary>Initializes a new instance of the MBEDecoderManaged class.</summary>
        MBEDecoderManaged(MBEMode mode) :
            m_mode(mode)
        {
            switch (mode) {
            case MBEMode::DMRAMBE:
                m_decoder = new vocoder::MBEDecoder(vocoder::DECODE_DMR_AMBE);
                break;
            case MBEMode::IMBE:
            default:
                m_decoder = new vocoder::MBEDecoder(vocoder::DECODE_88BIT_IMBE);
                break;
            }
        }
        /// <summary>Finalizes a instance of the MBEDecoderManaged class.</summary>
        ~MBEDecoderManaged()
        {
            delete m_decoder;
        }

        /// <summary>Gets/sets the gain adjust for the MBE decoder.</summary>
        property float GainAdjust
        {
            float get() { return m_decoder->getGainAdjust(); }
            void set(float value) { m_decoder->setGainAdjust(value); }
        }

        /// <summary>Flag indicating the MBE decoder is using AGC.</summary>
        property bool AutoGain
        {
            bool get() { return m_decoder->getAutoGain(); }
            void set(bool value) { m_decoder->setAutoGain(value); }
        }

        /// <summary>Decodes the given MBE codewords to PCM samples using the decoder mode.</summary>
        Int32 decodeF(array<Byte>^ codeword, [Out] array<float>^% samples)
        {
            samples = nullptr;

            if (codeword == nullptr) {
                throw gcnew System::NullReferenceException("codeword");
            }

            // error check codeword length based on mode
            switch (m_mode) {
            case MBEMode::DMRAMBE:
            {
                if (codeword->Length > AMBE_CODEWORD_SAMPLES) {
                    throw gcnew System::ArgumentOutOfRangeException("AMBE codeword length is > 9");
                }

                if (codeword->Length < AMBE_CODEWORD_SAMPLES) {
                    throw gcnew System::ArgumentOutOfRangeException("AMBE codeword length is < 9");
                }
            }
            break;
            case MBEMode::IMBE:
            default:
            {
                if (codeword->Length > IMBE_CODEWORD_SAMPLES) {
                    throw gcnew System::ArgumentOutOfRangeException("IMBE codeword length is > 11");
                }

                if (codeword->Length < IMBE_CODEWORD_SAMPLES) {
                    throw gcnew System::ArgumentOutOfRangeException("IMBE codeword length is < 11");
                }
            }
            break;
            }

            // pin codeword byte array and decode into PCM samples
            pin_ptr<Byte> ppCodeword = &codeword[0];
            uint8_t* pCodeword = ppCodeword;

            float pcmSamples[PCM_SAMPLES];
            ::memset(pcmSamples, 0x00U, PCM_SAMPLES);
            int errs = m_decoder->decodeF(pCodeword, pcmSamples);

            // copy decoded PCM samples into the managed array
            samples = gcnew array<float>(PCM_SAMPLES);
            pin_ptr<float> ppSamples = &samples[0];
            for (int n = 0; n < PCM_SAMPLES; n++) {
                *ppSamples = pcmSamples[n];
                ppSamples++;
            }

            return errs;
        }

        /// <summary>Decodes the given MBE codewords to PCM samples using the decoder mode.</summary>
        Int32 decode(array<Byte>^ codeword, [Out] array<Int16>^% samples)
        {
            samples = nullptr;

            if (codeword == nullptr) {
                throw gcnew System::NullReferenceException("codeword");
            }

            // error check codeword length based on mode
            switch (m_mode) {
            case MBEMode::DMRAMBE:
            {
                if (codeword->Length > AMBE_CODEWORD_SAMPLES) {
                    throw gcnew System::ArgumentOutOfRangeException("AMBE codeword length is > 9");
                }

                if (codeword->Length < AMBE_CODEWORD_SAMPLES) {
                    throw gcnew System::ArgumentOutOfRangeException("AMBE codeword length is < 9");
                }
            }
            break;
            case MBEMode::IMBE:
            default:
            {
                if (codeword->Length > IMBE_CODEWORD_SAMPLES) {
                    throw gcnew System::ArgumentOutOfRangeException("IMBE codeword length is > 11");
                }

                if (codeword->Length < IMBE_CODEWORD_SAMPLES) {
                    throw gcnew System::ArgumentOutOfRangeException("IMBE codeword length is < 11");
                }
            }
            break;
            }

            // pin codeword byte array and decode into PCM samples
            pin_ptr<Byte> ppCodeword = &codeword[0];
            uint8_t* pCodeword = ppCodeword;

            int16_t pcmSamples[PCM_SAMPLES];
            ::memset(pcmSamples, 0x00U, PCM_SAMPLES);
            int errs = m_decoder->decode(pCodeword, pcmSamples);

            // copy decoded PCM samples into the managed array
            samples = gcnew array<Int16>(PCM_SAMPLES);
            pin_ptr<Int16> ppSamples = &samples[0];
            for (int n = 0; n < PCM_SAMPLES; n++) {
                *ppSamples = pcmSamples[n];
                ppSamples++;
            }

            return errs;
        }
    private:
        vocoder::MBEDecoder* m_decoder;
        MBEMode m_mode;
    };
} // namespace vocoder
