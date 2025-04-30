/*
 * Copyright (C) 2024-2025 Caleb H. (K4PHP) caleb.k4php@gmail.com
 * Copyright (C) 2025 Firav (firavdev@gmail.com)
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
using WhackerLinkLib.Interfaces;
using WhackerLinkServer.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WhackerLinkServer
{
    /// <summary>
    /// App Entry class
    /// </summary>
    class Program
    {
        internal static Config config;
        public static ILogger logger;
        private static List<Task> masterTasks = new List<Task>();
        private static List<IMasterService> masters = new List<IMasterService>();
        private static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private static string configPath = "config.yml";

        public static RestApiServer restServer { get; private set; }

        /// <summary>
        /// App entry point
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            logger = LoggerSetup.CreateLogger(string.Empty);

            try
            {
                if (args.Length > 0 && args[0] == "-c" && args.Length > 1)
                {
                    configPath = args[1];
                }

                config = LoadConfig(configPath);
                if (config == null)
                {
                    logger.Error("Failed to load config.");
                    return;
                }

                string debug = string.Empty;

#if DEBUG
                debug = "DEBUG_PROTO_LABTOOL";
#endif

                logger.Information("WhackerLink Server - Main networking router and handler for WhackerLink");
                logger.Information($"Server Version {System.Reflection.ThisAssembly.Git.Commit} Dirty: {System.Reflection.ThisAssembly.Git.IsDirtyString} {debug}");
                logger.Information("Copyright (C) 2024-2025 Caleb H., K4PHP (_php_)");

                logger.Information("Initializing Master instances");

                foreach (Config.MasterConfig masterConfig in config.Masters)
                {
                    IMasterService master = new Master(masterConfig);
                    masters.Add(master);
                    masterTasks.Add(Task.Run(() => master.Start(cancellationTokenSource.Token)));
                }


                if (config.System.Rest.Enabled)
                {
                    logger.Information("Starting REST server");
                    restServer = new RestApiServer(masters, config.System.Rest.Address, config.System.Rest.Port);
                    restServer.Start();
                }

                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    logger.Information("Shutting down...");
                    Shutdown();
                };

                await Task.WhenAll(masterTasks);
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
        /// Helper to load config file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static Config LoadConfig(string path)
        {
            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
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


        /// <summary>
        /// Helper to save config file
        /// </summary>
        /// <param name="path"></param>
        /// <param name="config"></param>
        private static void SaveConfig(string path, Config config)
        {
            try
            {
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                var yaml = serializer.Serialize(config);
                File.WriteAllText(path, yaml);
            }
            catch (Exception ex)
            {
                logger.Error("Error saving config: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Adds a new Master instance and starts it.
        /// </summary>
        /// <param name="masterConfig">The configuration for the new Master</param>
        /// <returns>True if the operation succeeded, otherwise false</returns>
        public static bool AddNewMaster(Config.MasterConfig masterConfig)
        {
            if (masterConfig == null)
            {
                logger.Error("MasterConfig is null.");
                return false;
            }

            try
            {
                IMasterService master = new Master(masterConfig);
                masters.Add(master);
                masterTasks.Add(Task.Run(() => master.Start(cancellationTokenSource.Token)));

                // Add the new master to the config and save it
                config.Masters.Add(masterConfig);
                SaveConfig(configPath, config);
                restServer?.AddMaster(master);


                return true;
            }
            catch (Exception ex)
            {
                logger.Error("Error in AddNewMaster: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Adds a new Master instance and starts it.
        /// </summary>
        /// <param name="masterConfig">The configuration for the updated Master</param>
        /// <param name="masterName">The name of the Master to update</param>
        /// <returns>True if the operation succeeded, otherwise false</returns>
        public static bool UpdateMaster(string masterName, Config.MasterConfig masterConfig)
        {
            if (masterConfig == null)
            {
                logger.Error("MasterConfig is null.");
                return false;
            }

            if (string.IsNullOrEmpty(masterName))
            {
                logger.Error("Master name is null or empty.");
                return false;
            }

            var master = masters.FirstOrDefault(m => m.Name == masterName);
            if (master == null)
            {
                logger.Warning($"No Master found with the name: {masterName}");
                return false;
            }

            try
            {
                // Remove the master from the list
                master.Stop();
                masters.Remove(master);
                restServer?.RemoveMaster(masterName);

                // Remove the task associated with the master
                var masterTask = masterTasks.FirstOrDefault(t => t.IsCompleted || t.IsFaulted || t.IsCanceled);
                if (masterTask != null)
                {
                    masterTasks.Remove(masterTask);
                }

                master = new Master(masterConfig);
                masters.Add(master);
                masterTasks.Add(Task.Run(() => master.Start(cancellationTokenSource.Token)));

                // Add the new master to the config and save it
                var oldMasterConfig = config.Masters.FirstOrDefault(m => m.Name == masterName);
                config.Masters.Remove(oldMasterConfig);
                config.Masters.Add(masterConfig);
                SaveConfig(configPath, config);

                logger.Information($"Master {masterName} has been updated.");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("Error in AddNewMaster: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Stops a Master instance by its name and removes it from the active list.
        /// </summary>
        /// <param name="masterName">The name of the Master to stop</param>
        public static bool StopMaster(string masterName)
        {
            if (string.IsNullOrEmpty(masterName))
            {
                logger.Error("Master name is null or empty.");
                return false;
            }

            var master = masters.FirstOrDefault(m => m.Name == masterName);
            if (master == null)
            {
                logger.Warning($"No Master found with the name: {masterName}");
                return false;
            }

            try
            {
                // Remove the master from the list
                master.Stop();
                masters.Remove(master);

                // Remove the task associated with the master
                var masterTask = masterTasks.FirstOrDefault(t => t.IsCompleted || t.IsFaulted || t.IsCanceled);
                if (masterTask != null)
                {
                    masterTasks.Remove(masterTask);
                }

                logger.Information($"Master {masterName} has been stopped.");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("Error stopping Master: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Starts a Master instance by its name and adds it to the active list.
        /// </summary>
        /// <param name="masterName">The name of the Master to start</param>
        public static bool StartMaster(string masterName)
        {
            if (string.IsNullOrEmpty(masterName))
            {
                logger.Error("Master name is null or empty.");
                return false;
            }

            // Check if the master is already running
            IMasterService master = masters.FirstOrDefault(m => m.Name == masterName);
            if (master != null)
            {
                logger.Warning($"Master with the name {masterName} is already running.");
                return false;
            }

            // Search for the master configuration in the config file
            var masterConfig = config.Masters.FirstOrDefault(m => m.Name == masterName);
            if (masterConfig == null)
            {
                logger.Warning($"No Master configuration found with the name: {masterName}");
                return false;
            }

            try
            {
                // Create and start the master instance
                master = new Master(masterConfig);
                masters.Add(master);
                masterTasks.Add(Task.Run(() => master.Start(cancellationTokenSource.Token)));

                logger.Information($"Master {masterName} has been started.");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("Error starting Master: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Removes a Master instance by its name and removes it from the active list, and config.
        /// </summary>
        /// <param name="masterName">The name of the Master to stop</param>
        public static bool RemoveMaster(string masterName)
        {
            if (string.IsNullOrEmpty(masterName))
            {
                logger.Error("Master name is null or empty.");
                return false;
            }

            var master = masters.FirstOrDefault(m => m.Name == masterName);
            if (master == null)
            {
                logger.Warning($"No Master found with the name: {masterName}");
                return false;
            }

            try
            {
                // Remove the master from the list
                master.Stop();
                masters.Remove(master);
                restServer?.RemoveMaster(masterName);

                // Remove the task associated with the master
                var masterTask = masterTasks.FirstOrDefault(t => t.IsCompleted || t.IsFaulted || t.IsCanceled);
                if (masterTask != null)
                {
                    masterTasks.Remove(masterTask);
                }

                // Update the config and save it
                var masterConfig = config.Masters.FirstOrDefault(m => m.Name == masterName);
                if (masterConfig != null)
                {
                    config.Masters.Remove(masterConfig);
                    SaveConfig(configPath, config);
                }

                logger.Information($"Master {masterName} has been stopped and removed.");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("Error stopping Master: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Gracefully kill all masters then die
        /// </summary>
        private static void Shutdown()
        {
            cancellationTokenSource.Cancel();

            try
            {
                Task.WhenAll(masterTasks).Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var innerException in ex.InnerExceptions)
                {
                    logger.Error(innerException, "Error during shutdown");
                }
            }
        }
    }
}

