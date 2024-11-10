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
using Serilog.Core;
using Serilog.Events;
using System;

/// <summary>
/// Class to create logger
/// </summary>
public static class LoggerSetup
{
    /// <summary>
    /// Creates logger config
    /// </summary>
    /// <param name="masterName"></param>
    /// <returns></returns>
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

/// <summary>
/// Class to enrich logging
/// </summary>
public class MasterNameEnricher : ILogEventEnricher
{
    private string _masterName;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="masterName"></param>
    public MasterNameEnricher(string masterName)
    {
        _masterName = masterName;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="logEvent"></param>
    /// <param name="propertyFactory"></param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (string.IsNullOrEmpty(_masterName))
            _masterName = "SYSTEM";

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("MasterName", _masterName));
    }
}