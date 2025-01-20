/*
* WhackerLink - WhackerLinkServer
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program.  If not, see <http://www.gnu.org/licenses/>.
* 
* Copyright (C) 2024 Caleb, K4PHP
* 
*/

using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WhackerLinkServer.Models;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;
using WhackerLinkServer.Managers;
using Serilog;
using Nancy;
using WhackerLinkLib.Models;
using WhackerLinkLib.Models.IOSP;
using WhackerLinkLib.Utils;
using System.Collections.Generic;
using System.Threading;
using System;
using System.IO;
using System.Linq;
using Microsoft.VisualBasic;
using NAudio.Wave;

#if !NOVOCODE && !AMBEVOCODE
using vocoder;
#endif

namespace WhackerLinkServer
{
    /// <summary>
    /// Class to handle web socket clients
    /// </summary>
    public class ClientHandler : WebSocketBehavior
    {
        private Dictionary<string, Timer> talkgroupHangTimers = new Dictionary<string, Timer>();
        private readonly TimeSpan hangTimeout = TimeSpan.FromMinutes(3);

        private Config.MasterConfig masterConfig;
        private RidAclManager aclManager;
        private AffiliationsManager affiliationsManager;
        private VoiceChannelManager voiceChannelManager;
        private SiteManager siteManager;
        private Reporter reporter;
        private ILogger logger;

        private readonly WaveFormat waveFormat = new WaveFormat(8000, 16, 1);

#if !NOVOCODE && !AMBEVOCODE
        private readonly Dictionary<string, (MBEDecoderManaged Decoder, MBEEncoderManaged Encoder)> vocoderInstances;
#endif
#if AMBEVOCODE && !NOVOCODE
        private readonly Dictionary<string, (AmbeVocoderManager FullRate, AmbeVocoderManager HalfRate)> ambeVocoderInstances;
#endif

        /// <summary>
        /// Creates an instance of ClientHandler
        /// </summary>
        /// <param name="config"></param>
        /// <param name="aclManager"></param>
        /// <param name="affiliationsManager"></param>
        /// <param name="voiceChannelManager"></param>
        /// <param name="reporter"></param>
        /// <param name="p25Decoder"></param>
        /// <param name="p25Encoder"></param>
        /// <param name="logger"></param>
        public ClientHandler(Config.MasterConfig config, RidAclManager aclManager, AffiliationsManager affiliationsManager,
            VoiceChannelManager voiceChannelManager, SiteManager siteManager, Reporter reporter,
#if !NOVOCODE && !AMBEVOCODE
            Dictionary<string, (MBEDecoderManaged Decoder, MBEEncoderManaged Encoder)> vocoderInstances,
#endif
#if AMBEVOCODE && !NOVOCODE
            Dictionary<string, (AmbeVocoderManager FullRate, AmbeVocoderManager HalfRate)> ambeVocoderInstances,
#endif
            ILogger logger)
        {
            this.masterConfig = config;
            this.aclManager = aclManager;
            this.affiliationsManager = affiliationsManager;
            this.voiceChannelManager = voiceChannelManager;
            this.siteManager = siteManager;
            this.reporter = reporter;
            this.logger = logger;

#if !NOVOCODE && !AMBEVOCODE
            this.vocoderInstances = vocoderInstances;
#endif
#if AMBEVOCODE && !NOVOCODE
            this.ambeVocoderInstances = ambeVocoderInstances;
#endif
        }

        /// <summary>
        /// Callback for webhook message
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMessage(MessageEventArgs e)
        {
            try
            {
                var data = JObject.Parse(e.Data);
                var type = Convert.ToInt32(data["type"]);

                switch (type)
                {
                    case (int)PacketType.U_REG_REQ:
                        HandleUnitRegistrationRequest(data["data"].ToObject<U_REG_REQ>());
                        break;
                    case (int)PacketType.U_DE_REG_REQ:
                        HandleUnitDeRegistrationRequest(data["data"].ToObject<U_DE_REG_REQ>());
                        break;
                    case (int)PacketType.GRP_AFF_REQ:
                        HandleGroupAffiliationRequest(data["data"].ToObject<GRP_AFF_REQ>());
                        break;
                    case (int)PacketType.GRP_VCH_REQ:
                        HandleVoiceChannelRequest(data["data"].ToObject<GRP_VCH_REQ>());
                        break;
                    case (int)PacketType.GRP_VCH_RLS:
                        HandleVoiceChannelRelease(data["data"].ToObject<GRP_VCH_RLS>());
                        break;
                    case (int)PacketType.EMRG_ALRM_REQ:
                        HandleEmergencyAlarmRequest(data["data"].ToObject<EMRG_ALRM_REQ>());
                        break;
                    case (int)PacketType.CALL_ALRT_REQ:
                        HandleCallAlertRequest(data["data"].ToObject<CALL_ALRT_REQ>());
                        break;
                    case (int)PacketType.REL_DEMAND:
                        HandleReleaseDemand(data["data"].ToObject<REL_DEMND>());
                        break;
                    case (int)PacketType.LOC_BCAST:
                        HandleLocBcast(data["data"].ToObject<LOC_BCAST>());
                        break;
                    case (int)PacketType.STS_BCAST:
                        HandleStsBcast(data["data"].ToObject<STS_BCAST>());
                        break;
                    case (int)PacketType.SPEC_FUNC:
                        HandleSpecFunc(data["data"].ToObject<SPEC_FUNC>());
                        break;
                    case (int)PacketType.ACK_RSP:
                        HandleAckResponse(data["data"].ToObject<ACK_RSP>());
                        break;
                    case (int)PacketType.AUDIO_DATA:
                        BroadcastAudio(data["data"].ToObject<AudioPacket>());
                        break;
                    default:
                        logger.Warning("Unhandled Wlink Packet, Opcode: {Type}", type);
                        break;
                }
            }
            catch (IOException ex)
            {
                logger.Error(ex, "IOException occurred while processing message.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "An error occurred while processing message.");           
            }
        }

        /// <summary>
        /// Called on webhook disconnect
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);

            var clientId = ID;

            var channelsToRemove = voiceChannelManager.VoiceChannels
                .Where(vc => vc.ClientId == clientId)
                .Select(vc => vc.Frequency)
                .ToList();

            if (channelsToRemove != null && channelsToRemove.Count > 0)
            {
                foreach (var channel in channelsToRemove)
                {
                    voiceChannelManager.RemoveVoiceChannel(channel);
                    BroadcastMessage(JsonConvert.SerializeObject(new GRP_VCH_RLS
                    {
                        SrcId = voiceChannelManager.FindVoiceChannelByClientId(ID).SrcId,
                        DstId = voiceChannelManager.FindVoiceChannelByClientId(ID).DstId,
                        Channel = voiceChannelManager.FindVoiceChannelByClientId(ID).Frequency
                    }));
                    logger.Information("Voice channel {Channel} removed for disconnected client {ClientId}", channel, clientId);
                }
            }

            var affiliations = affiliationsManager.GetAffiliationsByClientId(clientId);

            if (affiliations != null && affiliations.Count > 0)
            {
                foreach (var affiliation in affiliations)
                {
                    U_DE_REG_RSP packet = new U_DE_REG_RSP
                    {
                        SrcId = affiliation.SrcId,
                        SysId = affiliation.Site.SystemID
                    };

                    logger.Information(packet.ToString());
                    BroadcastMessage(packet.GetStrData());

                    if (talkgroupHangTimers.ContainsKey(affiliation.DstId))
                    {
                        talkgroupHangTimers[affiliation.DstId].Dispose();
                        talkgroupHangTimers.Remove(affiliation.DstId);
                    }
                }

                affiliationsManager.RemoveAffiliationByClientId(clientId);
                logger.Information("Affiliations removed for disconnected client {ClientId}", clientId);
            }
        }

        /// <summary>
        /// Called on webhook error
        /// </summary>
        /// <param name="e"></param>
        protected override void OnError(ErrorEventArgs e)
        {
            base.OnError(e);
            logger.Error("WebSocket error: {Message}", e.Message);
        }

        /// <summary>
        /// Called on webhook connection
        /// </summary>
        protected override void OnOpen()
        {
            base.OnOpen();
            // logger.Information("WebSocket connection opened for client {ClientId}", ID);
        }

        /// <summary>
        /// Handled emergencyy alarm requests
        /// </summary>
        /// <param name="request"></param>
        private void HandleEmergencyAlarmRequest(EMRG_ALRM_REQ request)
        {
            logger.Information(request.ToString());
            reporter.Send(PacketType.EMRG_ALRM_REQ, request.SrcId, request.DstId, request.Site, null, ResponseType.UNKOWN, request.Lat, request.Long);

            var response = new EMRG_ALRM_RSP
            {
                SrcId = request.SrcId,
                DstId = request.DstId
            };

            BroadcastMessage(JsonConvert.SerializeObject(new { type = (int)PacketType.EMRG_ALRM_RSP, data = response }));
            logger.Information(response.ToString());
            reporter.Send(PacketType.EMRG_ALRM_RSP, request.SrcId, request.DstId, request.Site, null);
        }

        /// <summary>
        /// Handles call alert requests
        /// </summary>
        /// <param name="request"></param>
        private void HandleCallAlertRequest(CALL_ALRT_REQ request)
        {
            logger.Information(request.ToString());
            reporter.Send(PacketType.CALL_ALRT_REQ, request.SrcId, request.DstId, request.Site, null);

            var response = new CALL_ALRT
            {
                SrcId = request.SrcId,
                DstId = request.DstId
            };

            BroadcastMessage(JsonConvert.SerializeObject(new { type = (int)PacketType.CALL_ALRT, data = response }));
            logger.Information(response.ToString());
            reporter.Send(PacketType.CALL_ALRT, request.SrcId, request.DstId, request.Site, null);
        }

        /// <summary>
        /// Handles release demands
        /// </summary>
        /// <param name="request"></param>
        private void HandleReleaseDemand(REL_DEMND request)
        {
            logger.Information(request.ToString());
            // TODO: Actually handle?
        }

        /// <summary>
        /// Handles location broadcasts
        /// </summary>
        /// <param name="request"></param>
        private void HandleLocBcast(LOC_BCAST request)
        {
            // for now, only log, report, and repeate the packet. maybe in the future we do something fun server side?
            if (!masterConfig.DisableLocBcastLogs)
                logger.Information(request.ToString());

            reporter.Send(PacketType.LOC_BCAST, request.SrcId, null, request.Site, null, ResponseType.UNKOWN, request.Lat, request.Long);
            BroadcastMessage(request.GetStrData());
        }

        /// <summary>
        /// Handles location broadcasts
        /// </summary>
        /// <param name="request"></param>
        private void HandleStsBcast(STS_BCAST request)
        {
            // for now, only log, report, and repeate
            logger.Information(request.ToString());
            reporter.Send(PacketType.STS_BCAST, request);

            BroadcastMessage(request.GetStrData());
        }

        /// <summary>
        /// Handles special function
        /// </summary>
        /// <param name="request"></param>
        private void HandleSpecFunc(SPEC_FUNC request)
        {
            logger.Information(request.ToString());

            SPEC_FUNC response = new SPEC_FUNC
            {
                Function = SpecFuncType.UNKOWN,
                SrcId = request.SrcId,
                DstId = request.DstId,
            };

            switch (request.Function)
            {

                case SpecFuncType.RID_INHIBIT: // TODO: Store radio inhibit status
                    response.Function = SpecFuncType.RID_INHIBIT;
                    BroadcastMessage(response.GetStrData());
                    break;
                case SpecFuncType.RID_UNINHIBIT: // TODO: Store radio uninhibit status
                    response.Function = SpecFuncType.RID_UNINHIBIT;
                    BroadcastMessage(response.GetStrData());
                    break;
                default:
                    logger.Warning($"Unhandled SPEC_FUNC function: {request.Function}");
                    break;
            }
        }

        /// <summary>
        /// Handle ack response
        /// </summary>
        /// <param name="response"></param>
        private void HandleAckResponse(ACK_RSP response)
        {
            logger.Information(response.ToString());

            switch (response.Service)
            {
                case PacketType.CALL_ALRT:
                    BroadcastMessage(response.GetStrData());
                    break;
                case PacketType.SPEC_FUNC:
                    BroadcastMessage(response.GetStrData());
                    break;
                default:
                    logger.Warning($"Unhandled ACK RSP service: {response.Service}");
                    break;
            }
        }

        /// <summary>
        /// Handles group affiliation request
        /// </summary>
        /// <param name="request"></param>
        private void HandleGroupAffiliationRequest(GRP_AFF_REQ request)
        {
            logger.Information(request.ToString());
            reporter.Send(PacketType.GRP_AFF_REQ, request.SrcId, request.DstId, request.Site, null);

            var clientId = ID;
            Affiliation affiliation = new Affiliation(clientId, request.SrcId, request.DstId, request.Site);

            var response = new GRP_AFF_RSP
            {
                SrcId = request.SrcId,
                DstId = request.DstId,
                SysId = request.SysId
            };

            if (isAffiliationPermitted(request.SrcId, request.DstId))
            {
                response.Status = (int)ResponseType.GRANT;
                affiliationsManager.RemoveAffiliation(request.SrcId);
                affiliationsManager.AddAffiliation(affiliation);

                AFF_UPDATE affUpdate = new AFF_UPDATE
                {
                    Affiliations = affiliationsManager.GetAffiliations()
                };

                BroadcastMessage(JsonConvert.SerializeObject(new { type = (int)PacketType.AFF_UPDATE, data = affUpdate }));
            }
            else
            {
                response.Status = (int)ResponseType.DENY;
                affiliationsManager.RemoveAffiliation(affiliation);
            }

            BroadcastMessage(JsonConvert.SerializeObject(new { type = (int)PacketType.GRP_AFF_RSP, data = response }));
            logger.Information(response.ToString());
            reporter.Send(PacketType.GRP_AFF_RSP, request.SrcId, request.DstId, request.Site, null, (ResponseType)response.Status);
        }

        /// <summary>
        /// Handles u reg request
        /// </summary>
        /// <param name="request"></param>
        private void HandleUnitRegistrationRequest(U_REG_REQ request)
        {
            logger.Information(request.ToString());
            reporter.Send(PacketType.U_REG_REQ, request.SrcId, null, request.Site, null);

            var response = new U_REG_RSP
            {
                SrcId = request.SrcId,
                SysId = request.SysId,
                Wacn = request.Wacn
            };

            if (isRidAuthed(request.SrcId))
            {
                response.Status = (int)ResponseType.GRANT;
            }
            else
            {
                logger.Warning("RID ACL Rejection, {srcId}", request.SrcId);
                response.Status = (int)ResponseType.REFUSE;
            }

            Send(JsonConvert.SerializeObject(new { type = (int)PacketType.U_REG_RSP, data = response }));
            logger.Information(response.ToString());
            reporter.Send(PacketType.U_REG_RSP, request.SrcId, null, request.Site, null, (ResponseType)response.Status);

            if (aclManager.IsAuthEnabled(response.SrcId))
            {
                AUTH_DEMAND authDemand = new AUTH_DEMAND { SrcId = response.SrcId };

                Send(JsonConvert.SerializeObject(authDemand.GetData()));
                logger.Information(authDemand.ToString());
            }
        }

        /// <summary>
        /// Handles u de reg request
        /// </summary>
        /// <param name="request"></param>
        private void HandleUnitDeRegistrationRequest(U_DE_REG_REQ request)
        {
            logger.Information(request.ToString());
            reporter.Send(PacketType.U_DE_REG_REQ, request.SrcId, null, request.Site, null);

            var response = new U_DE_REG_RSP
            {
                SrcId = request.SrcId,
                SysId = request.SysId,
                Wacn = request.Wacn
            };

            if (isRidAuthed(request.SrcId))
            {
                response.Status = (int)ResponseType.GRANT;
                affiliationsManager.RemoveAffiliation(request.SrcId);

                AFF_UPDATE affUpdate = new AFF_UPDATE
                {
                    Affiliations = affiliationsManager.GetAffiliations()
                };

                BroadcastMessage(JsonConvert.SerializeObject(new { type = (int)PacketType.AFF_UPDATE, data = affUpdate }));
            }
            else
            {
                logger.Warning("RID ACL Rejection, {srcId}", request.SrcId);
                response.Status = (int)ResponseType.FAIL;
            }

            Send(JsonConvert.SerializeObject(new { type = (int)PacketType.U_DE_REG_RSP, data = response }));
            logger.Information(response.ToString());
            reporter.Send(PacketType.U_DE_REG_RSP, request.SrcId, null,request.Site, null, (ResponseType)response.Status);

            Context.WebSocket.Close();
        }

        /// <summary>
        /// Handles voice channel request
        /// </summary>
        /// <param name="request"></param>
        private void HandleVoiceChannelRequest(GRP_VCH_REQ request)
        {
            logger.Information(request.ToString());
            reporter.Send(PacketType.GRP_VCH_REQ, request.SrcId, request.DstId, request.Site, null);

            var response = new GRP_VCH_RSP
            {
                DstId = request.DstId,
                SrcId = request.SrcId
            };

            var site = siteManager.GetSiteById(request.Site.SiteID);

            if (site == null)
            {
                logger.Warning("Site not found for SiteId: {SiteId}", request.Site.SiteID);
                response.Status = (int)ResponseType.DENY;
                BroadcastMessage(JsonConvert.SerializeObject(new { type = (int)PacketType.GRP_VCH_RSP, data = response }));
                return;
            }

            var availableChannel = GetAvailableVoiceChannel(site);

            if (availableChannel != null && isDestinationPermitted(request.SrcId, request.DstId))
            {
                ResetHangTimer(request.DstId, request.SrcId, site);

                voiceChannelManager.AddVoiceChannel(new VoiceChannel
                {
                    DstId = request.DstId,
                    SrcId = request.SrcId,
                    Frequency = availableChannel,
                    ClientId = ID,
                    IsActive = true,
                    Site = site
                });

                response.Channel = availableChannel;
                response.Status = (int)ResponseType.GRANT;

                BroadcastMessage(JsonConvert.SerializeObject(new { type = (int)PacketType.GRP_VCH_RSP, data = response }));
                logger.Information(response.ToString());
            }
            else
            {
                response.Status = (int)ResponseType.DENY;

                BroadcastMessage(JsonConvert.SerializeObject(new { type = (int)PacketType.GRP_VCH_RSP, data = response }));
                logger.Information(response.ToString());
            }

            reporter.Send(PacketType.GRP_VCH_RSP, request.SrcId, request.DstId, request.Site, null, (ResponseType)response.Status);
        }

        /// <summary>
        /// Handles voice channel release
        /// </summary>
        /// <param name="request"></param>
        private void HandleVoiceChannelRelease(GRP_VCH_RLS request)
        {
            logger.Information(request.ToString());

            GRP_VCH_RLS response = new GRP_VCH_RLS
            {
                SrcId = request.SrcId,
                DstId = request.DstId,
                Channel = request.Channel
            };

            if (!request.Channel.IsNullOrEmpty())
            {
                voiceChannelManager.FindVoiceChannelByClientId(ID).IsActive = false;
            }

            if (voiceChannelManager.IsVoiceChannelActive(new VoiceChannel { Frequency = request.Channel }))
            {
                voiceChannelManager.RemoveVoiceChannel(request.Channel);
                BroadcastMessage(JsonConvert.SerializeObject(new { type = (int)PacketType.GRP_VCH_RLS, data = response }));
                logger.Information("Voice channel {Channel} released for {SrcId} to {DstId}", request.Channel, request.SrcId, request.DstId);
#if !NOVOCODE && !AMBEVOCODE
                if (vocoderInstances.ContainsKey(request.DstId))
                {
                    vocoderInstances.Remove(request.DstId);
                }
#endif
#if AMBEVOCODE
                if (ambeVocoderInstances.ContainsKey(request.DstId))
                {
                    ambeVocoderInstances.Remove(request.DstId);
                }
#endif
            }
            else
            {
                if (request.Channel.IsNullOrEmpty())
                {
                    logger.Warning("Removing channel grant for {DstId} due to the voice channel being null", request.DstId); // TODO: Not 100% if this is a proper fix, but it seems to work
                    BroadcastMessage(JsonConvert.SerializeObject(new { type = (int)PacketType.GRP_VCH_RLS, data = new GRP_VCH_RLS { SrcId = request.SrcId, DstId = request.DstId } }));
                    voiceChannelManager.RemoveVoiceChannelByDstId(request.DstId);
#if !NOVOCODE && !AMBEVOCODE
                if (vocoderInstances.ContainsKey(request.DstId))
                {
                    vocoderInstances.Remove(request.DstId);
                }
#endif
#if AMBEVOCODE
                    if (ambeVocoderInstances.ContainsKey(request.DstId))
                    {
                        ambeVocoderInstances.Remove(request.DstId);
                    }
#endif
                }

                logger.Warning("Voice channel {Channel} not found to release", request.Channel);
            }

            if (talkgroupHangTimers.ContainsKey(request.DstId))
            {
                talkgroupHangTimers[request.DstId].Dispose();
                talkgroupHangTimers.Remove(request.DstId);
                logger.Information("Stopped hang timer for talkgroup {DstId}", request.DstId);
            }

            reporter.Send(PacketType.GRP_VCH_RLS, request.SrcId, request.DstId, request.Site, null);
        }

        /// <summary>
        /// Checks if the affiliation is allowed to happen
        /// </summary>
        /// <param name="srcId"></param>
        /// <param name="dstId"></param>
        /// <returns></returns>
        private bool isAffiliationPermitted(string srcId, string dstId)
        {
            return true; // TODO: Actually use this
        }

        /// <summary>
        /// Checks if the destination is currently permitted
        /// </summary>
        /// <param name="srcId"></param>
        /// <param name="dstId"></param>
        /// <returns></returns>
        private bool isDestinationPermitted(string srcId, string dstId)
        {
            bool value = true;

            if (voiceChannelManager.IsDestinationActive(dstId))
                value = false;

            // TODO: TG ACL

            return value;
        }

        /// <summary>
        /// Checks if RID is authorized
        /// </summary>
        /// <param name="srcId"></param>
        /// <returns></returns>
        private bool isRidAuthed(string srcId)
        {
            return aclManager.IsRidAllowed(srcId);
        }

        /// <summary>
        /// Checks if RID is authorized
        /// </summary>
        /// <param name="srcId"></param>
        /// <returns></returns>
        private ResponseType isRidLlaValid(string srcId, string hashed)
        {
            return aclManager.IsRidAllowed(srcId, hashed);
        }

        /// <summary>
        /// Gets list of available voice channels
        /// </summary>
        /// <returns></returns>
        private string GetAvailableVoiceChannel(Site site)
        {
            foreach (var channel in site.VoiceChannels)
            {
                if (!voiceChannelManager.IsVoiceChannelActive(new VoiceChannel { Frequency = channel }))
                {
                    return channel;
                }
            }
            return null;
        }

        /// <summary>
        /// Helper to reset hang timer
        /// </summary>
        /// <param name="talkgroupId"></param>
        /// <param name="srcId"></param>
        /// <param name="site"></param>
        private void ResetHangTimer(string talkgroupId, string srcId, Site site)
        {
            if (talkgroupHangTimers.ContainsKey(talkgroupId))
            {
                talkgroupHangTimers[talkgroupId].Change(hangTimeout, Timeout.InfiniteTimeSpan);
            }
            else
            {
                var timer = new Timer(HangTimerElapsed, new Tuple<string, string, Site>(talkgroupId, srcId, site), hangTimeout, Timeout.InfiniteTimeSpan);
                talkgroupHangTimers.Add(talkgroupId, timer);
            }
        }

        /// <summary>
        /// Hang timer callback
        /// </summary>
        /// <param name="state"></param>
        private void HangTimerElapsed(object state)
        {
            var data = (Tuple<string, string, Site>)state;
            string talkgroupId = data.Item1;
            string srcId = data.Item2;
            Site site = data.Item3;

            talkgroupHangTimers.Remove(talkgroupId);

            var response = new GRP_VCH_RLS
            {
                SrcId = srcId,
                DstId = talkgroupId,
                Site = site
            };

            BroadcastMessage(JsonConvert.SerializeObject(new { type = (int)PacketType.GRP_VCH_RLS, data = response }));
            voiceChannelManager.RemoveVoiceChannelByDstId(talkgroupId);
            logger.Information("Talkgroup hang timer elapsed, releasing voice channel for talkgroup {TalkgroupId}", talkgroupId);
        }

        /// <summary>
        /// Broadcasts a message to all clients (Optionally skip the sender
        /// </summary>
        /// <param name="message"></param>
        /// <param name="skipMe"></param>
        private void BroadcastMessage(string message, bool skipMe = false)
        {
            foreach (var session in Sessions.Sessions)
            {
                if (skipMe && ID == session.ID)
                    return;

                Sessions.SendTo(message, session.ID);
            }
        }

        /// <summary>
        /// Helper to broadcast audio. Also handles vocoding
        /// </summary>
        /// <param name="audioData"></param>
        /// <param name="voiceChannel"></param>
        private void BroadcastAudio(AudioPacket audioPacket)
        {
            VoiceChannel channel = voiceChannelManager.FindVoiceChannelByDstId(audioPacket.VoiceChannel.DstId);
            string dstId = audioPacket.VoiceChannel.DstId;

            if (!voiceChannelManager.IsDestinationActive(audioPacket.VoiceChannel.DstId))
            {
                logger.Warning("Ignoring call; destination not permitted for traffic srcId: {SrcId}, dstId: {DstId}", audioPacket.VoiceChannel.SrcId, audioPacket.VoiceChannel.DstId);
                return;
            }

            /*
                        if (audioPacket.VoiceChannel != null && channel.ClientId != ID && channel.DstId == audioPacket.VoiceChannel.DstId)
                        {
                            logger.Warning("Ignoring call; traffic collision srcId: {SrcId}, dstId: {DstId}", audioPacket.VoiceChannel.SrcId, audioPacket.VoiceChannel.DstId);
                            voiceChannelManager.RemoveVoiceChannelByClientId(ID);
                            BroadcastMessage(JsonConvert.SerializeObject(new { type = (int)PacketType.GRP_VCH_RLS, data = new GRP_VCH_RLS { DstId = audioPacket.VoiceChannel.DstId, SrcId = audioPacket.VoiceChannel.SrcId, Site = audioPacket.Site } }));
                            return;
                        }
            */

            if (audioPacket.LopServerVocode)
            {
                // This is mainly for the console sending tones.
                // Lops off the vocoder so you dont lop off your ears.
                audioPacket.AudioMode = AudioMode.PCM_8_16;
                BroadcastMessage(audioPacket.GetStrData());
                return;
            }


            if (masterConfig.VocoderMode != VocoderModes.DISABLED)
            {
#if !NOVOCODE || AMBEVOCODE
                byte[] imbe = null;

                // Ensure a vocoder instance exists for the channel
#if !NOVOCODE && !AMBEVOCODE
                if (!vocoderInstances.ContainsKey(dstId))
                {
                    vocoderInstances[dstId] = CreateInternalVocoderInstance();
                    logger.Information("Created new vocoder instance for channel {ChannelId}", dstId);
                }

                var (decoder, encoder) = vocoderInstances[dstId];
#endif
#if AMBEVOCODE && !NOVOCODE
                if (!ambeVocoderInstances.ContainsKey(dstId))
                {
                    ambeVocoderInstances[dstId] = CreateExternalVocoderInstance();
                    logger.Information("Created new external vocoder instance for dstId {dstId}", dstId);
                }

                var (fullRateVocoder, halfRateVocoder) = ambeVocoderInstances[dstId];
#endif
                var chunks = AudioConverter.SplitToChunks(audioPacket.Data);
                if (chunks.Count == 0)
                {
                    logger.Error("Invalid audio data length for conversion.");
                    return;
                }

                List<byte[]> processedChunks = new List<byte[]>();

                foreach (var chunk in chunks)
                {
                    // Thanks gatekeep :wink:
                    BufferedWaveProvider buffer = new BufferedWaveProvider(waveFormat);
                    buffer.AddSamples(chunk, 0, chunk.Length);

                    VolumeWaveProvider16 gainControl = new VolumeWaveProvider16(buffer);
                    gainControl.Volume = masterConfig.PreEncodeGain;
                    gainControl.Read(chunk, 0, chunk.Length);

                    int smpIdx = 0;
                    short[] samples = new short[chunk.Length / 2];
                    for (int pcmIdx = 0; pcmIdx < chunk.Length; pcmIdx += 2)
                    {
                        samples[smpIdx] = (short)((chunk[pcmIdx + 1] << 8) + chunk[pcmIdx]);
                        smpIdx++;
                    }

#if !AMBEVOCODE
                    encoder.encode(samples, out imbe);
#else
                    if (masterConfig.VocoderMode == VocoderModes.IMBE)
                    {
                        fullRateVocoder.Encode(samples, out imbe);
                    }
                    else if (masterConfig.VocoderMode == VocoderModes.DMRAMBE)
                    {
                        halfRateVocoder.Encode(samples, out imbe);
                    }
#endif
                    short[] decodedSamples = null;

#if !AMBEVOCODE
                    int errors = decoder.decode(imbe, out decodedSamples);
#else
                    if (masterConfig.VocoderMode == VocoderModes.IMBE)
                    {
                        int errors = fullRateVocoder.Decode(imbe, out decodedSamples);
                    }
                    else if (masterConfig.VocoderMode == VocoderModes.DMRAMBE)
                    {
                        int errors = halfRateVocoder.Decode(imbe, out decodedSamples);
                    }
#endif

                    if (decodedSamples != null)
                    {
                        int pcmIdx = 0;
                        byte[] pcmData = new byte[decodedSamples.Length * 2];
                        for (int i = 0; i < decodedSamples.Length; i++)
                        {
                            pcmData[pcmIdx] = (byte)(decodedSamples[i] & 0xFF);
                            pcmData[pcmIdx + 1] = (byte)((decodedSamples[i] >> 8) & 0xFF);
                            pcmIdx += 2;
                        }

                        processedChunks.Add(pcmData);
                    }
                }

                var combinedAudioData = AudioConverter.CombineChunks(processedChunks);
                if (combinedAudioData != null)
                {
                    audioPacket.AudioMode = AudioMode.PCM_8_16;
                    audioPacket.Data = combinedAudioData;
                    BroadcastMessage(audioPacket.GetStrData());
                }
                else
                {
                    logger.Error("Failed to combine processed audio chunks for dstId {dstId}", dstId);
                }
#endif
            }
            else
            {
                audioPacket.AudioMode = AudioMode.PCM_8_16;
                BroadcastMessage(audioPacket.GetStrData());
            }
        }

#if !NOVOCODE && !AMBEVOCODE
        private (MBEDecoderManaged Decoder, MBEEncoderManaged Encoder) CreateInternalVocoderInstance()
        {
            var decoder = new MBEDecoderManaged(masterConfig.VocoderMode == VocoderModes.IMBE ? MBEMode.IMBE : MBEMode.DMRAMBE);
            var encoder = new MBEEncoderManaged(masterConfig.VocoderMode == VocoderModes.IMBE ? MBEMode.IMBE : MBEMode.DMRAMBE);
            return (decoder, encoder);
        }
#endif

#if AMBEVOCODE && !NOVOCODE
        private (AmbeVocoderManager FullRate, AmbeVocoderManager HalfRate) CreateExternalVocoderInstance()
        {
            return (new AmbeVocoderManager(), new AmbeVocoderManager(false));
        }
#endif

        private void CleanupVocoderInstance(string channelId)
        {
#if !NOVOCODE && !AMBEVOCODE
            if (vocoderInstances.ContainsKey(channelId))
            {
                vocoderInstances.Remove(channelId);
                logger.Information("Removed vocoder instance for channel {ChannelId}", channelId);
            }
#endif
#if AMBEVOCODE && !NOVOCODE
            if (ambeVocoderInstances.ContainsKey(channelId))
            {
                ambeVocoderInstances.Remove(channelId);
                logger.Information("Removed external vocoder instance for channel {ChannelId}", channelId);
            }
#endif
        }
    }
}