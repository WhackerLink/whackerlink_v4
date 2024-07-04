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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;

using fnecore.DMR;
using fnecore.P25;
using fnecore.NXDN;

namespace fnecore
{
    /// <summary>
    /// Callback used to process a raw network frame.
    /// </summary>
    /// <param name="frame"><see cref="UdpFrame"/></param>
    /// <param name="peerId">Peer ID</param>
    /// <param name="streamId">Stream ID</param>
    /// <returns>True, if the frame was handled, otherwise false.</returns>
    public delegate bool RawNetworkFrame(UdpFrame frame, uint peerId, uint streamId);

    /// <summary>
    /// Implements an FNE "peer".
    /// </summary>
    public class FnePeer : FneBase
    {
        private const int MAX_MISSED_PEER_PINGS = 5;

        private UdpReceiver client = null;

        private bool abortListening = false;

        private CancellationTokenSource listenCancelToken = new CancellationTokenSource();
        private Task listenTask = null;
        private CancellationTokenSource maintainenceCancelToken = new CancellationTokenSource();
        private Task maintainenceTask = null;

        private PeerInformation info;
        private IPEndPoint masterEndpoint = null;

        private ushort currPktSeq = 0;
        private uint streamId = 0;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets/sets the peer information.
        /// </summary>
        public PeerInformation Information
        {
            get { return info; }
            set { info = value; }
        }

        /// <summary>
        /// Gets/sets the password used for connecting to a master.
        /// </summary>
        public string Passphrase
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the number of pings sent.
        /// </summary>
        public int PingsSent
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the number of pings acked.
        /// </summary>
        public int PingsAcked
        {
            get;
            private set;
        }

        /*
        ** Events/Callbacks
        */

        /// <summary>
        /// Event action that handles a raw network frame directly.
        /// </summary>
        public RawNetworkFrame NetworkFrameHandler = null;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="FnePeer"/> class.
        /// </summary>
        /// <param name="systemName"></param>
        /// <param name="peerId"></param>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public FnePeer(string systemName, uint peerId, string address, int port) : this(systemName, peerId, new IPEndPoint(IPAddress.Parse(address), port))
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FnePeer"/> class.
        /// </summary>
        /// <param name="systemName"></param>
        /// <param name="peerId"></param>
        /// <param name="endpoint"></param>
        public FnePeer(string systemName, uint peerId, IPEndPoint endpoint) : base(systemName, peerId)
        {
            masterEndpoint = endpoint;
            client = new UdpReceiver();

            info = new PeerInformation();
            info.PeerID = peerId;
            info.Connection = false;

            PingsAcked = 0;
        }

        /// <summary>
        /// Starts the main execution loop for this <see cref="FnePeer"/>.
        /// </summary>
        public override void Start()
        {
            if (isStarted)
                throw new InvalidOperationException("Cannot start listening when already started.");

            Logger(LogLevel.INFO, $"({systemName}) starting network services, {masterEndpoint}");

            // attempt initial connection
            try
            {
                client.Connect(masterEndpoint);
            }
            catch (SocketException se)
            {
                Log(LogLevel.FATAL, $"({systemName}) SOCKET ERROR: {se.SocketErrorCode}; {se.Message}");
            }

            abortListening = false;
            listenTask = Task.Factory.StartNew(Listen, listenCancelToken.Token);
            maintainenceTask = Task.Factory.StartNew(Maintainence, maintainenceCancelToken.Token);

            isStarted = true;
        }

        /// <summary>
        /// Stops the main execution loop for this <see cref="FnePeer"/>.
        /// </summary>
        public override void Stop()
        {
            if (!isStarted)
                throw new InvalidOperationException("Cannot stop listening when not started.");

            Logger(LogLevel.INFO, $"({systemName}) stopping network services, {masterEndpoint}");
            
            // stop UDP listen task
            if (listenTask != null)
            {
                abortListening = true;
                listenCancelToken.Cancel();

                try
                {
                    listenTask.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException) { /* stub */ }
                finally
                {
                    listenCancelToken.Dispose();
                }
            }

            // stop maintainence task
            if (maintainenceTask != null)
            {
                maintainenceCancelToken.Cancel();

                try
                {
                    maintainenceTask.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException) { /* stub */ }
                finally
                {
                    maintainenceCancelToken.Dispose();
                }
            }

            isStarted = false;
        }

        /// <summary>
        /// Helper to send a raw UDP frame.
        /// </summary>
        /// <param name="frame">UDP frame to send</param>
        public void Send(UdpFrame frame)
        {
            if (RawPacketTrace)
                Log(LogLevel.DEBUG, $"({systemName}) Network Sent (to {frame.Endpoint}) -- {FneUtils.HexDump(frame.Message, 0)}");

            client.Send(frame);
        }

        /// <summary>
        /// Helper to send a data message to the master.
        /// </summary>
        /// <param name="opcode">Opcode</param>
        /// <param name="message">Byte array containing message to send</param>
        /// <param name="pktSeq">RTP Packet Sequence</param>
        /// <param name="streamId"></param>
        /// <param name="forceZeroStream"></param>
        public void SendMaster(Tuple<byte, byte> opcode, byte[] message, ushort pktSeq, uint streamId = 0, bool forceZeroStream = false)
        {
            if (streamId == 0 && !forceZeroStream)
                streamId = this.streamId;

            Send(new UdpFrame()
            {
                Endpoint = masterEndpoint,
                Message = WriteFrame(message, peerId, this.peerId, opcode, pktSeq, streamId)
            });
        }

        /// <summary>
        /// Helper to send a data message to the master.
        /// </summary>
        /// <param name="opcode">Opcode</param>
        /// <param name="message">Byte array containing message to send</param>
        public void SendMaster(Tuple<byte, byte> opcode, byte[] message)
        {
            SendMaster(opcode, message, pktSeq());
        }

        /// <summary>
        /// Helper to send group affiliation announcements to the master.
        /// </summary>
        /// <param name="srcId"></param>
        /// <param name="dstId"></param>
        public void SendMasterGroupAffiliation(uint srcId, uint dstId)
        {
            // send message to master
            byte[] res = new byte[6];

            FneUtils.Write3Bytes(srcId, ref res, 0);
            FneUtils.Write3Bytes(dstId, ref res, 3);

            SendMaster(CreateOpcode(Constants.NET_FUNC_TRANSFER, Constants.NET_ANNC_SUBFUNC_GRP_AFFIL), res, 0, 0, true);
        }

        /// <summary>
        /// Helper to send unit registration announcements to the master.
        /// </summary>
        /// <param name="srcId"></param>
        public void SendMasterUnitRegistration(uint srcId)
        {
            // send message to master
            byte[] res = new byte[3];

            FneUtils.Write3Bytes(srcId, ref res, 0);

            SendMaster(CreateOpcode(Constants.NET_FUNC_TRANSFER, Constants.NET_ANNC_SUBFUNC_UNIT_REG), res, 0, 0, true);
        }

        /// <summary>
        /// Helper to send unit deregistration announcements to the master.
        /// </summary>
        /// <param name="srcId"></param>
        public void SendMasterUnitDeRegistration(uint srcId)
        {
            // send message to master
            byte[] res = new byte[3];

            FneUtils.Write3Bytes(srcId, ref res, 0);

            SendMaster(CreateOpcode(Constants.NET_FUNC_TRANSFER, Constants.NET_ANNC_SUBFUNC_UNIT_DEREG), res, 0, 0, true);
        }

        /// <summary>
        /// Helper to update the RTP packet sequence.
        /// </summary>
        /// <param name="reset"></param>
        /// <returns>RTP packet sequence.</returns>
        public ushort pktSeq(bool reset = false)
        {
            if (reset)
            {
                currPktSeq = 0;
                return currPktSeq;
            }

            ushort curr = currPktSeq;
            ++currPktSeq;
            if (currPktSeq > (Constants.RtpCallEndSeq - 1))
                currPktSeq = 0;

            return curr;
        }

        /// <summary>
        /// Internal UDP listen routine.
        /// </summary>
        private async void Listen()
        {
            CancellationToken ct = listenCancelToken.Token;
            ct.ThrowIfCancellationRequested();

            while (!abortListening)
            {
                try
                {
                    UdpFrame frame = await client.Receive();
                    if (RawPacketTrace)
                        Log(LogLevel.DEBUG, $"Network Received (from {frame.Endpoint}) -- {FneUtils.HexDump(frame.Message, 0)}");

                    // decode RTP frame
                    if (frame.Message.Length <= 0)
                        continue;

                    RtpHeader rtpHeader;
                    RtpFNEHeader fneHeader;
                    int messageLength = 0;
                    byte[] message = ReadFrame(frame, out messageLength, out rtpHeader, out fneHeader);
                    if (message == null)
                    {
                        Log(LogLevel.ERROR, $"({systemName}) Malformed packet (from {frame.Endpoint}); failed to decode RTP frame");
                        continue;
                    }

                    if (message.Length < 1)
                    {
                        Log(LogLevel.WARNING, $"({systemName}) Malformed packet (from {frame.Endpoint}) -- {FneUtils.HexDump(message, 0)}");
                        continue;
                    }

                    // validate frame endpoint
                    if (frame.Endpoint.ToString() == masterEndpoint.ToString())
                    {
                        uint peerId = fneHeader.PeerID;

                        if (streamId != fneHeader.StreamID)
                            pktSeq(true);

                        // update current peer stream ID
                        streamId = fneHeader.StreamID;

                        // see if the peer is defining its own frame handler, if it is try to handle the frame there
                        if (NetworkFrameHandler != null)
                        {
                            if (NetworkFrameHandler(frame, peerId, streamId))
                                continue;
                        }

                        // process incoming message frame opcodes
                        switch (fneHeader.Function)
                        {
                            case Constants.NET_FUNC_PROTOCOL:
                                {
                                    if (fneHeader.SubFunction == Constants.NET_PROTOCOL_SUBFUNC_DMR)        // Encapsulated DMR data frame
                                    {
                                        if (peerId != this.peerId)
                                        {
                                            //Log(LogLevel.WARNING, $"({systemName}) PEER {peerId}; routed traffic, rewriting PEER {this.peerId}");
                                            peerId = this.peerId;
                                        }

                                        // is this for our peer?
                                        if (peerId == this.peerId)
                                        {
                                            byte seqNo = message[4];
                                            uint srcId = FneUtils.Bytes3ToUInt32(message, 5);
                                            uint dstId = FneUtils.Bytes3ToUInt32(message, 8);

                                            byte bits = message[15];
                                            byte slot = (byte)(((bits & 0x80) == 0x80) ? 1 : 0);
                                            CallType callType = ((bits & 0x40) == 0x40) ? CallType.PRIVATE : CallType.GROUP;
                                            FrameType frameType = (FrameType)((bits & 0x30) >> 4);

                                            DMRDataType dataType = DMRDataType.IDLE;
                                            if ((bits & 0x20) == 0x20)
                                                dataType = (DMRDataType)(bits & ~(0x20));

                                            byte n = (byte)(bits & 0xF);
#if DEBUG
                                            Log(LogLevel.DEBUG, $"{systemName} DMRD: SRC_PEER {peerId} SRC_ID {srcId} DST_ID {dstId} TS {slot} [STREAM ID {streamId}]");
#endif
                                            // perform any userland actions with the data
                                            FireDMRDataReceived(new DMRDataReceivedEvent(peerId, srcId, dstId, slot, callType, frameType, dataType, n, rtpHeader.Sequence, streamId, message));
                                        }
                                    }
                                    else if (fneHeader.SubFunction == Constants.NET_PROTOCOL_SUBFUNC_P25)   // Encapsulated P25 data frame
                                    {
                                        if (peerId != this.peerId)
                                        {
                                            //Log(LogLevel.WARNING, $"({systemName}) PEER {peerId}; routed traffic, rewriting PEER {this.peerId}");
                                            peerId = this.peerId;
                                        }

                                        // is this for our peer?
                                        if (peerId == this.peerId)
                                        {
                                            uint srcId = FneUtils.Bytes3ToUInt32(message, 5);
                                            uint dstId = FneUtils.Bytes3ToUInt32(message, 8);
                                            CallType callType = (message[4] == P25Defines.LC_PRIVATE) ? CallType.PRIVATE : CallType.GROUP;
                                            P25DUID duid = (P25DUID)message[22];
                                            FrameType frameType = ((duid != P25DUID.TDU) && (duid != P25DUID.TDULC)) ? FrameType.VOICE : FrameType.TERMINATOR;
#if DEBUG
                                            Log(LogLevel.DEBUG, $"{systemName} P25D: SRC_PEER {peerId} SRC_ID {srcId} DST_ID {dstId} [STREAM ID {streamId}]");
#endif
                                            // perform any userland actions with the data
                                            FireP25DataReceived(new P25DataReceivedEvent(peerId, srcId, dstId, callType, duid, frameType, rtpHeader.Sequence, streamId, message));
                                        }
                                    }
                                    else if (fneHeader.SubFunction == Constants.NET_PROTOCOL_SUBFUNC_NXDN)  // Encapsulated NXDN data frame
                                    {
                                        if (peerId != this.peerId)
                                        {
                                            //Log(LogLevel.WARNING, $"({systemName}) PEER {peerId}; routed traffic, rewriting PEER {this.peerId}");
                                            peerId = this.peerId;
                                        }

                                        // is this for our peer?
                                        if (peerId == this.peerId)
                                        {
                                            NXDNMessageType messageType = (NXDNMessageType)message[4];
                                            uint srcId = FneUtils.Bytes3ToUInt32(message, 5);
                                            uint dstId = FneUtils.Bytes3ToUInt32(message, 8);

                                            byte bits = message[15];
                                            CallType callType = ((bits & 0x40) == 0x40) ? CallType.PRIVATE : CallType.GROUP;
                                            FrameType frameType = (messageType != NXDNMessageType.MESSAGE_TYPE_TX_REL) ? FrameType.VOICE : FrameType.TERMINATOR;
#if DEBUG
                                            Log(LogLevel.DEBUG, $"{systemName} NXDD: SRC_PEER {peerId} SRC_ID {srcId} DST_ID {dstId} [STREAM ID {streamId}]");
#endif
                                            // perform any userland actions with the data
                                            FireNXDNDataReceived(new NXDNDataReceivedEvent(peerId, srcId, dstId, callType, messageType, frameType, rtpHeader.Sequence, streamId, message));
                                        }
                                    }
                                    else
                                    {
                                        Log(LogLevel.ERROR, $"({systemName}) Unknown protocol opcode {FneUtils.BytesToString(message, 0, 4)} -- {FneUtils.HexDump(message, 0)}");
                                    }
                                }
                                break;

                            case Constants.NET_FUNC_MASTER:
                                {
                                    /* stub */
                                }
                                break;

                            case Constants.NET_FUNC_NAK:                                                    // Master NAK
                                {
                                    if (this.peerId == peerId)
                                    {
                                        info.Connection = false;
                                        Log(LogLevel.DEBUG, $"({systemName}) PEER {this.peerId} MSTNAK received");
                                    }
                                }
                                break;
                            case Constants.NET_FUNC_ACK:                                                    // Repeater ACK
                                {
                                    if (info.State == ConnectionState.WAITING_LOGIN)                        // Repeater Login
                                    {
                                        uint salt = FneUtils.ToUInt32(message, 6);
                                        Log(LogLevel.INFO, $"({systemName}) PEER {this.peerId} login ACK received with ID {salt}");

                                        info.Salt = salt;

                                        // calculate our own hash
                                        byte[] inBuf = new byte[4 + Passphrase.Length];
                                        FneUtils.WriteBytes(info.Salt, ref inBuf, 0);
                                        FneUtils.StringToBytes(Passphrase, inBuf, 4, Passphrase.Length);
                                        byte[] calcHash = FneUtils.sha256_hash(inBuf);

                                        // send message to master
                                        byte[] res = new byte[calcHash.Length + 8];
                                        FneUtils.StringToBytes(Constants.TAG_REPEATER_AUTH, res, 0, 4);
                                        FneUtils.WriteBytes(peerId, ref res, 4);
                                        Buffer.BlockCopy(calcHash, 0, res, 8, calcHash.Length);
                                        SendMaster(CreateOpcode(Constants.NET_FUNC_RPTK), res);

                                        info.State = ConnectionState.WAITING_AUTHORISATION;
                                    }
                                    else if (info.State == ConnectionState.WAITING_AUTHORISATION)           // Repeater Authorization
                                    {
                                        if (this.peerId == peerId)
                                        {
                                            string json = string.Empty;
                                            using (MemoryStream stream = new MemoryStream())
                                            {
                                                using (Utf8JsonWriter jsonWriter = new Utf8JsonWriter(stream))
                                                {
                                                    jsonWriter.WriteStartObject();

                                                    // identity
                                                    jsonWriter.WriteString("identity", info.Details.Identity);
                                                    jsonWriter.WriteNumber("rxFrequency", info.Details.RxFrequency);
                                                    jsonWriter.WriteNumber("txFrequency", info.Details.TxFrequency);

                                                    // system info
                                                    {
                                                        jsonWriter.WritePropertyName("info");
                                                        jsonWriter.WriteStartObject();

                                                        jsonWriter.WriteNumber("latitude", info.Details.Latitude);
                                                        jsonWriter.WriteNumber("longitude", info.Details.Longitude);
                                                        jsonWriter.WriteNumber("height", info.Details.Height);
                                                        jsonWriter.WriteString("location", info.Details.Location);

                                                        jsonWriter.WriteEndObject();
                                                    }

                                                    // channel data
                                                    {
                                                        jsonWriter.WritePropertyName("channel");
                                                        jsonWriter.WriteStartObject();

                                                        jsonWriter.WriteNumber("txPower", info.Details.TxPower);
                                                        jsonWriter.WriteNumber("txOffsetMhz", (double)info.Details.TxOffsetMhz);
                                                        jsonWriter.WriteNumber("chBandwidthKhz", (double)info.Details.ChBandwidthKhz);
                                                        jsonWriter.WriteNumber("channelId", info.Details.ChannelID);
                                                        jsonWriter.WriteNumber("channelNo", info.Details.ChannelNo);

                                                        jsonWriter.WriteEndObject();
                                                    }

                                                    // RCON
                                                    {
                                                        jsonWriter.WritePropertyName("rcon");
                                                        jsonWriter.WriteStartObject();

                                                        jsonWriter.WriteString("password", info.Details.Password);
                                                        jsonWriter.WriteNumber("port", info.Details.Port);

                                                        jsonWriter.WriteEndObject();
                                                    }

                                                    jsonWriter.WriteString("software", info.Details.Software);

                                                    jsonWriter.WriteEndObject();
                                                }

                                                json = Encoding.UTF8.GetString(stream.ToArray());
                                            }

                                            // send message to master
                                            byte[] res = new byte[json.Length + 8];
                                            FneUtils.StringToBytes(Constants.TAG_REPEATER_CONFIG, res, 0, 4);
                                            FneUtils.WriteBytes(peerId, ref res, 4);
                                            FneUtils.StringToBytes(json, res, 8, json.Length);
                                            SendMaster(CreateOpcode(Constants.NET_FUNC_RPTC), res);

                                            info.State = ConnectionState.WAITING_CONFIG;
                                        }
                                        else
                                        {
                                            info.State = ConnectionState.WAITING_LOGIN;
                                            info.Connection = false;
                                            Log(LogLevel.ERROR, $"({systemName}) PEER {this.peerId} master ACK contained wrong ID - connection reset");
                                        }
                                    }
                                    else if (info.State == ConnectionState.WAITING_CONFIG)                  // Repeater Configuration
                                    {
                                        if (this.peerId == peerId)
                                        {
                                            info.Connection = true;
                                            info.State = ConnectionState.RUNNING;
                                            Log(LogLevel.INFO, $"({systemName}) PEER {this.peerId} connection to MASTER completed");

                                            // userland actions
                                            FirePeerConnected(new PeerConnectedEvent(peerId, info));
                                        }
                                        else
                                        {
                                            info.State = ConnectionState.WAITING_LOGIN;
                                            info.Connection = false;
                                            Log(LogLevel.ERROR, $"({systemName}) PEER {this.peerId} master ACK contained wrong ID - connection reset");
                                        }
                                    }
                                }
                                break;
                            case Constants.NET_FUNC_MST_CLOSING:                                            // Master Closing (Disconnect)
                                {
                                    if (this.peerId == peerId)
                                    {
                                        info.Connection = false;
                                        Log(LogLevel.DEBUG, $"({systemName}) PEER {this.peerId} MSTCL received");

                                        // userland actions
                                        if (PeerDisconnected != null)
                                            PeerDisconnected(peerId);
                                    }
                                }
                                break;
                            case Constants.NET_FUNC_PONG:                                                   // Master Ping Response
                                {
                                    if (this.peerId == peerId)
                                    {
                                        PingsAcked++;
                                        Log(LogLevel.DEBUG, $"({systemName}) PEER {this.peerId} MSTPONG received, pongs since connected {PingsAcked}");
                                    }
                                }
                                break;

                            default:
                                Log(LogLevel.ERROR, $"({systemName}) Unknown opcode {FneUtils.BytesToString(message, 0, 4)} -- {FneUtils.HexDump(message, 0)}");
                                break;
                        }
                    }
                }
                catch (InvalidOperationException)
                {
                    Log(LogLevel.ERROR, $"({systemName}) Not connected or lost connection to {masterEndpoint}; reconnecting...");
                    client.Connect(masterEndpoint);
                }
                catch (SocketException se)
                {
                    // what kind of socket error do we have?
                    switch (se.SocketErrorCode)
                    {
                        case SocketError.NotConnected:
                        case SocketError.ConnectionReset:
                        case SocketError.ConnectionAborted:
                        case SocketError.ConnectionRefused:
                            Log(LogLevel.ERROR, $"({systemName}) Not connected or lost connection to {masterEndpoint}; reconnecting...");
                            client.Connect(masterEndpoint);
                            break;
                        default:
                            Log(LogLevel.FATAL, $"({systemName}) SOCKET ERROR: {se.SocketErrorCode}; {se.Message}");
                            break;
                    }
                }

                if (ct.IsCancellationRequested)
                    abortListening = true;
            }
        }

        /// <summary>
        /// Internal maintainence routine.
        /// </summary>
        private async void Maintainence()
        {
            CancellationToken ct = maintainenceCancelToken.Token;
            while (!abortListening)
            {
                try
                {
                    // if we're not connected, zero out the connection stats and send a login request to the master
                    if (!info.Connection || info.State == ConnectionState.WAITING_LOGIN)
                    {
                        PingsSent = 0;
                        PingsAcked = 0;
                        info.State = ConnectionState.WAITING_LOGIN;

                        // send message to master
                        byte[] res = new byte[8];
                        FneUtils.StringToBytes(Constants.TAG_REPEATER_LOGIN, res, 0, 4);
                        FneUtils.WriteBytes(peerId, ref res, 4);
                        SendMaster(CreateOpcode(Constants.NET_FUNC_RPTL), res);

                        Log(LogLevel.INFO, $"({systemName}) Sending login request to MASTER {masterEndpoint}");
                    }

                    // if we are connected, sent a ping to the master and increment the counter
                    if (info.Connection && info.State == ConnectionState.RUNNING)
                    {
                        if (PingsSent > (PingsAcked + MAX_MISSED_PEER_PINGS))
                        {
                            Log(LogLevel.WARNING, $"({systemName} Peer connection lost to {masterEndpoint}; reconnecting...");

                            PingsSent = 0;
                            PingsAcked = 0;
                            info.State = ConnectionState.WAITING_LOGIN;

                            client.Connect(masterEndpoint);
                        }
                        else
                        {
                            // send message to master
                            byte[] res = new byte[1];
                            SendMaster(CreateOpcode(Constants.NET_FUNC_PING), res);

                            PingsSent++;
                            Log(LogLevel.DEBUG, $"({systemName}) RPTPING sent to MASTER {masterEndpoint}; pings since connected {PingsSent}");
                        }
                    }
                }
                catch (InvalidOperationException)
                {
                    Log(LogLevel.ERROR, $"({systemName}) Not connected or lost connection to {masterEndpoint}; reconnecting...");
                    client.Connect(masterEndpoint);
                }
                catch (SocketException se)
                {
                    // what kind of socket error do we have?
                    switch (se.SocketErrorCode)
                    {
                        case SocketError.NotConnected:
                        case SocketError.ConnectionReset:
                        case SocketError.ConnectionAborted:
                        case SocketError.ConnectionRefused:
                            Log(LogLevel.ERROR, $"({systemName}) Not connected or lost connection to {masterEndpoint}; reconnecting...");
                            client.Connect(masterEndpoint);
                            break;
                        default:
                            Log(LogLevel.FATAL, $"({systemName}) SOCKET ERROR: {se.SocketErrorCode}; {se.Message}");
                            abortListening = true;
                            break;
                    }
                }

                try
                {
                    await Task.Delay(PingTime * 1000, ct);
                }
                catch (TaskCanceledException) { /* stub */ }
            }
        }
    } // public class FnePeer
} // namespace fnecore
