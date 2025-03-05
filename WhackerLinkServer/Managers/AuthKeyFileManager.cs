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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using Serilog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using WhackerLinkLib.Managers;
using WhackerLinkLib.Models;
using Timer = System.Timers.Timer;

namespace WhackerLinkServer.Managers
{
    /// <summary>
    /// Manages authentication keys from a YAML file for a single master instance.
    /// </summary>
    public class AuthKeyFileManager
    {
        private HashSet<string> authKeys = new();
        private readonly Timer reloadTimer;
        private readonly string authFilePath;
        private readonly string masterName;
        private readonly bool isAclEnabled;
        private readonly ILogger logger;

        /// <summary>
        /// Creates an instance of <see cref="AuthKeyFileManager"/> to manage authentication keys for a specific master.
        /// </summary>
        /// <param name="filePath">Path to the authentication key YAML file</param>
        /// <param name="masterName">The name of the master instance</param>
        /// <param name="enabled">Whether authentication is enabled</param>
        /// <param name="reloadIntervalMs">Interval for reloading the auth file (0 to disable auto-reload)</param>
        public AuthKeyFileManager(string filePath, string masterName, bool enabled, ILogger logger, int reloadIntervalMs = 60000)
        {
            this.masterName = masterName;
            isAclEnabled = enabled;
            authFilePath = filePath;
            this.logger = logger;

            LoadAuthKeys();

            if (reloadIntervalMs > 0)
            {
                reloadTimer = new Timer(reloadIntervalMs);
                reloadTimer.Elapsed += (sender, args) => ReloadAuthFile();
                reloadTimer.AutoReset = true;
                reloadTimer.Start();
            }
        }

        /// <summary>
        /// Loads authentication keys from a YAML file and hashes them.
        /// </summary>
        private void LoadAuthKeys()
        {
            if (!isAclEnabled)
                return;

            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                var yaml = File.ReadAllText(authFilePath);
                var authData = deserializer.Deserialize<AuthKeyFile>(yaml);

                if (authData?.Entries == null)
                {
                    logger.Warning("Auth key file is empty or invalid.");
                    return;
                }

                // Filter only keys for this specific master
                authKeys = authData.Entries
                    .Where(entry => entry.Enabled && !string.IsNullOrWhiteSpace(entry.AuthKey))
                    .Select(entry => AuthKeyManager.HashKey(entry.AuthKey))
                    .ToHashSet();

                logger.Information("Loaded {Count} authentication entries for master {Master}.", authKeys.Count, masterName);
            }
            catch (Exception ex)
            {
                logger.Error("Error loading authentication keys: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Checks if authentication is enabled.
        /// </summary>
        public bool IsAuthEnabled() => isAclEnabled;

        /// <summary>
        /// Validates if the provided hashed key is allowed for this master.
        /// </summary>
        /// <param name="hashedKey">The provided hashed key</param>
        /// <returns>True if the key is valid, otherwise false</returns>
        public bool IsValidAuthKey(string hashedKey)
        {
            if (!isAclEnabled)
                return true;

            return authKeys.Contains(hashedKey);
        }

        /// <summary>
        /// Reloads authentication keys from the YAML file.
        /// </summary>
        private void ReloadAuthFile()
        {
            if (!isAclEnabled)
                return;

            try
            {
                LoadAuthKeys();
                logger.Information("Reloaded authentication keys from {Path} for master {Master}", authFilePath, masterName);
            }
            catch (Exception ex)
            {
                logger.Error("Error reloading authentication keys: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Stops the reload timer.
        /// </summary>
        public void StopReloading()
        {
            reloadTimer?.Stop();
            reloadTimer?.Dispose();
        }
    }

    /// <summary>
    /// Represents the YAML authentication key file structure.
    /// </summary>
    public class AuthKeyFile
    {
        public List<AuthKeyFileEntry> Entries { get; set; } = new();
    }
}
