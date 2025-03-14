﻿/*
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
using WhackerLinkLib.Interfaces;
using NWaves.Signals;
using WhackerLinkLib.Managers;
using NWaves.Filters.Butterworth;
using System.Diagnostics;
using NWaves.Filters.Base;

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
        private IMasterService master;
        private AuthKeyFileManager authKeyManager;
        private readonly VocoderManager vocoderManager;
        private ILogger logger;

        private ToneDetector toneDetecor = new ToneDetector();

        private readonly WaveFormat waveFormat = new WaveFormat(8000, 16, 1);

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
            VocoderManager vocoderManager,
#endif
#if AMBEVOCODE && !NOVOCODE
            Dictionary<string, (AmbeVocoderManager FullRate, AmbeVocoderManager HalfRate)> ambeVocoderInstances,
#endif
            IMasterService master,
            AuthKeyFileManager authManager,
            ILogger logger)
        {
            this.masterConfig = config;
            this.aclManager = aclManager;
            this.affiliationsManager = affiliationsManager;
            this.voiceChannelManager = voiceChannelManager;
            this.siteManager = siteManager;
            this.reporter = reporter;
            this.master = master;
            this.authKeyManager = authManager;
            this.logger = logger;

#if !NOVOCODE && !AMBEVOCODE
            this.vocoderManager = vocoderManager;
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
                    case (int)PacketType.GRP_AFF_RMV:
                        HandleGroupAffiliationRemoval(data["data"].ToObject<GRP_AFF_RMV>());
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
                        Task.Run(() => BroadcastAudio(data["data"].ToObject<AudioPacket>()));
                        break;
                    default:
                        logger.Warning("Unhandled Wlink Packet, Opcode: {Type}", type);
                        break;
                }
            }
            catch (ObjectDisposedException ex)
            {
                logger.Warning(ex, "WebSocket message processing failed: NetworkStream was disposed.");
            }
            catch (IOException ex)
            {
                logger.Error(ex, "IOException occurred while processing WebSocket message.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "An error occurred while processing WebSocket message.");
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
                    master.BroadcastPacket(JsonConvert.SerializeObject(new GRP_VCH_RLS
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
                    master.BroadcastPacket(packet.GetStrData());

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
        /// <summary>
        /// Called on webhook error
        /// </summary>
        /// <param name="e"></param>
        protected override void OnError(ErrorEventArgs e)
        {
            try
            {
                if (e.Exception is ObjectDisposedException)
                {
                    logger.Warning("WebSocket error: Attempted to use a disposed NetworkStream. Connection may have been lost.");
                }
                else if (e.Exception is NotSupportedException)
                {
                    logger.Warning("WebSocket error: Operation is not supported on this platform.");
                }
                else
                {
                    logger.Error(e.Exception, "Unexpected WebSocket error: {Message}", e.Message);
                }
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "An unhandled exception occurred in WebSocket error handling.");
            }
        }

        /// <summary>
        /// Called on webhook connection
        /// </summary>
        protected override void OnOpen()
        {
            base.OnOpen();

            if (masterConfig.Auth != null) {
                if (masterConfig.Auth.Enabled)
                {
                    string providedKey = Context.QueryString["authKey"];

                    if (string.IsNullOrEmpty(providedKey))
                    {
                        logger.Warning("[NET] peer authentication failed for client {ClientId}: Missing auth key");
                        Context.WebSocket.Close(CloseStatusCode.PolicyViolation, "Missing auth key.");
                        return;
                    }

                    if (!authKeyManager.IsValidAuthKey(providedKey))
                    {
                        logger.Warning("[NET] peer authentication failed for client {ClientId}: Invalid auth key", ID);
                        Context.WebSocket.Close(CloseStatusCode.PolicyViolation, "Invalid auth key.");
                        return;
                    }
                }
            }

            //logger.Information("WebSocket authentication successful for {ClientId}", ID);
        }

        /// <summary>
        /// Handled emergency alarm requests
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

            master.BroadcastPacket(JsonConvert.SerializeObject(new { type = (int)PacketType.EMRG_ALRM_RSP, data = response }));
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

            master.BroadcastPacket(JsonConvert.SerializeObject(new { type = (int)PacketType.CALL_ALRT, data = response }));
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
            if (masterConfig.DisableLocationBroadcasts)
                return;

            // for now, only log, report, and repeate the packet. maybe in the future we do something fun server side?
            if (!masterConfig.DisableLocBcastLogs)
                logger.Information(request.ToString());

            reporter.Send(PacketType.LOC_BCAST, request.SrcId, null, request.Site, null, ResponseType.UNKOWN, request.Lat, request.Long);

            if (!masterConfig.DisableLocationBroadcastsRepeats)
                master.BroadcastPacket(request.GetStrData());
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

            master.BroadcastPacket(request.GetStrData());
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
                    master.BroadcastPacket(response.GetStrData());
                    break;
                case SpecFuncType.RID_UNINHIBIT: // TODO: Store radio uninhibit status
                    response.Function = SpecFuncType.RID_UNINHIBIT;
                    master.BroadcastPacket(response.GetStrData());
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
                    master.BroadcastPacket(response.GetStrData());
                    break;
                case PacketType.SPEC_FUNC:
                    master.BroadcastPacket(response.GetStrData());
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

                // only remove the aff if the srcid is actually on that tg. This is mainly so the console can be on multi talkgroups. Idk if this is right.
                if (affiliationsManager.isSrcIdAffiliated(request.SrcId, request.DstId))
                    affiliationsManager.RemoveAffiliation(request.SrcId, request.DstId);

                affiliationsManager.AddAffiliation(affiliation);

                AFF_UPDATE affUpdate = new AFF_UPDATE
                {
                    Affiliations = affiliationsManager.GetAffiliations()
                };

                master.BroadcastPacket(affUpdate.GetStrData());
            }
            else
            {
                response.Status = (int)ResponseType.DENY;
                affiliationsManager.RemoveAffiliation(affiliation);
            }

            master.BroadcastPacket(response.GetStrData());
            logger.Information(response.ToString());
            reporter.Send(PacketType.GRP_AFF_RSP, request.SrcId, request.DstId, request.Site, null, (ResponseType)response.Status);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="removal"></param>
        private void HandleGroupAffiliationRemoval(GRP_AFF_RMV removal)
        {
            logger.Information(removal.ToString());
            reporter.Send(PacketType.GRP_AFF_RMV, removal.SrcId, removal.DstId, removal.Site, null);

            if (affiliationsManager.isSrcIdAffiliated(removal.SrcId, removal.DstId))
                affiliationsManager.RemoveAffiliation(removal.SrcId, removal.DstId);

            master.BroadcastPacket(removal.GetStrData());
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

            AFF_UPDATE affUpdate = new AFF_UPDATE
            {
                Affiliations = affiliationsManager.GetAffiliations()
            };

            master.BroadcastPacket(affUpdate.GetStrData());
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

                master.BroadcastPacket(JsonConvert.SerializeObject(new { type = (int)PacketType.AFF_UPDATE, data = affUpdate }));
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
                master.BroadcastPacket(JsonConvert.SerializeObject(new { type = (int)PacketType.GRP_VCH_RSP, data = response }));
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

                master.BroadcastPacket(JsonConvert.SerializeObject(new { type = (int)PacketType.GRP_VCH_RSP, data = response }));
                logger.Information(response.ToString());
            }
            else
            {
                response.Status = (int)ResponseType.DENY;

                master.BroadcastPacket(JsonConvert.SerializeObject(new { type = (int)PacketType.GRP_VCH_RSP, data = response }));
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
                master.BroadcastPacket(JsonConvert.SerializeObject(new { type = (int)PacketType.GRP_VCH_RLS, data = response }));
                logger.Information("Voice channel {Channel} released for {SrcId} to {DstId}", request.Channel, request.SrcId, request.DstId);
#if !NOVOCODE && !AMBEVOCODE
                vocoderManager.RemoveVocoder(request.DstId);
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
                    master.BroadcastPacket(JsonConvert.SerializeObject(new { type = (int)PacketType.GRP_VCH_RLS, data = new GRP_VCH_RLS { SrcId = request.SrcId, DstId = request.DstId } }));
                    voiceChannelManager.RemoveVoiceChannelByDstId(request.DstId);
#if !NOVOCODE && !AMBEVOCODE
                    vocoderManager.RemoveVocoder(request.DstId);
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

            if (Sessions.TryGetSession(ID, out var session))
            {
                master.BroadcastPacket(JsonConvert.SerializeObject(new { type = (int)PacketType.GRP_VCH_RLS, data = response }));
                voiceChannelManager.RemoveVoiceChannelByDstId(talkgroupId);
                logger.Information($"Talkgroup hang timer elapsed, releasing voice channel for talkgroup {talkgroupId}");
            }
            else
            {
                logger.Warning($"Skipping hang timer cleanup. Client {ID} is disconnected.");
            }
        }

        /// <summary>
        /// Helper to broadcast audio. Also handles vocoding
        /// </summary>
        /// <param name="audioData"></param>
        /// <param name="voiceChannel"></param>
        private void BroadcastAudio(AudioPacket audioPacket)
        {
            bool affRestrict = masterConfig.AffilationRestricted;

            string client = ID;

            if (!masterConfig.NoSelfRepeat)
                client = string.Empty;

            VoiceChannel channel = voiceChannelManager.FindVoiceChannelByDstId(audioPacket.VoiceChannel.DstId);
            string dstId = audioPacket.VoiceChannel.DstId;

            if (!voiceChannelManager.IsDestinationActive(audioPacket.VoiceChannel.DstId))
            {
                logger.Warning("Ignoring call; destination not permitted for traffic srcId: {SrcId}, dstId: {DstId}", audioPacket.VoiceChannel.SrcId, audioPacket.VoiceChannel.DstId);
                return;
            }

            if (!voiceChannelManager.IsSrcGranted(audioPacket.VoiceChannel.DstId, audioPacket.VoiceChannel.SrcId))
            {
                logger.Warning("Ignoring call; source not permitted for destination's traffic srcId: {SrcId}, dstId: {DstId}", audioPacket.VoiceChannel.SrcId, audioPacket.VoiceChannel.DstId);
                return;
            }

            if (!affiliationsManager.isSrcIdAffiliated(audioPacket.VoiceChannel.SrcId, dstId) && affRestrict)
            {
                logger.Warning("Ignoring call; source not affiliated to destination srcId: {SrcId}, dstId: {DstId}", audioPacket.VoiceChannel.SrcId, audioPacket.VoiceChannel.DstId);
                return;
            }

            List<string> affiliatedClients = affiliationsManager.GetAffiliations()
                                           .Where(a => a.DstId == dstId)
                                           .Select(a => a.ClientId)
                                           .ToList();

            if (affiliatedClients.Count == 0 && affRestrict)
            {
                logger.Warning("No affiliations found for dstId: {DstId}??", dstId);
                return;
            }

            /*
                        if (audioPacket.VoiceChannel != null && channel.ClientId != ID && channel.DstId == audioPacket.VoiceChannel.DstId)
                        {
                            logger.Warning("Ignoring call; traffic collision srcId: {SrcId}, dstId: {DstId}", audioPacket.VoiceChannel.SrcId, audioPacket.VoiceChannel.DstId);
                            voiceChannelManager.RemoveVoiceChannelByClientId(ID);
                            _master.BroadcastPacket(JsonConvert.SerializeObject(new { type = (int)PacketType.GRP_VCH_RLS, data = new GRP_VCH_RLS { DstId = audioPacket.VoiceChannel.DstId, SrcId = audioPacket.VoiceChannel.SrcId, Site = audioPacket.Site } }));
                            return;
                        }
            */

            if (audioPacket.LopServerVocode)
            {
                // This is mainly for the console sending tones.
                // Lops off the vocoder so you dont lop off your ears.
                audioPacket.AudioMode = AudioMode.PCM_8_16;

                if (!affRestrict)
                    master.BroadcastPacket(audioPacket.GetStrData(), client);
                else
                    master.BroadcastPacket(audioPacket.GetStrData(), affiliatedClients, client);

                return;
            }


            if (masterConfig.VocoderMode != VocoderModes.DISABLED)
            {
#if !NOVOCODE || AMBEVOCODE
                byte[] imbe = null;

                // Ensure a vocoder instance exists for the channel
#if !NOVOCODE && !AMBEVOCODE
                var (decoder, encoder, filters) = vocoderManager.GetOrCreateVocoder(dstId, masterConfig.VocoderMode);
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
                    gainControl.Volume = masterConfig.PreEncodeGain + 6;
                    gainControl.Read(chunk, 0, chunk.Length);

                    int smpIdx = 0;
                    short[] samples = new short[chunk.Length / 2];
                    for (int pcmIdx = 0; pcmIdx < chunk.Length; pcmIdx += 2)
                    {
                        samples[smpIdx] = (short)((chunk[pcmIdx + 1] << 8) + chunk[pcmIdx]);
                        smpIdx++;
                    }

                    int tone = 0;

                    if (masterConfig.EnableMbeTones)
                    {
                        float[] fSamples = AudioConverter.PcmToFloat(samples);

                        // Convert to signal
                        DiscreteSignal signal = new DiscreteSignal(waveFormat.SampleRate, fSamples, true);

                        try
                        {
                            tone = toneDetecor.Detect(signal);
                            //logger.Debug($"ANA: {tone} Detected");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error: {ex.Message}");
                        }
                    }

                    if (tone > 0 && masterConfig.EnableMbeTones)
                    {
                        try
                        {
                            if (masterConfig.VocoderMode == VocoderModes.IMBE)
                            {
                                imbe = new byte[11];
                                MBEToneGenerator.IMBEEncodeSingleTone((ushort)tone, imbe);
                            }
                            else
                            {
                                imbe = new byte[9];
                                MBEToneGenerator.AmbeEncodeSingleTone((ushort)tone, (char)120, imbe);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }

                    if (tone == 0)
                    {

#if !AMBEVOCODE
                        if (masterConfig.VocoderMode == VocoderModes.IMBE)
                            imbe = new byte[11];
                        else
                            imbe = new byte[9];
                        
                        encoder.encode(samples, imbe);
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
                    }

                    short[] decodedSamples = new short[320];

                    if (imbe == null)
                        return;

#if !AMBEVOCODE
                    int errors = decoder.decode(imbe, decodedSamples);
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
                        try
                        {
                            //float[] fSamples = AudioConverter.PcmToFloat(decodedSamples);

                            //// Convert PCM samples into a DiscreteSignal
                            //DiscreteSignal signal = new DiscreteSignal(8000, fSamples, true);

                            ////// Apply all filters sequentially
                            ////foreach (var filter in filters)
                            ////{
                            ////    signal = filter.ApplyTo(signal);
                            ////}

                            ////fSamples = AudioConverter.ApplyNoiseGate(fSamples, -50f);

                            //// Convert back to PCM
                            //short[] filtered16 = AudioConverter.FloatToPcm(signal.Samples);

                            int pcmIdx = 0;
                            byte[] pcmData = new byte[decodedSamples.Length * 2];
                            for (int i = 0; i < decodedSamples.Length; i++)
                            {
                                pcmData[pcmIdx] = (byte)(decodedSamples[i] & 0xFF);
                                pcmData[pcmIdx + 1] = (byte)((decodedSamples[i] >> 8) & 0xFF);
                                pcmIdx += 2;
                            }

                            //Console.WriteLine(BitConverter.ToString(pcmData));

                            processedChunks.Add(pcmData);
                        } catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                }

                var combinedAudioData = AudioConverter.CombineChunks(processedChunks);
                if (combinedAudioData != null)
                {
                    audioPacket.AudioMode = AudioMode.PCM_8_16;
                    audioPacket.Data = combinedAudioData;

                    if (!affRestrict)
                        master.BroadcastPacket(audioPacket.GetStrData(), client);
                    else
                        master.BroadcastPacket(audioPacket.GetStrData(), affiliatedClients, client);
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
                if (!affRestrict)
                    master.BroadcastPacket(audioPacket.GetStrData(), client);
                else
                    master.BroadcastPacket(audioPacket.GetStrData(), affiliatedClients, client);
            }
        }

#if !NOVOCODE && !AMBEVOCODE
        private (MBEDecoder Decoder, MBEEncoder Encoder) CreateInternalVocoderInstance()
        {
            var decoder = new MBEDecoder(masterConfig.VocoderMode == VocoderModes.IMBE ? MBE_MODE.IMBE_88BIT : MBE_MODE.DMR_AMBE);
            var encoder = new MBEEncoder(masterConfig.VocoderMode == VocoderModes.IMBE ? MBE_MODE.IMBE_88BIT : MBE_MODE.DMR_AMBE);
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
            vocoderManager.RemoveVocoder(channelId);
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