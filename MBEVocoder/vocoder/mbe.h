// SPDX-License-Identifier: GPL-2.0-only
/**
* Digital Voice Modem - MBE Vocoder
* GPLv2 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / MBE Vocoder
* @license GPLv2 License (https://opensource.org/licenses/GPL-2.0)
*
*/
#if !defined(__MBE_H__)
#define __MBE_H__

/*
 * Copyright (C) 2010 mbelib Author
 * GPG Key ID: 0xEA5EFE2C (9E7A 5527 9CDC EBF7 BF1B  D772 4F98 E863 EA5E FE2C)
 *
 * Permission to use, copy, modify, and/or distribute this software for any
 * purpose with or without fee is hereby granted, provided that the above
 * copyright notice and this permission notice appear in all copies.
 *
 * THE SOFTWARE IS PROVIDED "AS IS" AND ISC DISCLAIMS ALL WARRANTIES WITH
 * REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
 * AND FITNESS.  IN NO EVENT SHALL ISC BE LIABLE FOR ANY SPECIAL, DIRECT,
 * INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
 * LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE
 * OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
 * PERFORMANCE OF THIS SOFTWARE.
 */

#ifdef __cplusplus
extern "C" {
#endif

// ---------------------------------------------------------------------------
//  Structures
// ---------------------------------------------------------------------------

// ---------------------------------------------------------------------------
//  Structure Declaration
//      
// ---------------------------------------------------------------------------

struct mbe_parameters
{
    float w0;
    int L;
    int K;
    int Vl[57];
    float Ml[57];
    float log2Ml[57];
    float PHIl[57];
    float PSIl[57];
    float gamma;
    int un;
    int repeat;
};

typedef struct mbe_parameters mbe_parms;

// ---------------------------------------------------------------------------
//  Structure Declaration
//      
// ---------------------------------------------------------------------------

struct mbe_tones
{
    int ID;
    int AD;
    int n;
};

typedef struct mbe_tones mbe_tone;

// ---------------------------------------------------------------------------
//  Global Functions
// ---------------------------------------------------------------------------
/*
** Prototypes from ecc.c
*/
/// <summary></summary>
void mbe_checkGolayBlock(long int* block);
/// <summary></summary>
int mbe_golay2312(char* in, char* out);
/// <summary></summary>
int mbe_hamming1511(char* in, char* out);
/// <summary></summary>
int mbe_7100x4400Hamming1511(char* in, char* out);

/*
** Prototypes from ambe3600x2400.c
*/
/// <summary></summary>
int mbe_eccAmbe3600x2400C0(char ambe_fr[4][24]);
/// <summary></summary>
int mbe_eccAmbe3600x2400Data(char ambe_fr[4][24], char* ambe_d);
/// <summary></summary>
int mbe_decodeAmbe2400Parms(char* ambe_d, mbe_parms* cur_mp, mbe_parms* prev_mp);
/// <summary></summary>
void mbe_demodulateAmbe3600x2400Data(char ambe_fr[4][24]);
/// <summary></summary>
void mbe_processAmbe2400DataF(float* aout_buf, int* errs, int* errs2, char* err_str, char ambe_d[49], mbe_parms* cur_mp, mbe_parms* prev_mp, mbe_parms* prev_mp_enhanced, int uvquality);
/// <summary></summary>
void mbe_processAmbe2400Data(short* aout_buf, int* errs, int* errs2, char* err_str, char ambe_d[49], mbe_parms* cur_mp, mbe_parms* prev_mp, mbe_parms* prev_mp_enhanced, int uvquality);
/// <summary></summary>
void mbe_processAmbe3600x2400FrameF(float* aout_buf, int* errs, int* errs2, char* err_str, char ambe_fr[4][24], char ambe_d[49], mbe_parms* cur_mp, mbe_parms* prev_mp, mbe_parms* prev_mp_enhanced, int uvquality);
/// <summary></summary>
void mbe_processAmbe3600x2400Frame(short* aout_buf, int* errs, int* errs2, char* err_str, char ambe_fr[4][24], char ambe_d[49], mbe_parms* cur_mp, mbe_parms* prev_mp, mbe_parms* prev_mp_enhanced, int uvquality);

/*
** Prototypes from ambe3600x2450.c
*/
/// <summary></summary>
int mbe_eccAmbe3600x2450C0(char ambe_fr[4][24]);
/// <summary></summary>
int mbe_eccAmbe3600x2450Data(char ambe_fr[4][24], char* ambe_d);
/// <summary></summary>
int mbe_decodeAmbe2450Parms(char* ambe_d, mbe_parms* cur_mp, mbe_parms* prev_mp);
/// <summary></summary>
void mbe_demodulateAmbe3600x2450Data(char ambe_fr[4][24]);
/// <summary></summary>
void mbe_processAmbe2450DataF(float* aout_buf, int* errs, int* errs2, char* err_str, char ambe_d[49], mbe_parms* cur_mp, mbe_parms* prev_mp, mbe_parms* prev_mp_enhanced, int uvquality);
/// <summary></summary>
void mbe_processAmbe2450Data(short* aout_buf, int* errs, int* errs2, char* err_str, char ambe_d[49], mbe_parms* cur_mp, mbe_parms* prev_mp, mbe_parms* prev_mp_enhanced, int uvquality);
/// <summary></summary>
void mbe_processAmbe3600x2450FrameF(float* aout_buf, int* errs, int* errs2, char* err_str, char ambe_fr[4][24], char ambe_d[49], mbe_parms* cur_mp, mbe_parms* prev_mp, mbe_parms* prev_mp_enhanced, int uvquality);
/// <summary></summary>
void mbe_processAmbe3600x2450Frame(short* aout_buf, int* errs, int* errs2, char* err_str, char ambe_fr[4][24], char ambe_d[49], mbe_parms* cur_mp, mbe_parms* prev_mp, mbe_parms* prev_mp_enhanced, int uvquality);

/*
** Prototypes from ambe3600x2250.c
*/
/// <summary></summary>
int mbe_dequantizeAmbe2250Parms(mbe_parms* cur_mp, mbe_parms* prev_mp, const int* b);
/// <summary></summary>
int mbe_dequantizeAmbeTone(mbe_tone* tone, const int* u);

/*
** Prototypes from imbe7200x4400.c
*/
/// <summary></summary>
int mbe_eccImbe7200x4400C0(char imbe_fr[8][23]);
/// <summary></summary>
int mbe_eccImbe7200x4400Data(char imbe_fr[8][23], char* imbe_d);
/// <summary></summary>
int mbe_decodeImbe4400Parms(char* imbe_d, mbe_parms* cur_mp, mbe_parms* prev_mp);
/// <summary></summary>
void mbe_demodulateImbe7200x4400Data(char imbe[8][23]);
/// <summary></summary>
void mbe_processImbe4400DataF(float* aout_buf, int* errs, int* errs2, char* err_str, char imbe_d[88], mbe_parms* cur_mp, mbe_parms* prev_mp, mbe_parms* prev_mp_enhanced, int uvquality);
/// <summary></summary>
void mbe_processImbe4400Data(short* aout_buf, int* errs, int* errs2, char* err_str, char imbe_d[88], mbe_parms* cur_mp, mbe_parms* prev_mp, mbe_parms* prev_mp_enhanced, int uvquality);
/// <summary></summary>
void mbe_processImbe7200x4400FrameF(float* aout_buf, int* errs, int* errs2, char* err_str, char imbe_fr[8][23], char imbe_d[88], mbe_parms* cur_mp, mbe_parms* prev_mp, mbe_parms* prev_mp_enhanced, int uvquality);
/// <summary></summary>
void mbe_processImbe7200x4400Frame(short* aout_buf, int* errs, int* errs2, char* err_str, char imbe_fr[8][23], char imbe_d[88], mbe_parms* cur_mp, mbe_parms* prev_mp, mbe_parms* prev_mp_enhanced, int uvquality);

/*
** Prototypes from mbelib.c
*/
/// <summary></summary>
void mbe_moveMbeParms(mbe_parms* cur_mp, mbe_parms* prev_mp);
/// <summary></summary>
void mbe_useLastMbeParms(mbe_parms* cur_mp, mbe_parms* prev_mp);
/// <summary></summary>
void mbe_initMbeParms(mbe_parms* cur_mp, mbe_parms* prev_mp, mbe_parms* prev_mp_enhanced);
/// <summary></summary>
void mbe_spectralAmpEnhance(mbe_parms* cur_mp);
/// <summary></summary>
void mbe_synthesizeSilenceF(float* aout_buf);
/// <summary></summary>
void mbe_synthesizeSilence(short* aout_buf);
/// <summary></summary>
void mbe_synthesizeSpeechf(float* aout_buf, mbe_parms* cur_mp, mbe_parms* prev_mp, int uvquality);
/// <summary></summary>
void mbe_synthesizeSpeech(short* aout_buf, mbe_parms* cur_mp, mbe_parms* prev_mp, int uvquality);
/// <summary></summary>
void mbe_floatToShort(float* float_buf, short* aout_buf);

#ifdef __cplusplus
}
#endif

#endif // __MBE_H__
