using Serilog;
using WebSocketSharp.Server;
using WhackerLinkCommonLib.Interfaces;
using WhackerLinkServer.Models;
using WhackerLinkServer.Managers;
using WhackerLinkCommonLib.Models;
using System;
using WhackerLinkServer;

#nullable disable

public class Master : IMasterService
{
    private static readonly Lazy<Master> lazyInstance = new Lazy<Master>(() => new Master());

    public static Master Instance => lazyInstance.Value;

    private Config.MasterConfig config;
    private WebSocketServer server;
    private RidAclManager aclManager;
    private RestApiServer restServer;
    private Thread aclReloadThread;

    private Master() { }

    public void Initialize(Config.MasterConfig config)
    {
        if (this.config != null)
        {
            throw new InvalidOperationException("Master is already initialized.");
        }
        this.config = config;
        this.aclManager = new RidAclManager(config.RidAcl.Enabled);
    }

    public List<Affiliation> GetAffiliations()
    {
        return AffiliationsManager.Instance.GetAffiliations();
    }

    public List<VoiceChannel> GetVoiceChannels()
    {
        return VoiceChannelManager.Instance.GetVoiceChannels();
    }

    public List<string> GetAvailableVoiceChannels()
    {
        return config.VoiceChannels;
    }

    public void Start()
    {
        try
        {
            Log.Information("Starting Master {Name}", config.Name);

            aclManager.Load(config.RidAcl.Path);

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