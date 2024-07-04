// SPDX-License-Identifier: AGPL-3.0-only
/**
* Digital Voice Modem - Fixed Network Equipment Core Library
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / Fixed Network Equipment Core Library
* @license AGPLv3 License (https://opensource.org/licenses/AGPL-3.0)
*
*   Copyright (C) 2022-2023 Bryan Biedenkapp, N2PLL
*
*/

using System;
using System.Net;
using System.Security.Cryptography;

using fnecore.DMR;
using fnecore.P25;
using fnecore.NXDN;
using fnecore.EDAC;

namespace fnecore
{
    /// <summary>
    /// Structure containing detailed information about a connected peer.
    /// </summary>
    public class PeerDetails
    {
        /// <summary>
        /// Identity
        /// </summary>
        public string Identity;
        /// <summary>
        /// Receive Frequency
        /// </summary>
        public uint RxFrequency;
        /// <summary>
        /// Transmit Frequency
        /// </summary>
        public uint TxFrequency;

        /// <summary>
        /// Software Identifier
        /// </summary>
        public string Software;

        /*
        ** System Information
        */
        /// <summary>
        /// Latitude
        /// </summary>
        public double Latitude;
        /// <summary>
        /// Longitude
        /// </summary>
        public double Longitude;
        /// <summary>
        /// Height
        /// </summary>
        public int Height;
        /// <summary>
        /// Location
        /// </summary>
        public string Location;

        /*
        ** Channel Data
        */
        /// <summary>
        /// Transmit Offset (Mhz)
        /// </summary>
        public float TxOffsetMhz;
        /// <summary>
        /// Channel Bandwidth (Khz)
        /// </summary>
        public float ChBandwidthKhz;
        /// <summary>
        /// Channel ID
        /// </summary>
        public byte ChannelID;
        /// <summary>
        /// Channel Number
        /// </summary>
        public uint ChannelNo;
        /// <summary>
        /// Transmit Power
        /// </summary>
        public uint TxPower;

        /*
        ** REST API
        */
        /// <summary>
        /// REST API Password
        /// </summary>
        public string Password;
        /// <summary>
        /// REST API Port
        /// </summary>
        public int Port;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="PeerDetails"/> class.
        /// </summary>
        public PeerDetails()
        {
            /* stub */
        }
    } // public class PeerDetails

    /// <summary>
    /// Structure containing information about a connected peer.
    /// </summary>
    public class PeerInformation
    {
        /// <summary>
        /// Peer ID
        /// </summary>
        public uint PeerID;

        /// <summary>
        /// Stream ID
        /// </summary>
        public uint StreamID;

        /// <summary>
        /// RTP Packet Sequence
        /// </summary>
        public ushort PacketSequence;
        /// <summary>
        /// Next expected RTP Packet Sequence
        /// </summary>
        public ushort NextPacketSequence;

        /// <summary>
        /// Peer IP EndPoint
        /// </summary>
        public IPEndPoint EndPoint;

        /// <summary>
        /// Salt value used for authentication.
        /// </summary>
        public uint Salt;

        /// <summary>
        /// Connection State
        /// </summary>
        public ConnectionState State;

        /// <summary>
        /// Flag indicating peer is "connected".
        /// </summary>
        public bool Connection;

        /// <summary>
        /// Number of pings received.
        /// </summary>
        public int PingsReceived;
        /// <summary>
        /// Date/Time of last ping.
        /// </summary>
        public DateTime LastPing;

        /// <summary>
        /// Peer Details Structure
        /// </summary>
        public PeerDetails Details = null;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="PeerInformation"/> class.
        /// </summary>
        public PeerInformation()
        {
            Details = new PeerDetails();
        }
    } // public class PeerInformation

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
    public delegate bool DMRDataValidate(uint peerId, uint srcId, uint dstId, byte slot, CallType callType, FrameType frameType, DMRDataType dataType, uint streamId, byte[] message);
    /// <summary>
    /// Event used to process incoming DMR data.
    /// </summary>
    public class DMRDataReceivedEvent : EventArgs
    {
        /// <summary>
        /// Peer ID
        /// </summary>
        public uint PeerId { get; }
        /// <summary>
        /// Source Address
        /// </summary>
        public uint SrcId { get; }
        /// <summary>
        /// Destination Address
        /// </summary>
        public uint DstId { get; }
        /// <summary>
        /// Slot Number
        /// </summary>
        public byte Slot { get; }
        /// <summary>
        /// Call Type (Group or Private)
        /// </summary>
        public CallType CallType { get; }
        /// <summary>
        /// Frame Type
        /// </summary>
        public FrameType FrameType { get; }
        /// <summary>
        /// DMR Data Type
        /// </summary>
        public DMRDataType DataType { get; }
        /// <summary>
        /// 
        /// </summary>
        public byte n { get; }
        /// <summary>
        /// RTP Packet Sequence
        /// </summary>
        public ushort PacketSequence { get; }
        /// <summary>
        /// Stream ID
        /// </summary>
        public uint StreamId { get; }
        /// <summary>
        /// Raw message data
        /// </summary>
        public byte[] Data { get; }

        /*
        ** Methods
        */
        /// <summary>
        /// Initializes a new instance of the <see cref="DMRDataReceivedEvent"/> class.
        /// </summary>
        private DMRDataReceivedEvent()
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DMRDataReceivedEvent"/> class.
        /// </summary>
        /// <param name="peerId">Peer ID</param>
        /// <param name="srcId">Source Address</param>
        /// <param name="dstId">Destination Address</param>
        /// <param name="slot">Slot Number</param>
        /// <param name="callType">Call Type (Group or Private)</param>
        /// <param name="frameType">Frame Type</param>
        /// <param name="dataType">DMR Data Type</param>
        /// <param name="n"></param>
        /// <param name="pktSeq">RTP Packet Sequence</param>
        /// <param name="streamId">Stream ID</param>
        /// <param name="data">Raw message data</param>
        public DMRDataReceivedEvent(uint peerId, uint srcId, uint dstId, byte slot, CallType callType, FrameType frameType, DMRDataType dataType, byte n, ushort pktSeq, uint streamId, byte[] data) : base()
        {
            this.PeerId = peerId;
            this.SrcId = srcId;
            this.DstId = dstId;
            this.Slot = slot;
            this.CallType = callType;
            this.FrameType = frameType;
            this.DataType = dataType;
            this.n = n;
            this.PacketSequence = pktSeq;
            this.StreamId = streamId;

            this.Data = new byte[data.Length];
            Buffer.BlockCopy(data, 0, Data, 0, data.Length);
        }
    } // public class DMRDataReceivedEvent : EventArgs

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
    public delegate bool P25DataValidate(uint peerId, uint srcId, uint dstId, CallType callType, P25DUID duid, FrameType frameType, uint streamId, byte[] message);
    /// <summary>
    /// Event used to process incoming P25 data.
    /// </summary>
    public class P25DataReceivedEvent : EventArgs
    {
        /// <summary>
        /// Peer ID
        /// </summary>
        public uint PeerId { get; }
        /// <summary>
        /// Source Address
        /// </summary>
        public uint SrcId { get; }
        /// <summary>
        /// Destination Address
        /// </summary>
        public uint DstId { get; }
        /// <summary>
        /// Call Type (Group or Private)
        /// </summary>
        public CallType CallType { get; }
        /// <summary>
        /// P25 DUID
        /// </summary>
        public P25DUID DUID { get; }
        /// <summary>
        /// Frame Type
        /// </summary>
        public FrameType FrameType { get; }
        /// <summary>
        /// RTP Packet Sequence
        /// </summary>
        public ushort PacketSequence { get; }
        /// <summary>
        /// Stream ID
        /// </summary>
        public uint StreamId { get; }
        /// <summary>
        /// Raw message data
        /// </summary>
        public byte[] Data { get; }

        /*
        ** Methods
        */
        /// <summary>
        /// Initializes a new instance of the <see cref="P25DataReceivedEvent"/> class.
        /// </summary>
        private P25DataReceivedEvent()
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="P25DataPreprocessEvent"/> class.
        /// </summary>
        /// <param name="peerId">Peer ID</param>
        /// <param name="srcId">Source Address</param>
        /// <param name="dstId">Destination Address</param>
        /// <param name="callType">Call Type (Group or Private)</param>
        /// <param name="duid">P25 DUID</param>
        /// <param name="frameType">Frame Type</param>
        /// <param name="pktSeq">RTP Packet Sequence</param>
        /// <param name="streamId">Stream ID</param>
        /// <param name="data">Raw message data</param>
        public P25DataReceivedEvent(uint peerId, uint srcId, uint dstId, CallType callType, P25DUID duid, FrameType frameType, ushort pktSeq, uint streamId, byte[] data) : base()
        {
            this.PeerId = peerId;
            this.SrcId = srcId;
            this.DstId = dstId;
            this.CallType = callType;
            this.DUID = duid;
            this.FrameType = frameType;
            this.PacketSequence = pktSeq;
            this.StreamId = streamId;

            this.Data = new byte[data.Length];
            Buffer.BlockCopy(data, 0, Data, 0, data.Length);
        }
    } // public class P25DataReceivedEvent : EventArgs

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
    public delegate bool NXDNDataValidate(uint peerId, uint srcId, uint dstId, CallType callType, NXDNMessageType messageType, FrameType frameType, uint streamId, byte[] message);
    /// <summary>
    /// Event used to process incoming NXDN data.
    /// </summary>
    public class NXDNDataReceivedEvent : EventArgs
    {
        /// <summary>
        /// Peer ID
        /// </summary>
        public uint PeerId { get; }
        /// <summary>
        /// Source Address
        /// </summary>
        public uint SrcId { get; }
        /// <summary>
        /// Destination Address
        /// </summary>
        public uint DstId { get; }
        /// <summary>
        /// Call Type (Group or Private)
        /// </summary>
        public CallType CallType { get; }
        /// <summary>
        /// NXDN Message Type
        /// </summary>
        public NXDNMessageType MessageType { get; }
        /// <summary>
        /// Frame Type
        /// </summary>
        public FrameType FrameType { get; }
        /// <summary>
        /// RTP Packet Sequence
        /// </summary>
        public ushort PacketSequence { get; }
        /// <summary>
        /// Stream ID
        /// </summary>
        public uint StreamId { get; }
        /// <summary>
        /// Raw message data
        /// </summary>
        public byte[] Data { get; }

        /*
        ** Methods
        */
        /// <summary>
        /// Initializes a new instance of the <see cref="NXDNDataReceivedEvent"/> class.
        /// </summary>
        private NXDNDataReceivedEvent()
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NXDNDataReceivedEvent"/> class.
        /// </summary>
        /// <param name="peerId">Peer ID</param>
        /// <param name="srcId">Source Address</param>
        /// <param name="dstId">Destination Address</param>
        /// <param name="callType">Call Type (Group or Private)</param>
        /// <param name="messageType">NXDN Message Type</param>
        /// <param name="frameType">Frame Type</param>
        /// <param name="pktSeq">RTP Packet Sequence</param>
        /// <param name="streamId">Stream ID</param>
        /// <param name="data">Raw message data</param>
        public NXDNDataReceivedEvent(uint peerId, uint srcId, uint dstId, CallType callType, NXDNMessageType messageType, FrameType frameType, ushort pktSeq, uint streamId, byte[] data) : base()
        {
            this.PeerId = peerId;
            this.SrcId = srcId;
            this.DstId = dstId;
            this.CallType = callType;
            this.MessageType = messageType;
            this.FrameType = frameType;
            this.PacketSequence = pktSeq;
            this.StreamId = streamId;

            this.Data = new byte[data.Length];
            Buffer.BlockCopy(data, 0, Data, 0, data.Length);
        }
    } // public class NXDNDataReceivedEvent : EventArgs

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
    public delegate bool PeerIgnored(uint peerId, uint srcId, uint dstId, byte slot, CallType callType, FrameType frameType, DMRDataType dataType, uint streamId);
    /// <summary>
    /// Event when a peer connects.
    /// </summary>
    public class PeerConnectedEvent : EventArgs
    {
        /// <summary>
        /// Peer ID
        /// </summary>
        public uint PeerId { get; }
        /// <summary>
        /// Peer Information
        /// </summary>
        public PeerInformation Information { get; }

        /*
        ** Methods
        */
        /// <summary>
        /// Initializes a new instance of the <see cref="PeerConnectedEvent"/> class.
        /// </summary>
        /// <param name="peerId">Peer ID</param>
        /// <param name="peer">Peer Information</param>
        public PeerConnectedEvent(uint peerId, PeerInformation peer) : base()
        {
            this.PeerId = peerId;
            this.Information = peer;
        }
    } // public class PeerConnectedEvent : EventArgs

    /// <summary>
    /// This class implements some base functionality for all other FNE network classes.
    /// </summary>
    public abstract class FneBase
    {
        protected readonly string systemName = string.Empty;
        protected readonly uint peerId = 0;

        protected static Random rand = null;

        protected bool isStarted = false;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the system name for this <see cref="FneBase"/>.
        /// </summary>
        public string SystemName => systemName;

        /// <summary>
        /// Gets the peer ID for this <see cref="FneBase"/>.
        /// </summary>
        public uint PeerId => peerId;

        /// <summary>
        /// Flag indicating whether this <see cref="FneBase"/> is running.
        /// </summary>
        public bool IsStarted => isStarted;

        /// <summary>
        /// Gets/sets the interval that peers will need to ping the master.
        /// </summary>
        public int PingTime
        {
            get;
            set;
        }

        /// <summary>
        /// Get/sets the current logging level of the <see cref="FneBase"/> instance.
        /// </summary>
        public LogLevel LogLevel
        {
            get;
            set;
        }

        /// <summary>
        /// Get/sets a flag that enables dumping the raw recieved packets to the log.
        /// </summary>
        /// <remarks>This will also require the <see cref="FneBase.LogLevel"/> be set to DEBUG.</remarks>
        public bool RawPacketTrace
        {
            get;
            set;
        }

        /*
        ** Events/Callbacks
        */

        /// <summary>
        /// Callback action that handles validating a DMR call stream.
        /// </summary>
        public DMRDataValidate DMRDataValidate = null;
        /// <summary>
        /// Event action that handles processing a DMR call stream.
        /// </summary>
        public event EventHandler<DMRDataReceivedEvent> DMRDataReceived;

        /// <summary>
        /// Callback action that handles validating a P25 call stream.
        /// </summary>
        public P25DataValidate P25DataValidate = null;
        /// <summary>
        /// Event action that handles preprocessing a P25 call stream.
        /// </summary>
        public event EventHandler<P25DataReceivedEvent> P25DataPreprocess;
        /// <summary>
        /// Event action that handles processing a P25 call stream.
        /// </summary>
        public event EventHandler<P25DataReceivedEvent> P25DataReceived;

        /// <summary>
        /// Callback action that handles validating a NXDN call stream.
        /// </summary>
        public NXDNDataValidate NXDNDataValidate = null;
        /// <summary>
        /// Event action that handles processing a NXDN call stream.
        /// </summary>
        public event EventHandler<NXDNDataReceivedEvent> NXDNDataReceived;

        /// <summary>
        /// Callback action that handles verifying if a peer is ignored for a call stream.
        /// </summary>
        public PeerIgnored PeerIgnored = null;
        /// <summary>
        /// Event action that handles when a peer connects.
        /// </summary>
        public event EventHandler<PeerConnectedEvent> PeerConnected;
        /// <summary>
        /// Callback action that handles when a peer disconnects.
        /// </summary>
        public Action<uint> PeerDisconnected = null;

        /// <summary>
        /// Callback action that handles internal logging.
        /// </summary>
        public Action<LogLevel, string> Logger;

        /*
        ** Methods
        */

        /// <summary>
        /// Static initializer for the <see cref="FneMaster"/> class.
        /// </summary>
        static FneBase()
        {
            int seed = 0;
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] intBytes = new byte[4];
                rng.GetBytes(intBytes);
                seed = BitConverter.ToInt32(intBytes, 0);
            }

            rand = new Random(seed);
            rand.Next();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FneBase"/> class.
        /// </summary>
        /// <param name="systemName"></param>
        /// <param name="peerId"></param>
        protected FneBase(string systemName, uint peerId)
        {
            this.systemName = systemName;
            this.peerId = peerId;

            // set a default "noop" logger
            Logger = (LogLevel level, string message) => { };
        }

        /// <summary>
        /// Starts the main execution loop for this <see cref="FneBase"/>.
        /// </summary>
        public abstract void Start();

        /// <summary>
        /// Stops the main execution loop for this <see cref="FneBase"/>.
        /// </summary>
        public abstract void Stop();

        /// <summary>
        /// Helper to generate a new stream ID.
        /// </summary>
        /// <returns></returns>
        public static uint CreateStreamID()
        {
            return (uint)rand.Next(int.MinValue, int.MaxValue);
        }

        /// <summary>
        /// Helper to just quickly generate opcode tuples (mainly for brevity).
        /// </summary>
        /// <param name="func">Function</param>
        /// <param name="subFunc">Sub-Function</param>
        /// <returns></returns>
        public static Tuple<byte, byte> CreateOpcode(byte func, byte subFunc = Constants.NET_SUBFUNC_NOP)
        {
            return new Tuple<byte, byte>(func, subFunc);
        }

        /// <summary>
        /// Helper to fire the logging action.
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="message"></param>
        protected void Log(LogLevel logLevel, string message)
        {
            byte level = (byte)logLevel;
            if (level <= (byte)LogLevel)
                Logger(logLevel, message);
        }

        /// <summary>
        /// Helper to read and process a FNE RTP frame.
        /// </summary>
        /// <param name="frame">Raw UDP socket frame.</param>
        /// <param name="messageLength">Length of payload message.</param>
        /// <param name="rtpHeader">RTP Header.</param>
        /// <param name="fneHeader">RTP FNE Header.</param>
        protected byte[] ReadFrame(UdpFrame frame, out int messageLength, out RtpHeader rtpHeader, out RtpFNEHeader fneHeader)
        {
            int length = frame.Message.Length;
            messageLength = -1;
            rtpHeader = null;
            fneHeader = null;

            // read message from socket
            if (length > 0)
            {
                if (length < Constants.RtpHeaderLengthBytes + Constants.RtpExtensionHeaderLengthBytes)
                {
                    Log(LogLevel.ERROR, $"Message received from network is malformed! " +
                        $"{Constants.RtpHeaderLengthBytes + Constants.RtpExtensionHeaderLengthBytes} bytes != {frame.Message.Length} bytes");
                    return null;
                }

                // decode RTP header
                rtpHeader = new RtpHeader();
                if (!rtpHeader.Decode(frame.Message))
                {
                    Log(LogLevel.ERROR, $"Invalid RTP packet received from network");
                    return null;
                }

                // ensure the RTP header has extension header (otherwise abort)
                if (!rtpHeader.Extension)
                {
                    Log(LogLevel.ERROR, "Invalid RTP header received from network");
                    return null;
                }

                // ensure payload type is correct
                if ((rtpHeader.PayloadType != Constants.DVMRtpPayloadType) &&
                    (rtpHeader.PayloadType != Constants.DVMRtpPayloadType + 1))
                {
                    Log(LogLevel.ERROR, "Invalid RTP payload type received from network");
                    return null;
                }

                // decode FNE RTP header
                fneHeader = new RtpFNEHeader();
                if (!fneHeader.Decode(frame.Message))
                {
                    Log(LogLevel.ERROR, "Invalid RTP packet received from network");
                    return null;
                }

                // copy message
                messageLength = (int)fneHeader.MessageLength;
                byte[] message = new byte[messageLength];
                Buffer.BlockCopy(frame.Message, (int)(Constants.RtpHeaderLengthBytes + Constants.RtpExtensionHeaderLengthBytes + Constants.RtpFNEHeaderLengthBytes), 
                    message, 0, messageLength);

                ushort calc = CRC.CreateCRC16(message, (uint)(messageLength * 8));
                if (calc != fneHeader.CRC)
                {
                    Log(LogLevel.ERROR, "Failed CRC CCITT-162 check");
                    messageLength = -1;
                    return null;
                }

                return message;
            }

            return null;
        }

        /// <summary>
        /// Helper to generate and write a FNE RTP frame.
        /// </summary>
        /// <param name="message">Payload message.</param>
        /// <param name="peerId">Peer ID.</param>
        /// <param name="ssrc">Synchronization Source ID.</param>
        /// <param name="opcode">FNE Network Opcode.</param>
        /// <param name="pktSeq">RTP Packet Sequence.</param>
        /// <param name="streamId">Stream ID.</param>
        /// <returns></returns>
        protected byte[] WriteFrame(byte[] message, uint peerId, uint ssrc, Tuple<byte, byte> opcode, ushort pktSeq, uint streamId)
        {
            byte[] buffer = new byte[message.Length + Constants.RtpHeaderLengthBytes + Constants.RtpExtensionHeaderLengthBytes + Constants.RtpFNEHeaderLengthBytes];
            FneUtils.Memset(buffer, 0, buffer.Length);

            RtpHeader header = new RtpHeader();
            header.Extension = true;
            header.PayloadType = Constants.DVMRtpPayloadType;
            header.Sequence = pktSeq;
            header.SSRC = ssrc;

            header.Encode(ref buffer);

            RtpFNEHeader fneHeader = new RtpFNEHeader();
            fneHeader.CRC = CRC.CreateCRC16(message, (uint)(message.Length * 8));
            fneHeader.StreamID = streamId;
            fneHeader.PeerID = peerId;
            fneHeader.MessageLength = (uint)message.Length;

            fneHeader.Function = opcode.Item1;
            fneHeader.SubFunction = opcode.Item2;

            fneHeader.Encode(ref buffer);

            Buffer.BlockCopy(message, 0, buffer, (int)(Constants.RtpHeaderLengthBytes + Constants.RtpExtensionHeaderLengthBytes + Constants.RtpFNEHeaderLengthBytes),
                    message.Length);
            return buffer;
        }

        /// <summary>
        /// Helper to fire the DMR data received event.
        /// </summary>
        /// <param name="e"><see cref="DMRDataReceivedEvent"/> instance</param>
        protected void FireDMRDataReceived(DMRDataReceivedEvent e)
        {
            if (DMRDataReceived != null)
                DMRDataReceived.Invoke(this, e);
        }

        /// <summary>
        /// Helper to fire the P25 data pre-process event.
        /// </summary>
        /// <param name="e"><see cref="P25DataReceivedEvent"/> instance</param>
        protected void FireP25DataPreprocess(P25DataReceivedEvent e)
        {
            if (P25DataPreprocess != null)
                P25DataPreprocess.Invoke(this, e);
        }

        /// <summary>
        /// Helper to fire the P25 data received event.
        /// </summary>
        /// <param name="e"><see cref="P25DataReceivedEvent"/> instance</param>
        protected void FireP25DataReceived(P25DataReceivedEvent e)
        {
            if (P25DataReceived != null)
                P25DataReceived.Invoke(this, e);
        }

        /// <summary>
        /// Helper to fire the NXDN data received event.
        /// </summary>
        /// <param name="e"><see cref="NXDNDataReceivedEvent"/> instance</param>
        protected void FireNXDNDataReceived(NXDNDataReceivedEvent e)
        {
            if (NXDNDataReceived != null)
                NXDNDataReceived.Invoke(this, e);
        }

        /// <summary>
        /// Helper to fire the peer connected event.
        /// </summary>
        /// <param name="e"><see cref="PeerConnectedEvent"/> instance</param>
        protected void FirePeerConnected(PeerConnectedEvent e)
        {
            if (PeerConnected != null)
                PeerConnected.Invoke(this, e);
        }
    } // public abstract class FneBase
} // namespace fnecore
