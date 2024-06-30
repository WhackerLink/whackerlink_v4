using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp.Server;
using WhackerLinkServer.Models;

#nullable disable

namespace WhackerLinkServer
{
    public class Master
    {
        private Config.MasterConfig config;
        private WebSocketServer server;

        public Master(Config.MasterConfig config)
        {
            this.config = config;
        }

        public void Start()
        {
            try
            {
                Log.Information("Starting Master {Name}", config.Name);

                server = new WebSocketServer($"ws://localhost:{config.Port}");
                server.AddWebSocketService<ClientHandler>("/client", () => new ClientHandler(config));
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