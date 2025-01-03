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
* Copyright (C) 2024-2025 Caleb, K4PHP
* 
*/

using Serilog;
using WebSocketSharp.Server;
using WhackerLinkServer.Models;
using WhackerLinkServer.Managers;
using WhackerLinkLib.Models;
using WhackerLinkLib.Interfaces;
using System.Reflection;
using WhackerLinkLib.Models.IOSP;

using WhackerLinkLib.Utils;


#if !NOVOCODE
using vocoder;
#endif

namespace WhackerLinkServer
{
    /// <summary>
    /// Class WhackerLink Master
    /// </summary>
    public class Master : IMasterService
    {
        private Config.MasterConfig config;
        private WebSocketServer server;
        private RidAclManager aclManager;
        private RestApiServer restServer;
        private Reporter reporter;
        private AffiliationsManager affiliationsManager;
        private VoiceChannelManager voiceChannelManager;
        private SiteManager siteManager;
        private Timer aclReloadTimer;
        private ILogger logger;

#if !NOVOCODE && !AMBEVOCODE
        private Dictionary<string, (MBEDecoderManaged Decoder, MBEEncoderManaged Encoder)> vocoderInstances = 
            new Dictionary<string, (MBEDecoderManaged, MBEEncoderManaged)>();
#endif
#if AMBEVOCODE && !NOVOCODE
        private Dictionary<string, (AmbeVocoderManager FullRate, AmbeVocoderManager HalfRate)> ambeVocoderInstances =
            new Dictionary<string, (AmbeVocoderManager, AmbeVocoderManager)>();
#endif

        /// <summary>
        /// Creates an instance of the Master class
        /// </summary>
        /// <param name="config"></param>
        public Master(Config.MasterConfig config)
        {
            this.config = config;
            this.aclManager = new RidAclManager(config.RidAcl.Enabled);
            this.affiliationsManager = new AffiliationsManager();
            this.voiceChannelManager = new VoiceChannelManager();
            this.siteManager = new SiteManager();
            this.logger = LoggerSetup.CreateLogger(config.Name);

#if !NOVOCODE && !AMBEVOCODE
            if (config.VocoderMode == VocoderModes.DMRAMBE || config.VocoderMode == VocoderModes.IMBE)
            {
                logger.Information($"{config.VocoderMode} Vocoder mode enabled");
            }
            else
            {
                logger.Information("Vocoding disabled");
            }
#endif
#if AMBEVOCODE
#pragma warning disable SYSLIB0012 // Type or member is obsolete
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
#pragma warning restore SYSLIB0012 // Type or member is obsolete
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);

            if (File.Exists(Path.Combine(new string[] { Path.GetDirectoryName(path), "AMBE.DLL" })))
            {
                logger.Information($"Using EXTERNAL {config.VocoderMode} Vocoder");
            }
            else
            {
                logger.Error("ERROR: AMBE.DLL DOES NOT EXIST!");
            }
#endif
        }

        /// <summary>
        /// Instance of Logger
        /// </summary>
        public ILogger Logger { get { return logger; } }

        /// <summary>
        /// Gets current affiliations list
        /// </summary>
        /// <returns></returns>
        public List<Affiliation> GetAffiliations()
        {
            return affiliationsManager.GetAffiliations();
        }

        /// <summary>
        /// Gets a list of voice active channels
        /// </summary>
        /// <returns></returns>
        public List<VoiceChannel> GetVoiceChannels()
        {
            return voiceChannelManager.GetVoiceChannels();
        }

        /// <summary>
        /// Gets a list of sites
        /// </summary>
        /// <returns></returns>
        public List<Site> GetSites()
        {
            return config.Sites;
        }

        /// <summary>
        /// Gets the current RID ACL
        /// </summary>
        /// <returns></returns>
        public List<RidAclEntry> GetRidAcl()
        {
            return aclManager.ridAclEntries;
        }

        /// <summary>
        /// Gets if the RID ACL is enabled
        /// </summary>
        /// <returns></returns>
        public bool GetRidAclEnabled()
        {
            return aclManager.GetAclEnabled();
        }

        /// <summary>
        /// Starts the master
        /// </summary>
        /// <param name="cancellationToken"></param>
        public void Start(CancellationToken cancellationToken)
        {
            try
            {
                logger.Information("Starting Master {Name}", config.Name);

                aclManager.Load(config.RidAcl.Path);

                if (config.RidAcl.ReloadInterval > 0)
                {
                    aclReloadTimer = new Timer(ReloadAclFile, null, 0, config.RidAcl.ReloadInterval * 1000);
                }
                else
                {
                    logger.Information("ACL Auto reload disabled");
                }

                if (config.Rest.Enabled)
                {
                    restServer = new RestApiServer(this, config.Rest.Address, config.Rest.Port);
                    restServer.Start();
                }

                reporter = new Reporter(config.Reporter.Address, config.Reporter.Port, logger, config.Reporter.Enabled);

                foreach (var site in config.Sites)
                {
                    siteManager.AddSite(site);
                }

                server = new WebSocketServer($"ws://{config.Address}:{config.Port}");
#pragma warning disable CS0618 // Type or member is obsolete
                server.AddWebSocketService<ClientHandler>("/client", () => new ClientHandler(config, aclManager, affiliationsManager, voiceChannelManager, siteManager, reporter,
#if !NOVOCODE && !AMBEVOCODE
                        vocoderInstances,
#endif
#if AMBEVOCODE && !NOVOCODE
                        ambeVocoderInstances,
#endif
                    logger));
#pragma warning restore CS0618 // Type or member is obsolete
                server.Start();

                logger.Information("Master {Name} Listening on port {Port}", config.Name, config.Port);

                if (config.Sites.Count > 0)
                {
                    IntervalRunner siteBcastInterval = new IntervalRunner();
                    SITE_BCAST siteBcast = new SITE_BCAST();

                    siteBcast.Sites = config.Sites;

                    logger.Information("Started SITE_BCAST interval");

                    siteBcastInterval.Start(() =>
                    {
                        // BroadcastMessage(siteBcast.GetStrData());
                        reporter.Send(PacketType.SITE_BCAST, siteBcast);
                    }, 5000);
                }

                while (!cancellationToken.IsCancellationRequested)
                {
                    Thread.Sleep(1000);
                }
            }
            catch (IOException ex)
            {
                logger.Error(ex, "IO Error");
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "An unhandled exception occurred.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        /// <summary>
        /// Reload the ACL file
        /// </summary>
        /// <param name="state"></param>
        private void ReloadAclFile(object state)
        {
            try
            {
                aclManager.Load(config.RidAcl.Path);
                logger.Information("Reloaded RID ACL entries from {Path}", config.RidAcl.Path);
            }
            catch (Exception ex)
            {
                logger.Error("Error reloading RID ACL: {Message}", ex.Message);
            }
        }
    }
}