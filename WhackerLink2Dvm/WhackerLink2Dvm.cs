using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Extensions.Logging;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using WhackerLink2Dvm.Models;

using WhackerLinkLib.Interfaces;
using WhackerLinkLib.Network;
using Serilog.Events;

#nullable disable

namespace WhackerLink2Dvm
{
    public class WhackerLink2Dvm
    {
        public static Config config;
        public static Serilog.ILogger logger;

        private string configPath;

        public WhackerLink2Dvm(string configPath) 
        {
            this.configPath = configPath;

            Init();
        }

        public void Init()
        {
            try
            {

                using (FileStream stream = new FileStream(configPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (TextReader reader = new StreamReader(stream))
                    {
                        string yml = reader.ReadToEnd();

                        // setup the YAML deseralizer for the configuration
                        IDeserializer ymlDeserializer = new DeserializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .Build();

                        config = ymlDeserializer.Deserialize<Config>(yml);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"error: cannot read the configuration file, {this.configPath}\n{e.Message}");
                Environment.Exit(0);
            }

            var logConfig = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u1}] [{MasterName}] {Message:lj}{NewLine}{Exception}")
                        .CreateLogger();


            Log.Logger = logConfig;
            logger = logConfig;

            try
            {
                CreateHostBuilder().Build().Run();
            }
            catch (Exception e)
            {
                WhackerLink2Dvm.logger.Error(e, "Problem starting service");
            }
        }

        public static IHostBuilder CreateHostBuilder() =>
        Host.CreateDefaultBuilder()
        .ConfigureServices((hostContext, services) =>
        {
            services.AddLogging(config =>
            {
                config.ClearProviders();
                config.AddProvider(new SerilogLoggerProvider(Log.Logger));
            });
            services.AddHostedService<Service>();
        });
    }
}