// SPDX-License-Identifier: AGPL-3.0-only
/**
* Digital Voice Modem - Audio Bridge
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / Audio Bridge
* @license AGPLv3 License (https://opensource.org/licenses/AGPL-3.0)
*
*   Copyright (C) 2022-2024 Bryan Biedenkapp, N2PLL
*   Copyright (C) 2024 Caleb, KO4UYJ
*
*/

using System.Diagnostics;
using Serilog;

using fnecore;
using fnecore.DMR;

using vocoder;

using WhackerLinkCommonLib.Interfaces;
using WhackerLinkCommonLib.Handlers;
using WhackerLinkCommonLib.Models;
using WhackerLinkCommonLib.Models.IOSP;
using WhackerLinkCommonLib.Utils;

namespace WhackerLink2Dvm
{
    /// <summary>
    /// Represents the individual timeslot data status.
    /// </summary>
    public class SlotStatus
    {
        /// <summary>
        /// Rx Start Time
        /// </summary>
        public DateTime RxStart = DateTime.Now;

        /// <summary>
        /// 
        /// </summary>
        public uint RxSeq = 0;

        /// <summary>
        /// Rx RF Source
        /// </summary>
        public uint RxRFS = 0;
        /// <summary>
        /// Tx RF Source
        /// </summary>
        public uint TxRFS = 0;

        /// <summary>
        /// Rx Stream ID
        /// </summary>
        public uint RxStreamId = 0;
        /// <summary>
        /// Tx Stream ID
        /// </summary>
        public uint TxStreamId = 0;

        /// <summary>
        /// Rx TG ID
        /// </summary>
        public uint RxTGId = 0;
        /// <summary>
        /// Tx TG ID
        /// </summary>
        public uint TxTGId = 0;
        /// <summary>
        /// Tx Privacy TG ID
        /// </summary>
        public uint TxPITGId = 0;

        /// <summary>
        /// Rx Time
        /// </summary>
        public DateTime RxTime = DateTime.Now;
        /// <summary>
        /// Tx Time
        /// </summary>
        public DateTime TxTime = DateTime.Now;

        /// <summary>
        /// Rx Type
        /// </summary>
        public FrameType RxType = FrameType.TERMINATOR;

    } // public class SlotStatus

    /// <summary>
    /// Implements a FNE system.
    /// </summary>
    public abstract partial class FneSystemBase : fnecore.FneSystemBase
    {
        private const string LOCAL_CALL = "Local Traffic";
        private const string UDP_CALL = "UDP Traffic";

        private const int P25_FIXED_SLOT = 2;

        public const int SAMPLE_RATE = 8000;
        public const int BITS_PER_SECOND = 16;

        private const int MBE_SAMPLES_LENGTH = 160;

        private bool callInProgress = false;

        private SlotStatus[] status;

        private Stopwatch dropAudio;
        private int dropTimeMs;
        bool audioDetect;
        bool trafficFromUdp;

        private Random rand;
        private uint txStreamId;

        private IWebSocketHandler webSocketHandler;

        private uint srcIdOverride = 0;
        private uint udpSrcId = 0;
        private uint udpDstId = 0;


        private VoiceChannel voiceChannel;
        Dictionary<uint, uint> affiliations = new Dictionary<uint, uint>();

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="FneSystemBase"/> class.
        /// </summary>
        /// <param name="fne">Instance of <see cref="FneMaster"/> or <see cref="FnePeer"/></param>
        public FneSystemBase(FnePeer fne) : base(fne, LogLevel.INFO)
        {
            this.fne = fne;

            this.rand = new Random(Guid.NewGuid().GetHashCode());

            webSocketHandler = new WebSocketHandler();
            webSocketHandler.Connect(WhackerLink2Dvm.config.WhackerLink.Address, WhackerLink2Dvm.config.WhackerLink.Port);
            webSocketHandler.OnAudioData += WhackerLinkDataReceived;
            webSocketHandler.OnVoiceChannelResponse += WhackerLinkVoiceChannelResponse;

            // initialize slot statuses
            this.status = new SlotStatus[3];
            this.status[0] = new SlotStatus();  // DMR Slot 1
            this.status[1] = new SlotStatus();  // DMR Slot 2
            this.status[2] = new SlotStatus();  // P25

            // hook logger callback
            this.fne.Logger = (LogLevel level, string message) =>
            {
                switch (level)
                {
                    case LogLevel.WARNING:
                        WhackerLink2Dvm.logger.Warning(message);
                        break;
                    case LogLevel.ERROR:
                        WhackerLink2Dvm.logger.Error(message);
                        break;
                    case LogLevel.DEBUG:
                        WhackerLink2Dvm.logger.Debug(message);
                        break;
                    case LogLevel.FATAL:
                        WhackerLink2Dvm.logger.Fatal(message);
                        break;
                    case LogLevel.INFO:
                    default:
                        WhackerLink2Dvm.logger.Information(message);
                        break;
                }
            };

            this.dropAudio = new Stopwatch();
            this.dropTimeMs = WhackerLink2Dvm.config.DropTimeMs;

            // "stuck" call (improperly ended call) checker thread
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    string trafficType = LOCAL_CALL;
                    if (trafficFromUdp)
                        trafficType = UDP_CALL;

                    // if we've exceeded the audio drop timeout, then really drop the audio
                    if ((dropAudio.IsRunning && (dropAudio.ElapsedMilliseconds > dropTimeMs * 2)) ||
                        (!dropAudio.IsRunning && !audioDetect && callInProgress))
                    {
                        if (audioDetect)
                        {
                            WhackerLink2Dvm.logger.Information($"({SystemName}) {trafficType} *CALL END (S)   * PEER {fne.PeerId} [STREAM ID {txStreamId}]");

                            audioDetect = false;
                            dropAudio.Reset();
#if !ENCODER_LOOPBACK_TEST
                            if (!callInProgress)
                            {
                                SendP25TDU();
                                break;
                            }
#endif
                            srcIdOverride = 0;
                            txStreamId = 0;

                            dropTimeMs = WhackerLink2Dvm.config.DropTimeMs;

                            udpSrcId = 0;
                            udpDstId = 0;
                            trafficFromUdp = false;
                        }
                    }

                    Thread.Sleep(5);
                }
            });

            this.audioDetect = false;

            // initialize P25 vocoders
            p25Decoder = new MBEDecoderManaged(MBEMode.IMBE);
            //p25Decoder.GainAdjust = Program.Configuration.VocoderDecoderAudioGain;
            //p25Decoder.AutoGain = Program.Configuration.VocoderDecoderAutoGain;
            p25Encoder = new MBEEncoderManaged(MBEMode.IMBE);
            //p25Encoder.GainAdjust = Program.Configuration.VocoderEncoderAudioGain;

            netLDU1 = new byte[9 * 25];
            netLDU2 = new byte[9 * 25];
        }

        internal void WhackerLinkVoiceChannelResponse(GRP_VCH_RSP response)
        {
            if (response.SrcId == WhackerLink2Dvm.config.SourceId.ToString())
            {
                voiceChannel = new VoiceChannel
                {
                    DstId = response.DstId,
                    SrcId = response.SrcId,
                    Frequency = response.Channel
                };
            }
        }

        internal async void WhackerLinkDataReceived(byte[] audioData, VoiceChannel voiceChannel)
        {
            FnePeer peer = (FnePeer)fne;

            uint srcId = (uint)WhackerLink2Dvm.config.SourceId;

            uint dstId = (uint)WhackerLink2Dvm.config.DestinationId;

            if (voiceChannel.Frequency != null)
            {
                audioDetect = true;
                if (txStreamId == 0)
                {
                    txStreamId = (uint)rand.Next(int.MinValue, int.MaxValue);
                    WhackerLink2Dvm.logger.Information($"({SystemName}) WL *CALL START     * PEER {fne.PeerId} SRC_ID {srcId} TGID {dstId} [STREAM ID {txStreamId}]");

                    SendP25TDU(true);
                }
                dropAudio.Reset();
            }
            else
            {
                if (dropAudio.IsRunning && (dropAudio.ElapsedMilliseconds > dropTimeMs))
                {
                    if (audioDetect)
                    {
                        WhackerLink2Dvm.logger.Information($"({SystemName}) WL *CALL END       * PEER {fne.PeerId} SRC_ID {srcId} TGID {dstId} [STREAM ID {txStreamId}]");

                        audioDetect = false;
                        dropAudio.Reset();

                        if (!callInProgress)
                        {
                            SendP25TDU();
                        }

                        srcIdOverride = 0;
                        txStreamId = 0;

                        dropTimeMs = WhackerLink2Dvm.config.DropTimeMs;

                        udpSrcId = 0;
                        udpDstId = 0;
                        trafficFromUdp = false;
                    }
                }
            }

            if (!dropAudio.IsRunning)
                dropAudio.Start();

            if (audioDetect && !callInProgress)
            {
                // Convert the 1600-byte audioData to 320-byte chunks
                var chunks = AudioConverter.SplitToChunks(audioData);
                foreach (var chunk in chunks)
                {
                    P25EncodeAudioFrame(chunk);
                }
            }
        }

        /// <summary>
        /// Stops the main execution loop for this <see cref="FneSystemBase"/>.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
        }

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
        protected override bool PeerIgnored(uint peerId, uint srcId, uint dstId, byte slot, fnecore.CallType callType, FrameType frameType, DMRDataType dataType, uint streamId)
        {
            return false;
        }

        /// <summary>
        /// Event handler used to handle a peer connected event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void PeerConnected(object sender, PeerConnectedEvent e)
        {
            return;
        }
    } // public abstract partial class FneSystemBase : fnecore.FneSystemBase
}