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

using Microsoft.AspNetCore.Cors;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Timers;
using WhackerLinkLib.Models;
using WhackerLinkLib.Utils;
using WhackerLinkServer.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

#nullable disable

namespace WhackerLinkServer.Managers
{
    /// <summary>
    /// Class to manage radio id's
    /// </summary>
    public class RidAclManager
    {
        public List<RidAclEntry> ridAclEntries = new List<RidAclEntry>();
        private System.Timers.Timer reloadTimer;
        private string aclFilePath;
        private bool aclEnabled = true;

        /// <summary>
        /// Creates an instance of ridaclmanager
        /// </summary>
        /// <param name="enabled"></param>
        public RidAclManager(bool enabled)
        {
            aclEnabled = enabled;
        }

        /// <summary>
        /// Get if the acl is enabled
        /// </summary>
        /// <returns></returns>
        public bool GetAclEnabled()
        {
            return aclEnabled;
        }

        /// <summary>
        /// Helper to add an acl entry
        /// </summary>
        /// <param name="entry"></param>
        public void AddRidEntry(RidAclEntry entry)
        {
            if (!aclEnabled) return;

            ridAclEntries.Add(entry);
        }

        /// <summary>
        /// Helper to remove rid acl entry
        /// </summary>
        /// <param name="entry"></param>
        public void RemoveRidEntry(RidAclEntry entry)
        {
            if (!aclEnabled) return;

            ridAclEntries.Remove(entry);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public bool IsAuthEnabled(string rid)
        {
            var entry = ridAclEntries.FirstOrDefault(a => a.Rid == rid);

            return entry != null && entry.AuthKey != null;
        }

        /// <summary>
        /// Checks if a rid is allowed using an acl entry
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public bool IsRidAllowed(RidAclEntry entry)
        {
            if (!aclEnabled) return true;

            return ridAclEntries.Any(a => a.Rid == entry.Rid && a.Allowed);
        }

        /// <summary>
        /// Checks if an rid is allowed using a rid
        /// </summary>
        /// <param name="rid"></param>
        /// <returns></returns>
        public bool IsRidAllowed(string rid)
        {
            if (!aclEnabled) return true;

            return ridAclEntries.Any(a => a.Rid == rid && a.Allowed);
        }

        /// <summary>
        /// Checks if an rid is allowed using a rid and auth key
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="hashedKey"></param>
        /// <returns></returns>
        public ResponseType IsRidAllowed(string rid, string hashedKey)
        {
            if (!aclEnabled) return ResponseType.GRANT;

            var entry = ridAclEntries.FirstOrDefault(a => a.Rid == rid);

            if (entry == null || !entry.Allowed)
            {
                return ResponseType.REFUSE;
            }

            string hashedInputKey = AuthUtil.HashPassword(hashedKey);

            if (entry.AuthKey != hashedInputKey)
            {
                return ResponseType.FAIL;
            }

            return ResponseType.GRANT;
        }


        /// <summary>
        /// Helper to log acl entries to console
        /// </summary>
        public void LogEntries()
        {
            if (!aclEnabled) return;

            Console.WriteLine("RID ACL ENTRIES:");
            if (ridAclEntries != null)
            {
                foreach (var entry in ridAclEntries)
                {
                    Console.WriteLine($"RID: {entry.Rid}, Allowed: {entry.Allowed}, Alias: {entry.Alias}");
                }
            }
            else
            {
                Console.WriteLine("No entries found or failed to load entries.");
            }
        }

        /// <summary>
        /// Helper to load RID ACL from file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public RidAcl Load(string path)
        {
            if (!aclEnabled) return null;

            try
            {
                aclFilePath = path;

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                var yaml = File.ReadAllText(path);
                RidAcl ridAcl = deserializer.Deserialize<RidAcl>(yaml);

                if (ridAcl != null && ridAcl.entries != null)
                {
                    ridAclEntries = ridAcl.entries;
                    Log.Information("Loaded {Count} RID ACL entries.", ridAclEntries.Count);
                }

                return ridAcl;
            }
            catch (Exception ex)
            {
                Log.Error("Error loading RID ACL: {Message}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Start ACL reload
        /// </summary>
        /// <param name="intervalInMilliseconds"></param>
        public void StartReloadTimer(int intervalInMilliseconds)
        {
            reloadTimer = new System.Timers.Timer(intervalInMilliseconds);
            reloadTimer.Elapsed += (sender, args) => ReloadAclFile();
            reloadTimer.AutoReset = true;
            reloadTimer.Start();
        }

        /// <summary>
        /// Reload ACL file
        /// </summary>
        internal void ReloadAclFile()
        {
            if (!aclEnabled) return;

            try
            {
                Load(aclFilePath);
                Log.Information("Reloaded RID ACL entries from {Path}", aclFilePath);
            }
            catch (Exception ex)
            {
                Log.Error("Error reloading RID ACL: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Stop ACL reload
        /// </summary>
        public void StopReloadTimer()
        {
            reloadTimer?.Stop();
            reloadTimer?.Dispose();
        }
    }
}