using Serilog;
using WebSocketSharp.Server;
using WhackerLinkCommonLib.Interfaces;
using WhackerLinkServer.Models;
using WhackerLinkServer;
using WhackerLinkServer.Managers;
using WhackerLinkCommonLib.Models;

#nullable disable

public class Master : IMasterService
{
    private Config.MasterConfig config;
    private WebSocketServer server;
    private RidAclManager aclManager = new RidAclManager();
    private RestApiServer restServer;

    public Master(Config.MasterConfig config)
    {
        this.config = config;
    }

    public List<Affiliation> GetAffiliations()
    {
        return AffiliationsManager.Instance.GetAffiliations();
    }

    public List<VoiceChannel> GetVoiceChannels()
    {
        return VoiceChannelManager.Instance.GetVoiceChannels();
    }

    public void Start()
    {
        try
        {
            Log.Information("Starting Master {Name}", config.Name);

            aclManager.Load(config.RidAclPath);

            if (config.Rest.Enabled)
            {
                restServer = new RestApiServer(this, config.Rest.Address, config.Rest.Port);
                restServer.Start();
            }

            server = new WebSocketServer($"ws://{config.Address}:{config.Port}");
            server.AddWebSocketService<ClientHandler>("/client", () => new ClientHandler(config, aclManager));
            server.Start();

            Log.Information("Master {Name} Listening on port {Port}", config.Name, config.Port);
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
}