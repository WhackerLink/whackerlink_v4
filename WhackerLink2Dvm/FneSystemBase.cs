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
*   Copyright (C) 2024 Caleb, K4PHP
*
*/

using System.Diagnostics;
using Serilog;

using fnecore;
using fnecore.DMR;

#if !NOVODODE
using WhackerLinkLib.Vocoder;
#endif

using fnecore.P25.LC.TSBK;
using WhackerLinkLib.Models;
using WhackerLinkLib.Interfaces;
using WhackerLinkLib.Utils;
using WhackerLinkLib.Models.IOSP;
using WhackerLinkLib.Network;
using fnecore.P25;

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

        private Random rand;

        private IPeer webSocketHandler;

        private CallManager callManager;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="FneSystemBase"/> class.
        /// </summary>
        /// <param name="fne">Instance of <see cref="FneMaster"/> or <see cref="FnePeer"/></param>
        public FneSystemBase(FnePeer fne) : base(fne, fnecore.LogLevel.INFO)
        {
            this.fne = fne;

            this.rand = new Random(Guid.NewGuid().GetHashCode());

            WhackerLink2Dvm.logger.Information($"({SystemName}) Connecting to WLINK Master");

            webSocketHandler = new Peer();

            webSocketHandler.OnAffiliationUpdate += WhackerLinkAffiliationUpdate;
            webSocketHandler.OnUnitDeRegistrationResponse += WhackerLinkUniteDeRegistration;
            webSocketHandler.OnAudioData += WhackerLinkDataReceived;
            webSocketHandler.OnVoiceChannelResponse += WhackerLinkVoiceChannelResponse;
            webSocketHandler.OnVoiceChannelRelease += WhackerLinkVoiceChannelRelease;
            webSocketHandler.OnCallAlert += WhackerLinkCallAlert;

            webSocketHandler.OnSpecialFunction += (SPEC_FUNC response) =>
            {
                ushort extFuncType = (ushort)ExtendedFunction.INHIBIT;

                switch (response.Function)
                {
                    case SpecFuncType.RID_INHIBIT:
                        extFuncType = (ushort)ExtendedFunction.INHIBIT;
                        break;
                    case SpecFuncType.RID_UNINHIBIT:
                        extFuncType = (ushort)ExtendedFunction.UNINHIBIT;
                        break;
                    default:
                        return;
                };

                IOSP_EXT_FNCT extFunc = new IOSP_EXT_FNCT(extFuncType, P25Defines.WUID_FNE, uint.Parse(response.DstId));
                RemoteCallData callData = new RemoteCallData
                {
                    SrcId = uint.Parse(response.SrcId),
                    DstId = uint.Parse(response.DstId)
                };

                byte[] tsbk = new byte[P25Defines.P25_TSBK_LENGTH_BYTES];
                extFunc.Encode(ref tsbk);

                SendP25TSBK(callData, tsbk);

                Log.Logger.Information($"WhackerLink SPEC FUNC; SrcId: {response.SrcId}, DstId: {response.DstId}, Function: {response.Function}");
            };

            webSocketHandler.OnAckResponse += (ACK_RSP response) =>
            {
                IOSP_ACK_RSP ackResponse = new IOSP_ACK_RSP(uint.Parse(response.DstId), uint.Parse(response.SrcId), false, P25Defines.TSBK_IOSP_CALL_ALRT);
                RemoteCallData callData = new RemoteCallData
                {
                    SrcId = uint.Parse(response.SrcId),
                    DstId = uint.Parse(response.DstId)
                };

                byte[] tsbk = new byte[P25Defines.P25_TSBK_LENGTH_BYTES];
                ackResponse.Encode(ref tsbk);

                SendP25TSBK(callData, tsbk);

                Log.Logger.Information($"WhackerLink ACK RSP; SrcId: {response.SrcId}, DstId: {response.DstId}, Service: {response.Service}");

                //Log.Logger.Information($"({SystemName}) P25D: TSBK *ACK Response   * SRC_ID {response.DstId} DST_ID {response.SrcId} SERVICE {ackResponse.Service}");
            };

            webSocketHandler.OnOpen += () =>
            {
                WhackerLink2Dvm.logger.Information($"({SystemName}) Connection to WLINK Master complete");

                foreach (uint group in WhackerLink2Dvm.config.AllowedGroups)
                {
                    GRP_AFF_REQ affReq = new GRP_AFF_REQ()
                    {
                        SrcId = "1",
                        DstId = group.ToString(),
                        Site = WhackerLink2Dvm.config.WhackerLink.Site
                    };

                    webSocketHandler.SendMessage(affReq.GetData());
                }
            };

            webSocketHandler.OnClose += () =>
            {
                WhackerLink2Dvm.logger.Information($"({SystemName}) Connection to WLINK Master lost");
            };

            webSocketHandler.OnReconnecting += () =>
            {
                WhackerLink2Dvm.logger.Information($"({SystemName}) Reconnecting to WLINK Master");
            };

            try
            {
                webSocketHandler.Connect(WhackerLink2Dvm.config.WhackerLink.Address, WhackerLink2Dvm.config.WhackerLink.Port);
            }
            catch (Exception ex)
            {
                WhackerLink2Dvm.logger.Fatal($"({SystemName}) Connection to WLINK Master FAILED");
                Console.WriteLine(ex);
                return;
            }

            callManager = new CallManager(WhackerLink2Dvm.config.AllowedGroups);

            // initialize slot statuses
            //this.status = new SlotStatus[3];
            //this.status[0] = new SlotStatus();  // DMR Slot 1
            //this.status[1] = new SlotStatus();  // DMR Slot 2
            //this.status[2] = new SlotStatus();  // P25

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
        }

        internal void SendWhackerLinkCallAlert(uint dstId, uint srcId)
        {
            webSocketHandler.SendMessage(new { type = PacketType.CALL_ALRT_REQ, data = new CALL_ALRT { SrcId = srcId.ToString(), DstId = dstId.ToString() } });
        }

        internal void SendWhackerLinkAckResponse(uint dstId, uint srcId)
        {
            webSocketHandler.SendMessage(new { type = PacketType.ACK_RSP, data = new ACK_RSP { SrcId = srcId.ToString(), DstId = dstId.ToString(), Service = PacketType.CALL_ALRT } });
        }

        internal void SendWhackerLinkExtendedFunction(uint dstId, uint srcId, SpecFuncType specType)
        {
            webSocketHandler.SendMessage(new { type = PacketType.SPEC_FUNC, data = new SPEC_FUNC { SrcId = srcId.ToString(), DstId = dstId.ToString(), Function = specType } });
        }

        internal void WhackerLinkAffiliationUpdate(AFF_UPDATE response)
        {
            foreach (Affiliation affiliation in response.Affiliations)
            {
                fne.SendMasterGroupAffiliationRemoval(Convert.ToUInt32(affiliation.SrcId));
                fne.SendMasterGroupAffiliation(Convert.ToUInt32(affiliation.SrcId), Convert.ToUInt32(affiliation.DstId));
            }
        }

        internal void WhackerLinkUniteDeRegistration(U_DE_REG_RSP response)
        {
            fne.SendMasterUnitDeRegistration(Convert.ToUInt32(response.SrcId));
        }  

        internal void WhackerLinkVoiceChannelResponse(GRP_VCH_RSP response)
        {
            CallInfo currentCall = callManager.GetOrCreateCall(uint.Parse(response.SrcId), uint.Parse(response.DstId));

            currentCall.VoiceChannel = new VoiceChannel
            {
                DstId = response.DstId,
                SrcId = response.SrcId,
                Frequency = response.Channel
            };
        }

        internal void WhackerLinkVoiceChannelRelease(GRP_VCH_RLS response)
        {
            CallInfo currentCall = callManager.GetOrCreateCall(uint.Parse(response.SrcId), uint.Parse(response.DstId));

            EndCall(response.SrcId, response.DstId);
            currentCall.VoiceChannel = null;
        }

        internal void WhackerLinkCallAlert(CALL_ALRT response)
        {
            Log.Logger.Information($"WhackerLink CALL ALRT; SrcId: {response.SrcId}, DstId: {response.DstId}");

            try
            {
                RemoteCallData callData = new RemoteCallData
                {
                    SrcId = UInt32.Parse(response.SrcId),
                    DstId = UInt32.Parse(response.DstId)
                };

                byte[] tsbk = new byte[fnecore.P25.P25Defines.P25_TSBK_LENGTH_BYTES];

                IOSP_CALL_ALRT callAlert = new IOSP_CALL_ALRT(UInt32.Parse(response.DstId), UInt32.Parse(response.SrcId));
                callAlert.Encode(ref tsbk, true, true);
                SendP25TSBK(callData, tsbk);
            } catch (Exception) { /* stub */ }
        }

        internal void WhackerLinkDataReceived(AudioPacket audioPacket)
        {
            CallInfo currentCall = callManager.GetOrCreateCall(uint.Parse(audioPacket.VoiceChannel.SrcId), uint.Parse(audioPacket.VoiceChannel.DstId));
            FnePeer peer = (FnePeer)fne;

            uint srcId = Convert.ToUInt32(currentCall.VoiceChannel.SrcId);

            uint dstId = Convert.ToUInt32(currentCall.VoiceChannel.DstId);

            if (currentCall.VoiceChannel != null && currentCall.VoiceChannel.Frequency != null)
            {
                if (currentCall.txStreamId == 0)
                {
                    currentCall.txStreamId = (uint)rand.Next(int.MinValue, int.MaxValue);
                    WhackerLink2Dvm.logger.Information($"({SystemName}) WL *CALL START     * PEER {fne.PeerId} SRC_ID {srcId} TGID {dstId} [STREAM ID {currentCall.txStreamId}]");
                    
                    SendP25TDU(true, currentCall.VoiceChannel.SrcId, currentCall.VoiceChannel.DstId);
                }
            }
            else
            {
                EndCall(srcId.ToString(), dstId.ToString());
            }

            var chunks = AudioConverter.SplitToChunks(audioPacket.Data);
            foreach (var chunk in chunks)
            {
                P25EncodeAudioFrame(chunk, srcId, dstId);
            }
        }

        private void EndCall(string srcId, string dstId)
        {
            CallInfo currentCall = callManager.GetOrCreateCall(uint.Parse(srcId), uint.Parse(dstId));

            WhackerLink2Dvm.logger.Information($"({SystemName}) WL *CALL END       * PEER {fne.PeerId} SRC_ID {srcId} TGID {dstId} [STREAM ID {currentCall.txStreamId}]");

            currentCall.accumulatedChunks.Clear();
            FneUtils.Memset(currentCall.netLDU1, 0x00, currentCall.netLDU1.Length);
            FneUtils.Memset(currentCall.netLDU2, 0x00, currentCall.netLDU1.Length);

            currentCall.p25SeqNo = 0;
            currentCall.p25N = 0;

            SendP25TDU(false, srcId, dstId);

            currentCall.txStreamId = 0;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void KeyResponse(object sender, KeyResponseEvent e)
        {
            return;
        }
    } // public abstract partial class FneSystemBase : fnecore.FneSystemBase
}