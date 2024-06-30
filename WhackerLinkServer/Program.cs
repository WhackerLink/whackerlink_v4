using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using WebSocketSharp;
using WebSocketSharp.Server;
using WhackerLinkCommonLib;
using WhackerLinkCommonLib.Models;
using WhackerLinkCommonLib.Models.IOSP;
using WhackerLinkServer.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

#nullable disable

namespace WhackerLinkServer
{
    class Program
    {
        internal static Config config;
        internal static WebSocketServer server;
        internal static Dictionary<string, VoiceChannel> activeVoiceChannels = new Dictionary<string, VoiceChannel>();

        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/whackerlink-.log", rollingInterval: RollingInterval.Day, outputTemplate: "{Level:u1}: {Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Message}{NewLine}{Exception}")
                .CreateLogger();

            try
            {
                Log.Information("Starting WhackerLink Server...");

                string configPath = "config.yml";
                if (args.Length > 0 && args[0] == "-c" && args.Length > 1)
                {
                    configPath = args[1];
                }

                config = LoadConfig(configPath);
                if (config == null)
                {
                    Log.Error("Failed to load config.");
                    return;
                }

                server = new WebSocketServer($"ws://localhost:{config.System.NetworkBindPort}");
                server.AddWebSocketService<ClientHandler>("/client");
                server.Start();

                Log.Information("Server started. Listening on port {Port}", config.System.NetworkBindPort);

                await Task.Delay(-1);
            }
            catch (IOException ex)
            {
                Log.Error(ex, "IO Error");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "An unhandled exception occurred.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static Config LoadConfig(string path)
        {
            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                var yaml = File.ReadAllText(path);
                return deserializer.Deserialize<Config>(yaml);
            }
            catch (Exception ex)
            {
                Log.Error("Error loading config: {Message}", ex.Message);
                return null;
            }
        }
    }

    public class ClientHandler : WebSocketBehavior
    {
        protected override void OnMessage(MessageEventArgs e)
        {
            try
            {
                var data = JObject.Parse(e.Data);
                var type = data["type"].ToString();

                switch (type)
                {
                    case "U_REG_REQ":
                        HandleUnitRegistrationRequest(data["data"].ToObject<U_REG_REQ>());
                        break;
                    case "GRP_AFF_REQ":
                        HandleGroupAffiliationRequest(data["data"].ToObject<GRP_AFF_REQ>());
                        break;
                    case "GRP_VCH_REQ":
                        HandleVoiceChannelRequest(data["data"].ToObject<GRP_VCH_REQ>());
                        break;
                    case "GRP_VCH_RLS":
                        HandleVoiceChannelRelease(data["data"].ToObject<GRP_VCH_RLS>());
                        break;
                    case "audio_data":
                        BroadcastAudio(data["data"].ToObject<byte[]>());
                        break;
                    default:
                        Serilog.Log.Warning("Unknown message type: {Type}", type);
                        break;
                }
            }
            catch (IOException ex)
            {
                Serilog.Log.Error(ex, "IOException occurred while processing message.");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "An error occurred while processing message.");
            }
        }

        private void HandleGroupAffiliationRequest(GRP_AFF_REQ request)
        {
            Serilog.Log.Information(request.ToString());

            var response = new GRP_AFF_RSP
            {
                SrcId = request.SrcId,
                DstId = request.DstId,
                SysId = request.SysId
            };

            if (isDestinationPerimitted(request.SrcId, request.DstId))
            {
                response.Status = (int)ResponseType.GRANT;
            }
            else
            {
                response.Status = (int)ResponseType.DENY;
            }

            Send(JsonConvert.SerializeObject(new { type = "U_REG_RSP", data = response }));
            Serilog.Log.Information(response.ToString());
        }

        private void HandleUnitRegistrationRequest(U_REG_REQ request)
        {
            Serilog.Log.Information(request.ToString());

            var response = new U_REG_RSP
            {
                SrcId = request.SrcId,
                SysId = request.SysId,
                Wacn = request.Wacn
            };

            if (isRidAuthed(request.SrcId))
            {
                response.Status = (int)ResponseType.GRANT;
            } else
            {
                response.Status = (int)ResponseType.REFUSE;
            }

            Send(JsonConvert.SerializeObject(new { type = "U_REG_RSP", data = response }));
            Serilog.Log.Information(response.ToString());
        }

        private void HandleVoiceChannelRequest(GRP_VCH_REQ request)
        {
            Serilog.Log.Information(request.ToString());

            var response = new GRP_VCH_RSP
            {
                DstId = request.DstId,
                SrcId = request.SrcId
            };

            var availableChannel = GetAvailableVoiceChannel();
            if (availableChannel != null)
            {
                Program.activeVoiceChannels[availableChannel] = new VoiceChannel
                {
                    DstId = request.DstId,
                    SrcId = request.SrcId,
                    Frequency = availableChannel
                };

                response.Channel = availableChannel;
                response.Status = (int)ResponseType.GRANT;

                Send(JsonConvert.SerializeObject(new { type = "GRP_VCH_RSP", data = response }));
                Serilog.Log.Information(response.ToString());
            }
            else
            {
                response.Status = (int)ResponseType.DENY;

                Send(JsonConvert.SerializeObject(new { type = "GRP_VCH_RSP", data = response }));
                Serilog.Log.Information(response.ToString());
            }
        }

        private void HandleVoiceChannelRelease(GRP_VCH_RLS request)
        {
            Serilog.Log.Information(request.ToString());

            if (Program.activeVoiceChannels.TryGetValue(request.Channel, out var channel))
            {
                Program.activeVoiceChannels.Remove(request.Channel);
                Serilog.Log.Information("Voice channel {Channel} released for {SrcId} to {DstId}", request.Channel, request.SrcId, request.DstId);
            }
            else
            {
                Serilog.Log.Warning("Voice channel {Channel} not found to release", request.Channel);
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
            foreach (var channel in Program.config.System.VoiceChannels)
            {
                if (!Program.activeVoiceChannels.ContainsKey(channel))
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
                    Sessions.SendTo(JsonConvert.SerializeObject(new { type = "audio_data", data = audioData }), session.ID);
                }
            }
        }
    }
}