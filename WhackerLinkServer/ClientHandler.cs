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
* Copyright (C) 2024 Caleb, KO4UYJ
* 
*/

using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WhackerLinkServer.Models;
using WhackerLinkCommonLib.Models;
using WhackerLinkCommonLib.Models.IOSP;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;
using WhackerLinkServer.Managers;
using Serilog;
using WhackerLinkCommonLib.Utils;
using Nancy;

#if !NOVOCODE
using vocoder;
#endif

#nullable disable

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

#if !NOVOCODE
        private MBEDecoderManaged p25Decoder;
        private MBEEncoderManaged p25Encoder;
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
#if !NOVOCODE
            MBEDecoderManaged p25Decoder, MBEEncoderManaged p25Encoder,
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

#if !NOVOCODE
            this.p25Encoder = p25Encoder;
            this.p25Decoder = p25Decoder;
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
                    case (int)PacketType.AUDIO_DATA:
                        BroadcastAudio(data["data"].ToObject<byte[]>(), data["voiceChannel"].ToObject<VoiceChannel>(), data["site"].ToObject<Site>());
                        break;
                    default:
                        logger.Warning("Unknown message type: {Type}", type);
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

            foreach (var channel in channelsToRemove)
            {
                voiceChannelManager.RemoveVoiceChannel(channel);
                BroadcastMessage(JsonConvert.SerializeObject(new GRP_VCH_RLS {
                    SrcId = voiceChannelManager.FindVoiceChannelByClientId(ID).SrcId,
                    DstId = voiceChannelManager.FindVoiceChannelByClientId(ID).DstId,
                    Channel = voiceChannelManager.FindVoiceChannelByClientId(ID).Frequency
                }));
                logger.Information("Voice channel {Channel} removed for disconnected client {ClientId}", channel, clientId);
            }

            var affiliations = affiliationsManager.GetAffiliationsByClientId(clientId);
            foreach (var affiliation in affiliations)
            {
                if (talkgroupHangTimers.ContainsKey(affiliation.DstId))
                {
                    talkgroupHangTimers[affiliation.DstId].Dispose();
                    talkgroupHangTimers.Remove(affiliation.DstId);
                }
            }

            affiliationsManager.RemoveAffiliationByClientId(clientId);
            logger.Information("Affiliations removed for disconnected client {ClientId}", clientId);
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
            reporter.Send(PacketType.EMRG_ALRM_REQ, request.SrcId, request.DstId, request.Site, null);

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
            }
            else
            {
                if (request.Channel.IsNullOrEmpty())
                {
                    logger.Warning("Removing channel grant for {DstId} due to the voice channel being null", request.DstId); // TODO: Not 100% if this is a proper fix, but it seems to work
                    BroadcastMessage(JsonConvert.SerializeObject(new { type = (int)PacketType.GRP_VCH_RLS, data = new GRP_VCH_RLS { SrcId = request.SrcId, DstId = request.DstId } }));
                    voiceChannelManager.RemoveVoiceChannelByDstId(request.DstId);
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
        private void BroadcastAudio(byte[] audioData, VoiceChannel voiceChannel, Site site)
        {
            VoiceChannel channel = voiceChannelManager.FindVoiceChannelByDstId(voiceChannel.DstId);

            if (!voiceChannelManager.IsDestinationActive(voiceChannel.DstId))
            {
                logger.Warning("Ignoring call; destination not permitted for traffic srcId: {SrcId}, dstId: {DstId}", voiceChannel.SrcId, voiceChannel.DstId);
                return;
            }

            if (voiceChannel != null && channel.ClientId != ID)
            {
                logger.Warning("Ignoring call; traffic collision srcId: {SrcId}, dstId: {DstId}", voiceChannel.SrcId, voiceChannel.DstId);
                voiceChannelManager.RemoveVoiceChannelByClientId(ID);
                BroadcastMessage(JsonConvert.SerializeObject(new { type = (int)PacketType.GRP_VCH_RLS, data = new GRP_VCH_RLS { DstId = voiceChannel.DstId, SrcId = voiceChannel.SrcId, Site = site } }));
                return;
            }

            if (masterConfig.VocoderMode != VocoderModes.DISABLED)
            {
#if !NOVOCODE
                byte[] imbe = null;

                if (p25Encoder == null || p25Decoder == null)
                {
                    Console.WriteLine("Vocoder is not initialized; This should not happen.");
                    return;
                }

                var chunks = AudioConverter.SplitToChunks(audioData);
                if (chunks.Count == 0)
                {
                    Console.WriteLine("Invalid audio data length for conversion.");
                    return;
                }

                List<byte[]> processedChunks = new List<byte[]>();

                foreach (var chunk in chunks)
                {
                    int smpIdx = 0;
                    short[] samples = new short[chunk.Length / 2];
                    for (int pcmIdx = 0; pcmIdx < chunk.Length; pcmIdx += 2)
                    {
                        samples[smpIdx] = (short)((chunk[pcmIdx + 1] << 8) + chunk[pcmIdx]);
                        smpIdx++;
                    }

                    p25Encoder.encode(samples, out imbe);

                    short[] samp2 = null;
                    int errs = p25Decoder.decode(imbe, out samp2);
                    if (samples != null)
                    {
                        int pcmIdx = 0;
                        byte[] pcm2 = new byte[samp2.Length * 2];
                        for (int smpIdx2 = 0; smpIdx2 < samp2.Length; smpIdx2++)
                        {
                            pcm2[pcmIdx] = (byte)(samp2[smpIdx2] & 0xFF);
                            pcm2[pcmIdx + 1] = (byte)((samp2[smpIdx2] >> 8) & 0xFF);
                            pcmIdx += 2;
                        }

                        processedChunks.Add(pcm2);
                    }
                }

                var combinedAudioData = AudioConverter.CombineChunks(processedChunks);
                if (combinedAudioData != null)
                {
                    BroadcastMessage(JsonConvert.SerializeObject(new { type = (int)PacketType.AUDIO_DATA, data = combinedAudioData, voiceChannel, site }));
                }
                else
                {
                    logger.Error("Channel not permitted; skipping audio");
                }
#endif
            }
            else
            {
                BroadcastMessage(JsonConvert.SerializeObject(new { type = (int)PacketType.AUDIO_DATA, data = audioData, voiceChannel, site }));
            }
        }
    }
}