using Microsoft.AspNetCore.Cors;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Timers;
using WhackerLinkServer.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

#nullable disable

namespace WhackerLinkServer.Managers
{
    public class RidAclManager
    {
        public List<RidAclEntry> ridAclEntries = new List<RidAclEntry>();
        private System.Timers.Timer reloadTimer;
        private string aclFilePath;
        private bool aclEnabled = true;

        public RidAclManager(bool enabled)
        {
            aclEnabled = enabled;
        }

        public bool GetAclEnabled()
        {
            return aclEnabled;
        }

        public void AddRidEntry(RidAclEntry entry)
        {
            if (!aclEnabled) return;

            ridAclEntries.Add(entry);
        }

        public void RemoveRidEntry(RidAclEntry entry)
        {
            if (!aclEnabled) return;

            ridAclEntries.Remove(entry);
        }

        public bool IsRidAllowed(RidAclEntry entry)
        {
            if (!aclEnabled) return true;

            return ridAclEntries.Any(a => a.Rid == entry.Rid && a.Allowed);
        }

        public bool IsRidAllowed(string rid)
        {
            if (!aclEnabled) return true;

            return ridAclEntries.Any(a => a.Rid == rid && a.Allowed);
        }

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

        public void StartReloadTimer(int intervalInMilliseconds)
        {
            reloadTimer = new System.Timers.Timer(intervalInMilliseconds);
            reloadTimer.Elapsed += (sender, args) => ReloadAclFile();
            reloadTimer.AutoReset = true;
            reloadTimer.Start();
        }

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

        public void StopReloadTimer()
        {
            reloadTimer?.Stop();
            reloadTimer?.Dispose();
        }
    }
}