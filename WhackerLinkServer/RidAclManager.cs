using Serilog;
using WhackerLinkServer.Models;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

#nullable disable

namespace WhackerLinkServer
{
    public class RidAclManager
    {
        public List<RidAclEntry> ridAclEntries = new List<RidAclEntry>();
        
        public RidAclManager() { /* stub */ }


        public void AddRidEntry(RidAclEntry entry)
        {
            ridAclEntries.Add(entry);
        }

        public void RemoveRidEntry(RidAclEntry entry)
        {
            ridAclEntries.Remove(entry);
        }

        public bool IsRidAllowed(RidAclEntry entry)
        {
            return ridAclEntries.Any(a => a.Rid == entry.Rid && a.Allowed);
        }

        public bool IsRidAllowed(string rid)
        {
            return  ridAclEntries.Any(a => a.Rid == rid && a.Allowed);
        }

        public void LogEntries()
        {
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
            try
            {
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
    }
}
