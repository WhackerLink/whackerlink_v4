using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;

public static class LoggerSetup
{
    public static ILogger CreateLogger(string masterName)
    {
        return new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u1}] [{MasterName}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("logs/whackerlink-.log", rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u1}] [{MasterName}] {Message:lj}{NewLine}{Exception}")
            .Enrich.With(new MasterNameEnricher(masterName))
            .CreateLogger();
    }
}

public class MasterNameEnricher : ILogEventEnricher
{
    private readonly string _masterName;

    public MasterNameEnricher(string masterName)
    {
        _masterName = masterName;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (!string.IsNullOrEmpty(_masterName))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("MasterName", _masterName));
        }
    }
}