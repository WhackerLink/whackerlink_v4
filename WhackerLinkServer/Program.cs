using Serilog;
using WhackerLinkCommonLib.Interfaces;
using WhackerLinkServer.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

#nullable disable

namespace WhackerLinkServer
{
    class Program
    {
        internal static Config config;
        public static ILogger logger;
        private static List<Task> masterTasks = new List<Task>();
        private static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        static async Task Main(string[] args)
        {
            logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/whackerlink-.log", rollingInterval: RollingInterval.Day, outputTemplate: "{Level:u1}: {Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Message}{NewLine}{Exception}")
                .CreateLogger();

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