using Serilog;
using WebSocketSharp.Server;
using WhackerLinkServer.Models;

#nullable disable

namespace WhackerLinkServer
{
    public class Master
    {
        private Config.MasterConfig config;
        private WebSocketServer server;
        private RidAclManager aclManager = new RidAclManager();

        public Master(Config.MasterConfig config)
        {
            this.config = config;
        }

        public void Start()
        {
            try
            {
                Log.Information("Starting Master {Name}", config.Name);

                aclManager.Load(config.RidAclPath);
                
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
}