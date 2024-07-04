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
#include "vocoder/MBEEncoder.h"
#include "Common.h"

using namespace System;
using namespace System::Runtime::InteropServices;

namespace vocoder
{
    // ---------------------------------------------------------------------------
    //  Class Declaration
    // 
    // ---------------------------------------------------------------------------

    public ref class MBEInterleaver
    {
    public:
        static const int PCM_SAMPLES = 160;
        static const int AMBE_CODEWORD_SAMPLES = 9;
        static const int AMBE_CODEWORD_BITS = 49;
        static const int IMBE_CODEWORD_SAMPLES = 11;
        static const int IMBE_CODEWORD_BITS = 88;

        /// <summary>Initializes a new instance of the MBEInterleaver class.</summary>
        MBEInterleaver(MBEMode mode) :
            m_mode(mode)
        {
            switch (mode) {
            case MBEMode::DMRAMBE:
                m_decoder = new vocoder::MBEDecoder(vocoder::DECODE_DMR_AMBE);
                m_encoder = new vocoder::MBEEncoder(vocoder::ENCODE_DMR_AMBE);
                break;
            case MBEMode::IMBE:
            default:
                m_decoder = new vocoder::MBEDecoder(vocoder::DECODE_88BIT_IMBE);
                m_encoder = new vocoder::MBEEncoder(vocoder::ENCODE_88BIT_IMBE);
                break;
            }
        }
        /// <summary>Finalizes a instance of the MBEInterleaver class.</summary>
        ~MBEInterleaver()
        {
            delete m_decoder;
            delete m_encoder;
        }

        /// <summary>Helper to decode the input MBE codeword to MBE bits.</summary>
        Int32 decode(array<Byte>^ codeword, [Out] array<Byte>^% mbeBits)
        {
            mbeBits = nullptr;

            if (codeword == nullptr) {
                throw gcnew System::NullReferenceException("codeword");
            }

            char* bits = nullptr;

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

                bits = new char[AMBE_CODEWORD_BITS];
                ::memset(bits, 0x00U, AMBE_CODEWORD_BITS);
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

                bits = new char[IMBE_CODEWORD_BITS];
                ::memset(bits, 0x00U, IMBE_CODEWORD_BITS);
            }
            break;
            }

            // pin codeword byte array and decode into MBE bits
            pin_ptr<Byte> ppCodeword = &codeword[0];
            uint8_t* pCodeword = ppCodeword;

            int errs = m_decoder->decodeBits(pCodeword, bits);

            switch (m_mode) {
            case MBEMode::DMRAMBE:
            {
                // copy decoded MBE bits into the managed array
                mbeBits = gcnew array<Byte>(AMBE_CODEWORD_BITS);
                pin_ptr<Byte> ppBits = &mbeBits[0];
                for (int n = 0; n < AMBE_CODEWORD_BITS; n++) {
                    *ppBits = bits[n];
                    ppBits++;
                }
            }
            break;
            case MBEMode::IMBE:
            default:
            {
                // copy decoded MBE bits into the managed array
                mbeBits = gcnew array<Byte>(IMBE_CODEWORD_BITS);
                pin_ptr<Byte> ppBits = &mbeBits[0];
                for (int n = 0; n < IMBE_CODEWORD_BITS; n++) {
                    *ppBits = bits[n];
                    ppBits++;
                }
            }
            break;
            }

            delete[] bits;
            return errs;
        }

        /// <summary>Encodes the given MBE bits using the encoder mode to MBE codewords.</summary>
        void encode(array<Byte>^ mbeBits, [Out] array<Byte>^% codeword)
        {
            codeword = nullptr;

            if (mbeBits == nullptr) {
                throw gcnew System::NullReferenceException("samples");
            }

            uint8_t* bits = nullptr;

            // error check codeword length based on mode
            switch (m_mode) {
            case MBEMode::DMRAMBE:
            {
                if (mbeBits->Length > AMBE_CODEWORD_BITS) {
                    throw gcnew System::ArgumentOutOfRangeException("AMBE codeword length is > 49");
                }

                if (mbeBits->Length < AMBE_CODEWORD_BITS) {
                    throw gcnew System::ArgumentOutOfRangeException("AMBE codeword length is < 49");
                }

                bits = new uint8_t[AMBE_CODEWORD_BITS];
                ::memset(bits, 0x00U, AMBE_CODEWORD_BITS);
                for (int n = 0; n < AMBE_CODEWORD_BITS; n++) {
                    bits[n] = mbeBits[n];
                }
            }
            break;
            case MBEMode::IMBE:
            default:
            {
                if (mbeBits->Length > IMBE_CODEWORD_BITS) {
                    throw gcnew System::ArgumentOutOfRangeException("IMBE codeword length is > 88");
                }

                if (mbeBits->Length < IMBE_CODEWORD_BITS) {
                    throw gcnew System::ArgumentOutOfRangeException("IMBE codeword length is < 88");
                }

                bits = new uint8_t[IMBE_CODEWORD_BITS];
                ::memset(bits, 0x00U, IMBE_CODEWORD_BITS);
                for (int n = 0; n < IMBE_CODEWORD_BITS; n++) {
                    bits[n] = mbeBits[n];
                }
            }
            break;
            }

            // encode samples
            switch (m_mode) {
            case MBEMode::DMRAMBE:
            {
                uint8_t codewords[AMBE_CODEWORD_SAMPLES];
                m_encoder->encodeBits(bits, codewords);

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
                m_encoder->encodeBits(bits, codewords);

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

            delete[] bits;
        }

    private:
        vocoder::MBEDecoder* m_decoder;
        vocoder::MBEEncoder* m_encoder;
        MBEMode m_mode;
    };
} // namespace vocoder
