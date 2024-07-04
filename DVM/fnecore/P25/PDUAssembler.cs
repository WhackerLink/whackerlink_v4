// SPDX-License-Identifier: AGPL-3.0-only
/**
* Digital Voice Modem - Fixed Network Equipment Core Library
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / Fixed Network Equipment Core Library
* @license AGPLv3 License (https://opensource.org/licenses/AGPL-3.0)
*
*   Copyright (C) 2023 Bryan Biedenkapp, N2PLL
*
*/

using System;
using System.Collections.Generic;

using fnecore.EDAC; 

namespace fnecore.P25
{
    /// <summary>
    /// 
    /// </summary>
    public enum PDUDecodeState
    {
        IDLE = 0,
        DECODING = 1
    } // public enum PDUDecodeState

    /// <summary>
    /// Implements helpers for dealing with FNE P25 PDU traffic.
    /// </summary>
    public class PDUAssembler
    {
        private PDUDecodeState decodeState;
        private uint dataOffset;
        private uint dataBlockCnt;
        private uint pduCount;

        private byte[] netPDU;
        private byte[] data;

        /// <summary>
        /// PDU Header.
        /// </summary>
        public DataHeader Header;

        /// <summary>
        /// PDU Second Header.
        /// </summary>
        public DataHeader SecondHeader;

        /// <summary>
        /// Flag indicating whether or not we are utilizing a secondary header.
        /// </summary>
        public bool UseSecondHeader;

        /// <summary>
        /// 
        /// </summary>
        public bool ExtendedAddress;

        /// <summary>
        /// Data blocks.
        /// </summary>
        public List<DataBlock> Blocks;

        /// <summary>
        /// Raw PDU user data.
        /// </summary>
        public byte[] UserData
        {
            get { return data; }
        }

        /// <summary>
        /// Length of raw PDU user data.
        /// </summary>
        public uint UserDataLength;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="PDUAssembler"/> class.
        /// </summary>
        public PDUAssembler()
        {
            decodeState = PDUDecodeState.IDLE;
            dataOffset = 0;
            dataBlockCnt = 0;
            pduCount = 0;

            netPDU = new byte[P25Defines.P25_MAX_PDU_COUNT * P25Defines.P25_LDU_FRAME_LENGTH_BYTES + 2U];
            data = new byte[P25Defines.P25_MAX_PDU_COUNT * P25Defines.P25_PDU_CONFIRMED_LENGTH_BYTES + 2U];
            UserDataLength = 0;

            Header = new DataHeader();
            SecondHeader = new DataHeader();
            UseSecondHeader = false;
            ExtendedAddress = false;

            Blocks = new List<DataBlock>();
        }

        /// <summary>
        /// Helper used to decode PDU traffic from the FNE network.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="blockLength"></param>
        public bool Decode(byte[] data, uint blockLength)
        {
            if (decodeState == PDUDecodeState.IDLE)
            {
                Header.Reset();
                SecondHeader.Reset();

                dataOffset = 0;
                dataBlockCnt = 0;
                pduCount = 0;

                decodeState = PDUDecodeState.DECODING;

                byte[] buffer = new byte[P25Defines.P25_PDU_FEC_LENGTH_BYTES];
                for (uint i = 0; i < P25Defines.P25_PDU_FEC_LENGTH_BYTES; i++)
                    buffer[i] = data[i + 24];

                bool ret = Header.Decode(buffer);
                if (!ret)
                {
                    // unfixable PDU data
                    Header.Reset();
                    SecondHeader.Reset();
                    dataBlockCnt = 0;
                    pduCount = 0;
                    decodeState = PDUDecodeState.IDLE;
                    return false;
                }

                // make sure we don't get a PDU with more blocks then we support
                if (Header.BlocksToFollow >= P25Defines.P25_MAX_PDU_COUNT)
                {
                    // too many PDU blocks to process
                    Header.Reset();
                    SecondHeader.Reset();
                    dataOffset = 0;
                    dataBlockCnt = 0;
                    pduCount = 0;
                    decodeState = PDUDecodeState.IDLE;
                    return false;
                }

                pduCount++;
            }

            if (decodeState == PDUDecodeState.DECODING)
            {
                for (uint i = 0; i < blockLength; i++)
                    netPDU[i + dataOffset] = data[i + 24];
                dataOffset += blockLength;
                pduCount++;
                dataBlockCnt++;

                if (dataBlockCnt >= Header.BlocksToFollow)
                {
                    byte blocksToFollow = Header.BlocksToFollow;
                    uint offset = 0;

                    byte[] buffer = new byte[P25Defines.P25_PDU_FEC_LENGTH_BYTES];

                    // process second header if we're using enhanced addressing
                    if (Header.SAP == P25Defines.PDU_SAP_EXT_ADDR && Header.Format == P25Defines.PDU_FMT_UNCONFIRMED)
                    {
                        for (uint i = 0; i < P25Defines.P25_PDU_FEC_LENGTH_BYTES; i++)
                            buffer[i] = netPDU[i];

                        bool ret = SecondHeader.Decode(buffer);
                        if (!ret)
                        {
                            // unfixable PDU data
                            Header.Reset();
                            SecondHeader.Reset();
                            dataBlockCnt = 0;
                            pduCount = 0;
                            UseSecondHeader = false;
                            decodeState = PDUDecodeState.IDLE;
                            return false;
                        }

                        UseSecondHeader = true;

                        offset += P25Defines.P25_PDU_FEC_LENGTH_BYTES;
                        blocksToFollow--;
                    }

                    dataBlockCnt = 0U;

                    // process all blocks in the data stream
                    uint dataOffset = 0U;

                    // if we are using a secondary header place it in the PDU user data buffer
                    if (UseSecondHeader)
                    {
                        SecondHeader.GetData(ref data, dataOffset);
                        dataOffset += P25Defines.P25_PDU_HEADER_LENGTH_BYTES;
                        UserDataLength += P25Defines.P25_PDU_HEADER_LENGTH_BYTES;
                    }

                    // decode data blocks
                    for (uint i = 0U; i < blocksToFollow; i++)
                    {
                        for (uint j = 0; j < P25Defines.P25_PDU_FEC_LENGTH_BYTES; j++)
                            buffer[j] = netPDU[j + offset];

                        DataBlock block = new DataBlock();
                        bool ret = block.Decode(buffer, (UseSecondHeader) ? SecondHeader : Header);
                        if (ret)
                        {
                            // if we are getting unconfirmed or confirmed blocks, and if we've reached the total number of blocks
                            // set this block as the last block for full packet CRC
                            if ((Header.Format == P25Defines.PDU_FMT_CONFIRMED) || (Header.Format == P25Defines.PDU_FMT_UNCONFIRMED))
                            {
                                if ((dataBlockCnt + 1U) == blocksToFollow)
                                    block.LastBlock = true;
                            }

                            // are we processing extended address data from the first block?
                            if (Header.SAP == P25Defines.PDU_SAP_EXT_ADDR && Header.Format == P25Defines.PDU_FMT_CONFIRMED &&
                                block.SerialNo == 0U)
                            {
                                SecondHeader.Reset();
                                SecondHeader.AckNeeded = true;
                                SecondHeader.Format = block.Format;
                                SecondHeader.LLId = block.LLId;
                                SecondHeader.SAP = block.SAP;
                                ExtendedAddress = true;
                            }

                            block.GetData(ref data, dataOffset);
                            dataOffset += (Header.Format == P25Defines.PDU_FMT_CONFIRMED) ? P25Defines.P25_PDU_CONFIRMED_DATA_LENGTH_BYTES : P25Defines.P25_PDU_UNCONFIRMED_LENGTH_BYTES;
                            UserDataLength = dataOffset;

                            dataBlockCnt++;

                            Blocks.Add(block);

                            // is this the last block?
                            if (block.LastBlock && dataBlockCnt == blocksToFollow)
                            {
                                bool crcRet = CRC.CheckCRC32(data, UserDataLength);
                                if (!crcRet)
                                    return false;
                                else
                                    return true;
                            }
                        }

                        offset += P25Defines.P25_PDU_FEC_LENGTH_BYTES;
                    }

                    decodeState = PDUDecodeState.IDLE;
                }
            }

            return false;
        }
    } // public class PDUAssembler
} // namespace fnecore.P25
