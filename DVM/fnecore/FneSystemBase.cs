// SPDX-License-Identifier: AGPL-3.0-only
/**
* Digital Voice Modem - Fixed Network Equipment Core Library
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / Fixed Network Equipment Core Library
* @license AGPLv3 License (https://opensource.org/licenses/AGPL-3.0)
*
*   Copyright (C) 2024 Bryan Biedenkapp, N2PLL
*
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using fnecore.EDAC;
using fnecore.DMR;
using fnecore.P25;
using fnecore.NXDN;

namespace fnecore
{
    /// <summary>
    /// Metadata class containing remote call data.
    /// </summary>
    public class RemoteCallData
    {
        /// <summary>
        /// Source ID.
        /// </summary>
        public uint SrcId = 0;
        /// <summary>
        /// Destination ID.
        /// </summary>
        public uint DstId = 0;

        /// <summary>
        /// Link-Control Opcode.
        /// </summary>
        public byte LCO = 0;
        /// <summary>
        /// Manufacturer ID.
        /// </summary>
        public byte MFId = 0;
        /// <summary>
        /// Service Options.
        /// </summary>
        public byte ServiceOptions = 0;

        /// <summary>
        /// Low-speed Data Byte 1
        /// </summary>
        public byte LSD1 = 0;
        /// <summary>
        /// Low-speed Data Byte 2
        /// </summary>
        public byte LSD2 = 0;

        /// <summary>
        /// Encryption Message Indicator
        /// </summary>
        public byte[] MessageIndicator = new byte[P25Defines.P25_MI_LENGTH];

        /// <summary>
        /// Algorithm ID.
        /// </summary>
        public byte AlgorithmId = P25Defines.P25_ALGO_UNENCRYPT;
        /// <summary>
        /// Key ID.
        /// </summary>
        public ushort KeyId = 0;

        /// <summary>
        /// 
        /// </summary>
        public uint TxStreamID = 0;

        /// <summary>
        /// 
        /// </summary>
        public FrameType FrameType = FrameType.TERMINATOR;
        /// <summary>
        /// 
        /// </summary>
        public byte Slot = 0;

        /*
        ** Methods
        */

        /// <summary>
        /// Reset values.
        /// </summary>
        public virtual void Reset()
        {
            SrcId = 0;
            DstId = 0;

            LCO = 0;
            MFId = 0;
            ServiceOptions = 0;

            LSD1 = 0;
            LSD2 = 0;

            MessageIndicator = new byte[P25Defines.P25_MI_LENGTH];

            AlgorithmId = P25Defines.P25_ALGO_UNENCRYPT;
            KeyId = 0;

            FrameType = FrameType.TERMINATOR;
            Slot = 0;
        }
    } // public class RemoteCallData

    /// <summary>
    /// Implements a FNE system.
    /// </summary>
    public abstract class FneSystemBase
    {
        protected FnePeer fne;

        protected const int DMR_FRAME_LENGTH_BYTES = 33;
        protected const int DMR_PACKET_SIZE = 55;

        protected static readonly byte[] DMR_SILENCE_DATA = { 0x01, 0x00,
            0xB9, 0xE8, 0x81, 0x52, 0x61, 0x73, 0x00, 0x2A, 0x6B, 0xB9, 0xE8,
            0x81, 0x52, 0x60, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x73, 0x00,
            0x2A, 0x6B, 0xB9, 0xE8, 0x81, 0x52, 0x61, 0x73, 0x00, 0x2A, 0x6B };

        protected const int P25_MSG_HDR_SIZE = 24;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the system name for this <see cref="FneSystemBase"/>.
        /// </summary>
        public string SystemName
        {
            get
            {
                if (fne != null)
                    return fne.SystemName;
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the peer ID for this <see cref="FneSystemBase"/>.
        /// </summary>
        public uint PeerId
        {
            get
            {
                if (fne != null)
                    return fne.PeerId;
                return uint.MaxValue;
            }
        }

        /// <summary>
        /// Flag indicating whether this <see cref="FneSystemBase"/> is running.
        /// </summary>
        public bool IsStarted
        { 
            get
            {
                if (fne != null)
                    return fne.IsStarted;
                return false;
            }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="FneSystemBase"/> class.
        /// </summary>
        /// <param name="fne">Instance of <see cref="FnePeer"/></param>
        /// <param name="fneLogLevel"></param>
        public FneSystemBase(FnePeer fne, LogLevel fneLogLevel = LogLevel.INFO)
        {
            this.fne = fne;

            // hook various FNE network callbacks
            this.fne.DMRDataValidate = DMRDataValidate;
            this.fne.DMRDataReceived += DMRDataReceived;

            this.fne.P25DataValidate = P25DataValidate;
            this.fne.P25DataPreprocess += P25DataPreprocess;
            this.fne.P25DataReceived += P25DataReceived;

            this.fne.NXDNDataValidate = NXDNDataValidate;
            this.fne.NXDNDataReceived += NXDNDataReceived;

            this.fne.PeerIgnored = PeerIgnored;
            this.fne.PeerConnected += PeerConnected;

            // hook logger callback
            this.fne.LogLevel = fneLogLevel;
        }

        /// <summary>
        /// Starts the main execution loop for this <see cref="FneSystemBase"/>.
        /// </summary>
        public virtual void Start()
        {
            if (!fne.IsStarted)
                fne.Start();
        }

        /// <summary>
        /// Stops the main execution loop for this <see cref="FneSystemBase"/>.
        /// </summary>
        public virtual void Stop()
        {
            if (fne.IsStarted)
                fne.Stop();
        }

        /// <summary>
        /// Callback used to validate incoming DMR data.
        /// </summary>
        /// <param name="peerId">Peer ID</param>
        /// <param name="srcId">Source Address</param>
        /// <param name="dstId">Destination Address</param>
        /// <param name="slot">Slot Number</param>
        /// <param name="callType">Call Type (Group or Private)</param>
        /// <param name="frameType">Frame Type</param>
        /// <param name="dataType">DMR Data Type</param>
        /// <param name="streamId">Stream ID</param>
        /// <param name="message">Raw message data</param>
        /// <returns>True, if data stream is valid, otherwise false.</returns>
        protected abstract bool DMRDataValidate(uint peerId, uint srcId, uint dstId, byte slot, CallType callType, FrameType frameType, DMRDataType dataType, uint streamId, byte[] message);

        /// <summary>
        /// Event handler used to process incoming DMR data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected abstract void DMRDataReceived(object sender, DMRDataReceivedEvent e);

        /// <summary>
        /// Callback used to validate incoming P25 data.
        /// </summary>
        /// <param name="peerId">Peer ID</param>
        /// <param name="srcId">Source Address</param>
        /// <param name="dstId">Destination Address</param>
        /// <param name="callType">Call Type (Group or Private)</param>
        /// <param name="duid">P25 DUID</param>
        /// <param name="frameType">Frame Type</param>
        /// <param name="streamId">Stream ID</param>
        /// <param name="message">Raw message data</param>
        /// <returns>True, if data stream is valid, otherwise false.</returns>
        protected abstract bool P25DataValidate(uint peerId, uint srcId, uint dstId, CallType callType, P25DUID duid, FrameType frameType, uint streamId, byte[] message);

        /// <summary>
        /// Event handler used to pre-process incoming P25 data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected abstract void P25DataPreprocess(object sender, P25DataReceivedEvent e);

        /// <summary>
        /// Event handler used to process incoming P25 data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected abstract void P25DataReceived(object sender, P25DataReceivedEvent e);

        /// <summary>
        /// Callback used to validate incoming NXDN data.
        /// </summary>
        /// <param name="peerId">Peer ID</param>
        /// <param name="srcId">Source Address</param>
        /// <param name="dstId">Destination Address</param>
        /// <param name="callType">Call Type (Group or Private)</param>
        /// <param name="messageType">NXDN Message Type</param>
        /// <param name="frameType">Frame Type</param>
        /// <param name="streamId">Stream ID</param>
        /// <param name="message">Raw message data</param>
        /// <returns>True, if data stream is valid, otherwise false.</returns>
        protected abstract bool NXDNDataValidate(uint peerId, uint srcId, uint dstId, CallType callType, NXDNMessageType messageType, FrameType frameType, uint streamId, byte[] message);

        /// <summary>
        /// Event handler used to process incoming NXDN data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected abstract void NXDNDataReceived(object sender, NXDNDataReceivedEvent e);

        /// <summary>
        /// Callback used to process whether or not a peer is being ignored for traffic.
        /// </summary>
        /// <param name="peerId">Peer ID</param>
        /// <param name="srcId">Source Address</param>
        /// <param name="dstId">Destination Address</param>
        /// <param name="slot">Slot Number</param>
        /// <param name="callType">Call Type (Group or Private)</param>
        /// <param name="frameType">Frame Type</param>
        /// <param name="dataType">DMR Data Type</param>
        /// <param name="streamId">Stream ID</param>
        /// <returns>True, if peer is ignored, otherwise false.</returns>
        protected abstract bool PeerIgnored(uint peerId, uint srcId, uint dstId, byte slot, CallType callType, FrameType frameType, DMRDataType dataType, uint streamId);

        /// <summary>
        /// Event handler used to handle a peer connected event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected abstract void PeerConnected(object sender, PeerConnectedEvent e);

        /// <summary>
        /// Creates an DMR frame message.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="callData"></param>
        /// <param name="n"></param>
        protected void CreateDMRMessage(ref byte[] data, RemoteCallData callData, byte seqNo, byte n)
        {
            FneUtils.StringToBytes(Constants.TAG_DMR_DATA, data, 0, Constants.TAG_DMR_DATA.Length);

            FneUtils.Write3Bytes(callData.SrcId, ref data, 5);                              // Source Address
            FneUtils.Write3Bytes(callData.DstId, ref data, 8);                              // Destination Address

            data[15U] = (byte)((callData.Slot == 1) ? 0x00 : 0x80);                         // Slot Number
            data[15U] |= 0x00;                                                              // Group

            if (callData.FrameType == FrameType.VOICE_SYNC)
                data[15U] |= 0x10;
            else if (callData.FrameType == FrameType.VOICE)
                data[15U] |= n;
            else
                data[15U] |= (byte)(0x20 | (byte)callData.FrameType);

            data[4U] = seqNo;
        }

        /// <summary>
        /// Helper to send a DMR terminator with LC message.
        /// </summary>
        /// <param name="callData"></param>
        /// <param name="seqNo"></param>
        /// <param name="dmrN"></param>
        /// <param name="embeddedData"></param>
        protected virtual void SendDMRTerminator(RemoteCallData callData, ref int seqNo, ref byte dmrN, EmbeddedData embeddedData)
        {
            byte n = (byte)((seqNo - 3U) % 6U);
            uint fill = 6U - n;

            FnePeer peer = (FnePeer)fne;
            ushort pktSeq = peer.pktSeq(true);

            byte[] data = null, dmrpkt = null;
            if (n > 0U)
            {
                for (uint i = 0U; i < fill; i++)
                {
                    // generate DMR AMBE data
                    data = new byte[DMR_FRAME_LENGTH_BYTES];
                    Buffer.BlockCopy(DMR_SILENCE_DATA, 0, data, 0, DMR_FRAME_LENGTH_BYTES);

                    byte lcss = embeddedData.GetData(ref data, n);

                    // generated embedded signalling
                    EMB emb = new EMB();
                    emb.ColorCode = 0;
                    emb.LCSS = lcss;
                    emb.Encode(ref data);

                    // generate DMR network frame
                    dmrpkt = new byte[DMR_PACKET_SIZE];
                    callData.FrameType = FrameType.DATA_SYNC;

                    CreateDMRMessage(ref dmrpkt, callData, (byte)seqNo, n);
                    Buffer.BlockCopy(data, 0, dmrpkt, 20, DMR_FRAME_LENGTH_BYTES);

                    peer.SendMaster(new Tuple<byte, byte>(Constants.NET_FUNC_PROTOCOL, Constants.NET_PROTOCOL_SUBFUNC_DMR), dmrpkt, pktSeq, callData.TxStreamID);

                    seqNo++;
                    dmrN++;
                }
            }

            data = new byte[DMR_FRAME_LENGTH_BYTES];

            // generate DMR LC
            LC dmrLC = new LC();
            dmrLC.FLCO = (byte)DMRFLCO.FLCO_GROUP;
            dmrLC.SrcId = callData.SrcId;
            dmrLC.DstId = callData.DstId;

            // generate the Slot TYpe
            SlotType slotType = new SlotType();
            slotType.DataType = (byte)DMRDataType.TERMINATOR_WITH_LC;
            slotType.GetData(ref data);

            FullLC.Encode(dmrLC, ref data, DMRDataType.TERMINATOR_WITH_LC);

            // generate DMR network frame
            dmrpkt = new byte[DMR_PACKET_SIZE];
            callData.FrameType = FrameType.DATA_SYNC;

            CreateDMRMessage(ref dmrpkt, callData, (byte)seqNo, 0);
            Buffer.BlockCopy(data, 0, dmrpkt, 20, DMR_FRAME_LENGTH_BYTES);

            peer.SendMaster(new Tuple<byte, byte>(Constants.NET_FUNC_PROTOCOL, Constants.NET_PROTOCOL_SUBFUNC_DMR), dmrpkt, pktSeq, callData.TxStreamID);

            seqNo = 0;
            dmrN = 0;
        }


        /// <summary>
        /// Creates an P25 frame message header.
        /// </summary>
        /// <param name="duid"></param>
        /// <param name="callData"></param>
        protected void CreateP25MessageHdr(byte duid, RemoteCallData callData, ref byte[] data)
        {
            FneUtils.StringToBytes(Constants.TAG_P25_DATA, data, 0, Constants.TAG_P25_DATA.Length);

            data[4U] = callData.LCO;                                                        // LCO

            FneUtils.Write3Bytes(callData.SrcId, ref data, 5);                              // Source Address
            FneUtils.Write3Bytes(callData.DstId, ref data, 8);                              // Destination Address

            data[11U] = 0;                                                                  // System ID
            data[12U] = 0;

            data[14U] = 0;                                                                  // Control Byte

            data[15U] = callData.MFId;                                                      // MFId

            data[16U] = 0;                                                                  // Network ID
            data[17U] = 0;
            data[18U] = 0;

            data[20U] = callData.LSD1;                                                      // LSD 1
            data[21U] = callData.LSD2;                                                      // LSD 2

            data[22U] = duid;                                                               // DUID

            data[180U] = 0;                                                                 // Frame Type
        }

        /// <summary>
        /// Helper to send a P25 TSDU message.
        /// </summary>
        /// <param name="tsbk"></param>
        public virtual void SendP25TSBK(RemoteCallData callData, byte[] tsbk)
        {
            if (tsbk.Length != P25Defines.P25_TSBK_LENGTH_BYTES)
                throw new InvalidOperationException($"TSBK length must be {P25Defines.P25_TSBK_LENGTH_BYTES}, passed length is {tsbk.Length}");

            Trellis trellis = new Trellis();

            byte[] payload = new byte[200];
            CreateP25MessageHdr((byte)P25DUID.TSDU, callData, ref payload);

            // pack raw P25 TSDU bytes
            byte[] tsbkTrellis = new byte[P25Defines.P25_TSBK_FEC_LENGTH_BYTES];
            trellis.Encode12(tsbk, ref tsbkTrellis);

            byte[] raw = new byte[P25Defines.P25_TSDU_FRAME_LENGTH_BYTES];
            P25Interleaver.Encode(tsbkTrellis, ref raw, 114, 318);

            Buffer.BlockCopy(raw, 0, payload, 24, raw.Length);
            payload[23U] = (byte)(P25_MSG_HDR_SIZE + raw.Length);

            FnePeer peer = (FnePeer)fne;
            ushort pktSeq = peer.pktSeq(true);
            peer.SendMaster(FneBase.CreateOpcode(Constants.NET_FUNC_PROTOCOL, Constants.NET_PROTOCOL_SUBFUNC_P25), payload, pktSeq, callData.TxStreamID);
        }

        /// <summary>
        /// Helper to send a P25 TDU message.
        /// </summary>
        /// <param name="callData"></param>
        /// <param name="grantDemand"></param>
        public virtual void SendP25TDU(RemoteCallData callData, bool grantDemand = false)
        {
            byte[] payload = new byte[200];
            CreateP25MessageHdr((byte)P25DUID.TDU, callData, ref payload);
            payload[23U] = P25_MSG_HDR_SIZE;

            // if this TDU is demanding a grant, set the grant demand control bit
            if (grantDemand)
                payload[14U] |= 0x80;

            FnePeer peer = (FnePeer)fne;
            ushort pktSeq = peer.pktSeq(true);
            peer.SendMaster(FneBase.CreateOpcode(Constants.NET_FUNC_PROTOCOL, Constants.NET_PROTOCOL_SUBFUNC_P25), payload, pktSeq, callData.TxStreamID);
        }
    } // public abstract class FneSystemBase
} // namespace fnecore
