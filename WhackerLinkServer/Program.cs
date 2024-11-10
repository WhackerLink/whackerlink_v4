/*
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
* Copyright (C) 2024 Caleb, K4PHP
* 
*/

using Serilog;
using WhackerLinkCommonLib.Interfaces;
using WhackerLinkServer.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

#nullable disable

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
        private static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

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
                string configPath = "config.yml";
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

                logger.Information("Initializing Master instances");

                foreach (Config.MasterConfig masterConfig in config.Masters)
                {
                    Master master = new Master(masterConfig);
                    masterTasks.Add(Task.Run(() => master.Start(cancellationTokenSource.Token)));
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