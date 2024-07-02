using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WhackerLinkServer.Models;
using WhackerLinkCommonLib.Models;
using WhackerLinkCommonLib.Models.IOSP;

#nullable disable

namespace WhackerLinkServer
{
    public class ClientHandler : WebSocketBehavior
    {
        internal static Dictionary<string, VoiceChannel> activeVoiceChannels = new Dictionary<string, VoiceChannel>();
        private Config.MasterConfig masterConfig;
        private RidAclManager aclManager;

        private AffiliationsManager affiliationsManager = new AffiliationsManager();

        public ClientHandler(Config.MasterConfig config, RidAclManager aclManager)
        {
            this.masterConfig = config;
            this.aclManager = aclManager;
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
                    case (int)PacketType.AUDIO_DATA:
                        BroadcastAudio(data["data"].ToObject<byte[]>(), data["voiceChannel"].ToObject<VoiceChannel>());
                        break;
                    default:
                        Program.logger.Warning("Unknown message type: {Type}", type);
                        break;
                }
            }
            catch (IOException ex)
            {
                Program.logger.Error(ex, "IOException occurred while processing message.");
            }
            catch (Exception ex)
            {
                Program.logger.Error(ex, "An error occurred while processing message.");           
            }
        }

        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);

            var clientId = ID;

            var channelsToRemove = activeVoiceChannels.Values
                .Where(vc => vc.ClientId == clientId)
                .Select(vc => vc.Frequency)
                .ToList();

            foreach (var channel in channelsToRemove)
            {
                activeVoiceChannels.Remove(channel);
                Program.logger.Information("Voice channel {Channel} removed for disconnected client {ClientId}", channel, clientId);
            }

            affiliationsManager.RemoveAffiliationByClientId(clientId);
            Program.logger.Information("Affiliations removed for disconnected client {ClientId}", clientId);
        }

        private void HandleGroupAffiliationRequest(GRP_AFF_REQ request)
        {
            Program.logger.Information(request.ToString());

            var clientId = ID;
            Affiliation affiliation = new Affiliation(clientId, request.SrcId, request.DstId);

            var response = new GRP_AFF_RSP
            {
                SrcId = request.SrcId,
                DstId = request.DstId,
                SysId = request.SysId
            };

            if (isDestinationPermitted(request.SrcId, request.DstId))
            {
                response.Status = (int)ResponseType.GRANT;
                affiliationsManager.AddAffiliation(affiliation);
            }
            else
            {
                response.Status = (int)ResponseType.DENY;
                affiliationsManager.RemoveAffiliation(affiliation);
            }

            Send(JsonConvert.SerializeObject(new { type = (int)PacketType.GRP_AFF_RSP, data = response }));
            Program.logger.Information(response.ToString());
        }

        private void HandleUnitRegistrationRequest(U_REG_REQ request)
        {
            Program.logger.Information(request.ToString());

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
                Program.logger.Warning("RID ACL Rejection, {srcId}", request.SrcId);
                response.Status = (int)ResponseType.REFUSE;
            }

            Send(JsonConvert.SerializeObject(new { type = (int)PacketType.U_REG_RSP, data = response }));
            Program.logger.Information(response.ToString());
        }

        private void HandleUnitDeRegistrationRequest(U_DE_REG_REQ request)
        {
            Program.logger.Information(request.ToString());

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
            }
            else
            {
                Program.logger.Warning("RID ACL Rejection, {srcId}", request.SrcId);
                response.Status = (int)ResponseType.FAIL;
            }

            Send(JsonConvert.SerializeObject(new { type = (int)PacketType.U_DE_REG_RSP, data = response }));
            Program.logger.Information(response.ToString());

            Context.WebSocket.Close();
        }

        private void HandleVoiceChannelRequest(GRP_VCH_REQ request)
        {
            Program.logger.Information(request.ToString());

            var response = new GRP_VCH_RSP
            {
                DstId = request.DstId,
                SrcId = request.SrcId
            };

            var availableChannel = GetAvailableVoiceChannel();
            if (availableChannel != null)
            {
                activeVoiceChannels[availableChannel] = new VoiceChannel
                {
                    DstId = request.DstId,
                    SrcId = request.SrcId,
                    Frequency = availableChannel,
                    ClientId = ID
                };

                response.Channel = availableChannel;
                response.Status = (int)ResponseType.GRANT;

                BroadcastMessage(JsonConvert.SerializeObject(new { type = (int)PacketType.GRP_VCH_RSP, data = response }));
                Program.logger.Information(response.ToString());
            }
            else
            {
                response.Status = (int)ResponseType.DENY;

                BroadcastMessage(JsonConvert.SerializeObject(new { type = (int)PacketType.GRP_VCH_RSP, data = response }));
                Program.logger.Information(response.ToString());
            }
        }

        private void HandleVoiceChannelRelease(GRP_VCH_RLS request)
        {
            Program.logger.Information(request.ToString());

            GRP_VCH_RLS response = new GRP_VCH_RLS
            {
                SrcId = request.SrcId,
                DstId = request.DstId,
                Channel = request.Channel
            };

            if (activeVoiceChannels.TryGetValue(request.Channel, out var channel))
            {
                activeVoiceChannels.Remove(request.Channel);
                BroadcastMessage(JsonConvert.SerializeObject(new { type = (int)PacketType.GRP_VCH_RLS, data = response }));
                Program.logger.Information("Voice channel {Channel} released for {SrcId} to {DstId}", request.Channel, request.SrcId, request.DstId);
            }
            else
            {
                Program.logger.Warning("Voice channel {Channel} not found to release", request.Channel);
            }
        }

        private bool isDestinationPermitted(string srcId, string dstId)
        {
            return true;
        }

        private bool isRidAuthed(string srcId)
        {
            return aclManager.IsRidAllowed(srcId);
        }

        private string GetAvailableVoiceChannel()
        {
            foreach (var channel in masterConfig.VoiceChannels)
            {
                if (!activeVoiceChannels.ContainsKey(channel))
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
            BroadcastMessage(JsonConvert.SerializeObject(new { type = (int)PacketType.AUDIO_DATA, data = audioData, voiceChannel }));
        }
    }
}