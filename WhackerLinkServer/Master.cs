/*
 * Copyright (C) 2024-2025 Caleb H. (K4PHP) caleb.k4php@gmail.com
 *
 * This file is part of the WhackerLinkServer project.
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program. If not, see <https://www.gnu.org/licenses/>.
 *
 * DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
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
using WhackerLinkServer.Vocoder;

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
        private AuthKeyFileManager authKeyManager;
        private ILogger logger;

#if !NOVOCODE && !AMBEVOCODE
        private VocoderManager vocoderManager;
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
            this.voiceChannelManager = new VoiceChannelManager(config.DisableVchUpdates);
            this.siteManager = new SiteManager();
            this.logger = LoggerSetup.CreateLogger(config.Name);

            voiceChannelManager.VoiceChannelUpdated += VoiceChannelUpdate;

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
        /// Gets the current auth key manager instance
        /// </summary>
        /// <returns>the insance of <see cref="AuthKeyFileManager"/></returns>
        public AuthKeyFileManager GetAuthManager()
        {
            return authKeyManager;
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
                    aclManager.StartReloadTimer(config.RidAcl.ReloadInterval * 1000);
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

                string serverAddress = $"ws://{config.Address}:{config.Port}";

                if (config.Ssl != null) {
                    serverAddress = config.Ssl.Enabled
                        ? $"wss://{config.Address}:{config.Port}"
                        : $"ws://{config.Address}:{config.Port}";
                }

                server = new WebSocketServer(serverAddress);

                if (config.Ssl != null)
                {
                    if (config.Ssl.Enabled) {
                        if (string.IsNullOrEmpty(config.Ssl.CertificatePath))
                        {
                            logger.Error("SSL is enabled, but no certificate path is provided.");
                            throw new InvalidOperationException("SSL requires a valid certificate path.");
                        }

                        server.SslConfiguration.ServerCertificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                            config.Ssl.CertificatePath,
                            config.Ssl.CertificatePassword);

                        server.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                        logger.Information("SSL Enabled");
                    }
                }

                if (config.Auth != null) 
                    authKeyManager = new AuthKeyFileManager(config.Auth.Path, config.Name, config.Auth.Enabled, logger, config.Auth.ReloadInterval);
                else
                    authKeyManager = new AuthKeyFileManager(string.Empty, config.Name, false, logger, 0);

#if !NOVOCODE && !AMBEVOCODE
                this.vocoderManager = new VocoderManager(logger);
#endif

                var masterInstance = this;

#pragma warning disable CS0618 // Type or member is obsolete
                server.AddWebSocketService<ClientHandler>("/client", () => new ClientHandler(config, aclManager, affiliationsManager, voiceChannelManager, siteManager, reporter,
#if !NOVOCODE && !AMBEVOCODE
                        vocoderManager,
#endif
#if AMBEVOCODE && !NOVOCODE
                        ambeVocoderInstances,
#endif
                        masterInstance,
                        authKeyManager,
                    logger));
#pragma warning restore CS0618 // Type or member is obsolete

                server.Start();

                logger.Information("Master {Name} Listening on port {Port}; address: {Address}", config.Name, config.Port, config.Address);

                if (config.Sites.Count > 0)
                {
                    if (!config.DisableSiteBcast)
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
                    else
                        logger.Information("SITE_BCAST interval not enabled");
                }

                while (!cancellationToken.IsCancellationRequested)
                {
                    Thread.Sleep(1000);
                }
            }
            catch (IOException ex)
            {
                logger.Error(ex, "IO Error occurred while starting the server.");
            }
            catch (ObjectDisposedException ex)
            {
                logger.Warning(ex, "ObjectDisposedException: Attempted to access a disposed object.");
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
        /// Broadcast <see cref="GRP_VCH_UPD"/>
        /// </summary>
        /// <param name="channel"></param>
        public void VoiceChannelUpdate(VoiceChannel channel)
        {
            if (config.DisableVchUpdates)
                return;

            GRP_VCH_UPD packet = new GRP_VCH_UPD { VoiceChannel = channel };
            
            logger.Information(packet.ToString());

            BroadcastPacket(packet.GetStrData());
        }

        /// <summary>
        /// Broadcasts a message to all connected clients (Optionally skip the sender)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="skipClientId"></param>
        public void BroadcastPacket(string message, string skipClientId = null)
        {
            try
            {
                foreach (var path in server.WebSocketServices.Paths)
                {
                    var serviceHost = server.WebSocketServices[path];

                    foreach (var sessionId in serviceHost.Sessions.IDs)
                    {
                        if (skipClientId != null && sessionId == skipClientId)
                            continue;

                        serviceHost.Sessions.SendTo(message, sessionId);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to broadcast message from master");
            }
        }

        /// <summary>
        /// Broadcasts a message to a specific list of client IDs
        /// </summary>
        /// <param name="message"></param>
        /// <param name="clientIds"></param>
        /// <param name="skipClientId"></param>
        public void BroadcastPacket(string message, List<string> clientIds, string skipClientId = null)
        {
            try
            {
                foreach (var path in server.WebSocketServices.Paths)
                {
                    var serviceHost = server.WebSocketServices[path];
                    foreach (var clientId in clientIds)
                    {
                        if (skipClientId != null && clientId == skipClientId)
                            continue;

                        if (serviceHost.Sessions.TryGetSession(clientId, out var session))
                        {
                            serviceHost.Sessions.SendTo(message, clientId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to broadcast message to specific clients from master");
            }
        }

        /// <summary>
        /// Stops the master server and all components
        /// </summary>
        public void Stop()
        {
            try
            {
                logger.Information("Stopping Master {Name}", config.Name);

                server?.Stop();
                restServer?.Stop();

                server = null;
                restServer = null;
                reporter = null;
                authKeyManager = null;

                this.aclManager = new RidAclManager(config.RidAcl.Enabled);
                this.affiliationsManager = new AffiliationsManager();
                this.voiceChannelManager = new VoiceChannelManager(config.DisableVchUpdates);
                this.siteManager = new SiteManager();

                logger.Information("Master stopped successfully");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while stopping the master server");
            }
        }

        /// <summary>
        /// Restarts the master server
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        public void Restart(CancellationToken cancellationToken)
        {
            Stop();
            Start(cancellationToken);
        }
    }
}

