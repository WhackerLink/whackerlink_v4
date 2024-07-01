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

        private AffiliationsManager affiliationsManager = new AffiliationsManager();

        public ClientHandler(Config.MasterConfig config)
        {
            this.masterConfig = config;
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
                        BroadcastAudio(data["data"].ToObject<byte[]>());
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

        private void HandleGroupAffiliationRequest(GRP_AFF_REQ request)
        {
            Program.logger.Information(request.ToString());

            Affiliation affiliation = new Affiliation(request.SrcId, request.DstId);

            var response = new GRP_AFF_RSP
            {
                SrcId = request.SrcId,
                DstId = request.DstId,
                SysId = request.SysId
            };

            if (isDestinationPerimitted(request.SrcId, request.DstId))
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
                    Frequency = availableChannel
                };

                response.Channel = availableChannel;
                response.Status = (int)ResponseType.GRANT;

                Send(JsonConvert.SerializeObject(new { type = (int)PacketType.GRP_VCH_RSP, data = response }));
                Program.logger.Information(response.ToString());
            }
            else
            {
                response.Status = (int)ResponseType.DENY;

                Send(JsonConvert.SerializeObject(new { type = (int)PacketType.GRP_VCH_RSP, data = response }));
                Program.logger.Information(response.ToString());
            }
        }

        private void HandleVoiceChannelRelease(GRP_VCH_RLS request)
        {
            Program.logger.Information(request.ToString());

            if (activeVoiceChannels.TryGetValue(request.Channel, out var channel))
            {
                activeVoiceChannels.Remove(request.Channel);
                Program.logger.Information("Voice channel {Channel} released for {SrcId} to {DstId}", request.Channel, request.SrcId, request.DstId);
            }
            else
            {
                Program.logger.Warning("Voice channel {Channel} not found to release", request.Channel);
            }
        }

        private bool isDestinationPerimitted(string srcId, string dstId)
        {
            return true;
        }

        private bool isRidAuthed(string srcId)
        {
            return true;
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

        private void BroadcastAudio(byte[] audioData)
        {
            foreach (var session in Sessions.Sessions)
            {
                if (ID != session.ID)
                {
                    Sessions.SendTo(JsonConvert.SerializeObject(new { type = (int)PacketType.AUDIO_DATA, data = audioData }), session.ID);
                }
            }
        }
    }
}