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
    public class ClientHandler : WebSocketBehavior
    {
        private Config.MasterConfig masterConfig;
        private RidAclManager aclManager;
        private AffiliationsManager affiliationsManager;
        private VoiceChannelManager voiceChannelManager;
        private Reporter reporter;
        private ILogger logger;

#if !NOVOCODE
        private MBEDecoderManaged p25Decoder;
        private MBEEncoderManaged p25Encoder;
#endif

        public ClientHandler(Config.MasterConfig config, RidAclManager aclManager, AffiliationsManager affiliationsManager,
            VoiceChannelManager voiceChannelManager, Reporter reporter,
#if !NOVOCODE
            MBEDecoderManaged p25Decoder, MBEEncoderManaged p25Encoder,
#endif
            ILogger logger)
        {
            this.masterConfig = config;
            this.aclManager = aclManager;
            this.affiliationsManager = affiliationsManager;
            this.voiceChannelManager = voiceChannelManager;
            this.reporter = reporter;
            this.logger = logger;

#if !NOVOCODE
            this.p25Encoder = p25Encoder;
            this.p25Decoder = p25Decoder;
#endif
        }

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
                        BroadcastAudio(data["data"].ToObject<byte[]>(), data["voiceChannel"].ToObject<VoiceChannel>());
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
                logger.Information("Voice channel {Channel} removed for disconnected client {ClientId}", channel, clientId);
            }

            affiliationsManager.RemoveAffiliationByClientId(clientId);
            logger.Information("Affiliations removed for disconnected client {ClientId}", clientId);
        }

        protected override void OnError(ErrorEventArgs e)
        {
            base.OnError(e);
            logger.Error("WebSocket error: {Message}", e.Message);
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            // logger.Information("WebSocket connection opened for client {ClientId}", ID);
        }

        private void HandleEmergencyAlarmRequest(EMRG_ALRM_REQ request)
        {
            logger.Information(request.ToString());
            reporter.Send(PacketType.EMRG_ALRM_REQ, request.SrcId, request.DstId, null);

            var response = new EMRG_ALRM_RSP
            {
                SrcId = request.SrcId,
                DstId = request.DstId
            };

            BroadcastMessage(JsonConvert.SerializeObject(new { type = (int)PacketType.EMRG_ALRM_RSP, data = response }));
            logger.Information(response.ToString());
            reporter.Send(PacketType.EMRG_ALRM_RSP, request.SrcId, request.DstId, null);
        }

        private void HandleCallAlertRequest(CALL_ALRT_REQ request)
        {
            logger.Information(request.ToString());
            reporter.Send(PacketType.CALL_ALRT_REQ, request.SrcId, request.DstId, null);

            var response = new CALL_ALRT
            {
                SrcId = request.SrcId,
                DstId = request.DstId
            };

            BroadcastMessage(JsonConvert.SerializeObject(new { type = (int)PacketType.CALL_ALRT, data = response }));
            logger.Information(response.ToString());
            reporter.Send(PacketType.CALL_ALRT, request.SrcId, request.DstId, null);
        }

        private void HandleGroupAffiliationRequest(GRP_AFF_REQ request)
        {
            logger.Information(request.ToString());
            reporter.Send(PacketType.GRP_AFF_REQ, request.SrcId, request.DstId, null);

            var clientId = ID;
            Affiliation affiliation = new Affiliation(clientId, request.SrcId, request.DstId);

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
            reporter.Send(PacketType.GRP_AFF_RSP, request.SrcId, request.DstId, null, (ResponseType)response.Status);
        }

        private void HandleUnitRegistrationRequest(U_REG_REQ request)
        {
            logger.Information(request.ToString());
            reporter.Send(PacketType.U_REG_REQ, request.SrcId, null, null);

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

            reporter.Send(PacketType.U_REG_RSP, request.SrcId, null, null, (ResponseType)response.Status);
        }

        private void HandleUnitDeRegistrationRequest(U_DE_REG_REQ request)
        {
            logger.Information(request.ToString());
            reporter.Send(PacketType.U_DE_REG_REQ, request.SrcId, null, null);

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
            reporter.Send(PacketType.U_DE_REG_RSP, request.SrcId, null, null, (ResponseType)response.Status);

            Context.WebSocket.Close();
        }

        private void HandleVoiceChannelRequest(GRP_VCH_REQ request)
        {
            logger.Information(request.ToString());
            reporter.Send(PacketType.GRP_VCH_REQ, request.SrcId, request.DstId, null);

            var response = new GRP_VCH_RSP
            {
                DstId = request.DstId,
                SrcId = request.SrcId
            };

            var availableChannel = GetAvailableVoiceChannel();

            if (availableChannel != null && isDestinationPermitted(request.SrcId, request.DstId))
            {
                voiceChannelManager.AddVoiceChannel(new VoiceChannel
                {
                    DstId = request.DstId,
                    SrcId = request.SrcId,
                    Frequency = availableChannel,
                    ClientId = ID,
                    IsActive = true
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

            reporter.Send(PacketType.GRP_VCH_RSP, request.SrcId, request.DstId, null, (ResponseType)response.Status);
        }

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

            reporter.Send(PacketType.GRP_VCH_RLS, request.SrcId, request.DstId, null);
        }

        private bool isAffiliationPermitted(string srcId, string dstId)
        {
            return true; // TODO: Actually use this
        }

        private bool isDestinationPermitted(string srcId, string dstId)
        {
            bool value = true;

            if (voiceChannelManager.IsDestinationActive(dstId))
                value = false;

            // TODO: TG ACL

            return value;
        }

        private bool isRidAuthed(string srcId)
        {
            return aclManager.IsRidAllowed(srcId);
        }

        private string GetAvailableVoiceChannel()
        {
            foreach (var channel in masterConfig.VoiceChannels)
            {
                if (!voiceChannelManager.IsVoiceChannelActive(new VoiceChannel { Frequency = channel }))
                {
                    return channel;
                }
            }
            return null;
        }

        private void BroadcastMessage(string message, bool skipMe = false)
        {
            foreach (var session in Sessions.Sessions)
            {
                if (skipMe && ID == session.ID)
                    return;

                Sessions.SendTo(message, session.ID);
            }
        }

        private void BroadcastAudio(byte[] audioData, VoiceChannel voiceChannel)
        {
            VoiceChannel channel = voiceChannelManager.FindVoiceChannelByDstId(voiceChannel.DstId);

            if (voiceChannel != null && channel.ClientId != ID)
            {
                logger.Warning("Ingoring call; traffic collision srcId: {SrcId}, dstId: {DstId}", voiceChannel.SrcId, voiceChannel.DstId);
                voiceChannelManager.RemoveVoiceChannelByClientId(ID);
                BroadcastMessage(JsonConvert.SerializeObject(new { type = (int)PacketType.GRP_VCH_RLS, data = new GRP_VCH_RLS { DstId = voiceChannel.DstId, SrcId = voiceChannel.SrcId } }));
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
                    BroadcastMessage(JsonConvert.SerializeObject(new { type = (int)PacketType.AUDIO_DATA, data = combinedAudioData, voiceChannel }));
                }
                else
                {
                    logger.Error("Channel not permitted; skipping audio");
                }
#endif
            }
            else
            {
                BroadcastMessage(JsonConvert.SerializeObject(new { type = (int)PacketType.AUDIO_DATA, data = audioData, voiceChannel }));
            }
        }
    }
}