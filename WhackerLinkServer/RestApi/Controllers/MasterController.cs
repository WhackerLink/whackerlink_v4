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

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using WhackerLinkLib.Models;
using WhackerLinkServer.Models;

namespace WhackerLinkServer.RestApi.Controllers
{
    [ApiController]
    [Route("api/masters")]
    public class MasterController : ControllerBase
    {
        [HttpPost("add")]
        public IActionResult AddMaster([FromBody] JsonElement masterConfigJson)
        {
            try
            {
                // Validate required fields
                if (!masterConfigJson.TryGetProperty("Name", out var nameProp) || string.IsNullOrWhiteSpace(nameProp.GetString()))
                {
                    return BadRequest(new { error = "The 'Name' field is required and cannot be empty." });
                }

                if (!masterConfigJson.TryGetProperty("Address", out var addressProp) || string.IsNullOrWhiteSpace(addressProp.GetString()))
                {
                    return BadRequest(new { error = "The 'Address' field is required and cannot be empty." });
                }

                if (!masterConfigJson.TryGetProperty("Port", out var portProp) || !portProp.TryGetInt32(out _))
                {
                    return BadRequest(new { error = "The 'Port' field is required and must be a valid integer." });
                }

                // Check for existing masters with the same Name or Port
                //var existingMasters = Program.GetRunningMasters(); // Assuming this method exists and returns a list of current masters
                //if (existingMasters.Any(m => m.Name == nameProp.GetString()))
                //{
                //    return Conflict(new { error = "A master with the same 'Name' already exists." });
                //}

                //if (existingMasters.Any(m => m.Port == portProp))
                //{
                //    return Conflict(new { error = "A master with the same 'Port' already exists." });
                //}

                if (!masterConfigJson.TryGetProperty("Auth", out var authProp) ||
                    !authProp.TryGetProperty("Enabled", out var authEnabledProp) ||
                    authEnabledProp.ValueKind != JsonValueKind.True && authEnabledProp.ValueKind != JsonValueKind.False)
                {
                    return BadRequest(new { error = "The 'Auth.Enabled' field is required and must be a boolean." });
                }

                if (!authProp.TryGetProperty("Path", out var authPathProp) || string.IsNullOrWhiteSpace(authPathProp.GetString()))
                {
                    return BadRequest(new { error = "The 'Auth.Path' field is required and cannot be empty." });
                }

                if (!authProp.TryGetProperty("ReloadInterval", out var authReloadIntervalProp) || !authReloadIntervalProp.TryGetInt32(out _))
                {
                    return BadRequest(new { error = "The 'Auth.ReloadInterval' field is required and must be a valid integer." });
                }

                if (!masterConfigJson.TryGetProperty("Ssl", out var sslProp) || !sslProp.TryGetProperty("Enabled", out var sslEnabledProp) || sslEnabledProp.ValueKind != JsonValueKind.True && sslEnabledProp.ValueKind != JsonValueKind.False)
                {
                    return BadRequest(new { error = "The 'Ssl.Enabled' field is required and must be a boolean." });
                }

                if (!sslProp.TryGetProperty("CertificatePath", out var certPathProp) || string.IsNullOrWhiteSpace(certPathProp.GetString()))
                {
                    return BadRequest(new { error = "The 'Ssl.CertificatePath' field is required and cannot be empty." });
                }

                if (!sslProp.TryGetProperty("CertificatePassword", out var certPasswordProp))
                {
                    return BadRequest(new { error = "The 'Ssl.CertificatePassword' field is required." });
                }

                if (!masterConfigJson.TryGetProperty("AffilationRestricted", out var affilationRestrictedProp) || affilationRestrictedProp.ValueKind != JsonValueKind.True && affilationRestrictedProp.ValueKind != JsonValueKind.False)
                {
                    return BadRequest(new { error = "The 'AffilationRestricted' field is required and must be a boolean." });
                }

                if (!masterConfigJson.TryGetProperty("NoSelfRepeat", out var noSelfRepeatProp) || noSelfRepeatProp.ValueKind != JsonValueKind.True && noSelfRepeatProp.ValueKind != JsonValueKind.False)
                {
                    return BadRequest(new { error = "The 'NoSelfRepeat' field is required and must be a boolean." });
                }

                if (!masterConfigJson.TryGetProperty("DisableSiteBcast", out var disableSiteBcastProp) || disableSiteBcastProp.ValueKind != JsonValueKind.True && disableSiteBcastProp.ValueKind != JsonValueKind.False)
                {
                    return BadRequest(new { error = "The 'DisableSiteBcast' field is required and must be a boolean." });
                }

                if (!masterConfigJson.TryGetProperty("DisableVchUpdates", out var disableVchUpdatesProp) || disableVchUpdatesProp.ValueKind != JsonValueKind.True && disableVchUpdatesProp.ValueKind != JsonValueKind.False)
                {
                    return BadRequest(new { error = "The 'DisableVchUpdates' field is required and must be a boolean." });
                }

                if (!masterConfigJson.TryGetProperty("DisableLocationBroadcasts", out var disableLocationBroadcastsProp) || disableLocationBroadcastsProp.ValueKind != JsonValueKind.True && disableLocationBroadcastsProp.ValueKind != JsonValueKind.False)
                {
                    return BadRequest(new { error = "The 'DisableLocationBroadcasts' field is required and must be a boolean." });
                }

                if (!masterConfigJson.TryGetProperty("DisableLocBcastLogs", out var disableLocBcastLogsProp) || disableLocBcastLogsProp.ValueKind != JsonValueKind.True && disableLocBcastLogsProp.ValueKind != JsonValueKind.False)
                {
                    return BadRequest(new { error = "The 'DisableLocBcastLogs' field is required and must be a boolean." });
                }

                if (!masterConfigJson.TryGetProperty("VocoderMode", out var vocoderModeProp) || string.IsNullOrWhiteSpace(vocoderModeProp.GetString()))
                {
                    return BadRequest(new { error = "The 'VocoderMode' field is required and cannot be empty." });
                }

                if (!masterConfigJson.TryGetProperty("PreEncodeGain", out var preEncodeGainProp) || !preEncodeGainProp.TryGetSingle(out _))
                {
                    return BadRequest(new { error = "The 'PreEncodeGain' field is required and must be a valid float." });
                }

                if (!masterConfigJson.TryGetProperty("Reporter", out var reporterProp) || !reporterProp.TryGetProperty("Enabled", out var reporterEnabledProp) || reporterEnabledProp.ValueKind != JsonValueKind.True && reporterEnabledProp.ValueKind != JsonValueKind.False)
                {
                    return BadRequest(new { error = "The 'Reporter.Enabled' field is required and must be a boolean." });
                }

                if (!reporterProp.TryGetProperty("Address", out var reporterAddressProp) || string.IsNullOrWhiteSpace(reporterAddressProp.GetString()))
                {
                    return BadRequest(new { error = "The 'Reporter.Address' field is required and cannot be empty." });
                }

                if (!reporterProp.TryGetProperty("Port", out var reporterPortProp) || !reporterPortProp.TryGetInt32(out _))
                {
                    return BadRequest(new { error = "The 'Reporter.Port' field is required and must be a valid integer." });
                }

                if (masterConfigJson.TryGetProperty("Sites", out var sitesProp) && sitesProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var site in sitesProp.EnumerateArray())
                    {
                        if (!site.TryGetProperty("Name", out var siteNameProp) || string.IsNullOrWhiteSpace(siteNameProp.GetString()))
                        {
                            return BadRequest(new { error = "Each site must have a 'Name' field that is not empty." });
                        }

                        if (!site.TryGetProperty("ControlChannel", out var controlChannelProp) || string.IsNullOrWhiteSpace(controlChannelProp.GetString()))
                        {
                            return BadRequest(new { error = "Each site must have a 'ControlChannel' field that is not empty." });
                        }

                        if (!site.TryGetProperty("VoiceChannels", out var voiceChannelsProp) || voiceChannelsProp.ValueKind != JsonValueKind.Array || !voiceChannelsProp.EnumerateArray().Any())
                        {
                            return BadRequest(new { error = "Each site must have a 'VoiceChannels' field that is a non-empty array." });
                        }

                        if (!site.TryGetProperty("Location", out var locationProp) || !locationProp.TryGetProperty("X", out var xProp) || string.IsNullOrWhiteSpace(xProp.GetString()) ||
                            !locationProp.TryGetProperty("Y", out var yProp) || string.IsNullOrWhiteSpace(yProp.GetString()) ||
                            !locationProp.TryGetProperty("Z", out var zProp) || string.IsNullOrWhiteSpace(zProp.GetString()))
                        {
                            return BadRequest(new { error = "Each site must have a 'Location' field with non-empty 'X', 'Y', and 'Z' properties." });
                        }

                        if (!site.TryGetProperty("SiteID", out var siteIdProp) || string.IsNullOrWhiteSpace(siteIdProp.GetString()))
                        {
                            return BadRequest(new { error = "Each site must have a 'SiteID' field that is not empty." });
                        }

                        if (!site.TryGetProperty("SystemID", out var systemIdProp) || string.IsNullOrWhiteSpace(systemIdProp.GetString()))
                        {
                            return BadRequest(new { error = "Each site must have a 'SystemID' field that is not empty." });
                        }

                        if (!site.TryGetProperty("Range", out var rangeProp) || !rangeProp.TryGetDouble(out _))
                        {
                            return BadRequest(new { error = "Each site must have a 'Range' field that is a valid double." });
                        }
                    }
                }
                else
                {
                    return BadRequest(new { error = "The 'Sites' field must be a valid array." });
                }

                if (!masterConfigJson.TryGetProperty("RidAcl", out var ridAclProp) || !ridAclProp.TryGetProperty("Enabled", out var ridAclEnabledProp) || ridAclEnabledProp.ValueKind != JsonValueKind.True && ridAclEnabledProp.ValueKind != JsonValueKind.False)
                {
                    return BadRequest(new { error = "The 'RidAcl.Enabled' field is required and must be a boolean." });
                }

                if (!ridAclProp.TryGetProperty("Path", out var ridAclPathProp) || string.IsNullOrWhiteSpace(ridAclPathProp.GetString()))
                {
                    return BadRequest(new { error = "The 'RidAcl.Path' field is required and cannot be empty." });
                }

                if (!ridAclProp.TryGetProperty("ReloadInterval", out var ridAclReloadIntervalProp) || !ridAclReloadIntervalProp.TryGetInt32(out _))
                {
                    return BadRequest(new { error = "The 'RidAcl.ReloadInterval' field is required and must be a valid integer." });
                }

                // Proceed with creating the masterConfig object
                var masterConfig = new Config.MasterConfig
                {
                    Name = masterConfigJson.GetProperty("Name").GetString(),
                    Address = masterConfigJson.GetProperty("Address").GetString(),
                    Port = masterConfigJson.GetProperty("Port").GetInt32(),
                    Auth = new Config.AuthConfig
                    {
                        Enabled = masterConfigJson.GetProperty("Auth").GetProperty("Enabled").GetBoolean(),
                        Path = masterConfigJson.GetProperty("Auth").GetProperty("Path").GetString(),
                        ReloadInterval = masterConfigJson.GetProperty("Auth").GetProperty("ReloadInterval").GetInt32()
                    },
                    Ssl = new Config.SslConfig
                    {
                        Enabled = masterConfigJson.GetProperty("Ssl").GetProperty("Enabled").GetBoolean(),
                        CertificatePath = masterConfigJson.GetProperty("Ssl").GetProperty("CertificatePath").GetString(),
                        CertificatePassword = masterConfigJson.GetProperty("Ssl").GetProperty("CertificatePassword").GetString()
                    },
                    AffilationRestricted = masterConfigJson.GetProperty("AffilationRestricted").GetBoolean(),
                    NoSelfRepeat = masterConfigJson.GetProperty("NoSelfRepeat").GetBoolean(),
                    DisableSiteBcast = masterConfigJson.GetProperty("DisableSiteBcast").GetBoolean(),
                    DisableVchUpdates = masterConfigJson.GetProperty("DisableVchUpdates").GetBoolean(),
                    DisableLocationBroadcasts = masterConfigJson.GetProperty("DisableLocationBroadcasts").GetBoolean(),
                    DisableLocBcastLogs = masterConfigJson.GetProperty("DisableLocBcastLogs").GetBoolean(),
                    VocoderMode = Enum.Parse<VocoderModes>(masterConfigJson.GetProperty("VocoderMode").GetString()),
                    PreEncodeGain = masterConfigJson.GetProperty("PreEncodeGain").GetSingle(),
                    Reporter = new Config.ReporterConfiguration
                    {
                        Enabled = masterConfigJson.GetProperty("Reporter").GetProperty("Enabled").GetBoolean(),
                        Address = masterConfigJson.GetProperty("Reporter").GetProperty("Address").GetString(),
                        Port = masterConfigJson.GetProperty("Reporter").GetProperty("Port").GetInt32()
                    },
                    Sites = masterConfigJson.GetProperty("Sites").EnumerateArray().Select(site => new Site
                    {
                        Name = site.GetProperty("Name").GetString(),
                        ControlChannel = site.GetProperty("ControlChannel").GetString(),
                        VoiceChannels = site.GetProperty("VoiceChannels").EnumerateArray().Select(vc => vc.GetString()).ToList(),
                        Location = new Location
                        {
                            X = site.GetProperty("Location").GetProperty("X").GetString(),
                            Y = site.GetProperty("Location").GetProperty("Y").GetString(),
                            Z = site.GetProperty("Location").GetProperty("Z").GetString()
                        },
                        SiteID = site.GetProperty("SiteID").GetString(),
                        SystemID = site.GetProperty("SystemID").GetString(),
                        Range = (float)site.GetProperty("Range").GetDouble()
                    }).ToList(),
                    RidAcl = new Config.RidAclConfiguration
                    {
                        Enabled = masterConfigJson.GetProperty("RidAcl").GetProperty("Enabled").GetBoolean(),
                        Path = masterConfigJson.GetProperty("RidAcl").GetProperty("Path").GetString(),
                        ReloadInterval = masterConfigJson.GetProperty("RidAcl").GetProperty("ReloadInterval").GetInt32()
                    }
                };

                // Reflect success or failure in the status code
                bool success = Program.AddNewMaster(masterConfig);
                if (!success)
                {
                    return StatusCode(500, new { error = "Failed to add the master configuration." });
                }

                return Ok(new { message = "Master added successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred: {ex.Message}" });
            }
        }
    }
}
