/*
 * Copyright (C) 2024-2026 Caleb H. (K4PHP) caleb.k4php@gmail.com
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

using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using Serilog;
using WhackerLinkLib.Models;
using WhackerLinkLib.Models.IOSP;

namespace WhackerLinkServer
{
    /// <summary>
    /// Class to send HTTP POST reports and optional Discord webhook notifications
    /// </summary>
    public class Reporter
    {
        private readonly string address;
        private readonly int port;
        private readonly ILogger logger;
        private readonly bool enabled;
        private readonly bool discordEnabled;

        private readonly HttpClient _httpClient;
        private readonly HttpClient _discordClient;
        private readonly string _discordWebhookUrl;

        /// <summary>
        /// Creates an instance of <see cref="Reporter"/>
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="logger"></param>
        /// <param name="enabled"></param>
        /// <param name="discordEnabled"></param>
        /// <param name="discordWebhookUrl"></param>
        public Reporter(
            string address,
            int port,
            ILogger logger,
            bool enabled = false,
            bool discordEnabled = false,
            string discordWebhookUrl = null)
        {
            this.address = address;
            this.port = port;
            this.enabled = enabled;
            this.discordEnabled = discordEnabled;
            this.logger = logger;
            _discordWebhookUrl = discordWebhookUrl;

            if (enabled)
            {
                _httpClient = new HttpClient
                {
                    BaseAddress = new Uri($"http://{address}:{port}")
                };

                logger.Information("Started Reporter at http://{Address}:{Port}", address, port);
            }

            if (discordEnabled && !string.IsNullOrWhiteSpace(discordWebhookUrl))
            {
                _discordClient = new HttpClient();
                logger.Information("Discord webhook reporting enabled");
            }
        }

        /// <summary>
        /// Send HTTP report
        /// </summary>
        private async Task SendHttpReportAsync(object reportData)
        {
            if (!enabled)
                return;

            var json = JsonConvert.SerializeObject(reportData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync("/", content);
                if (!response.IsSuccessStatusCode)
                {
                    logger.Error("[REPORTER] HTTP send failed: {Status}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                logger.Error("[REPORTER] HTTP error: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Send Discord webhook embed
        /// </summary>
        private async Task SendDiscordAsync(string title, string description)
        {
            if (!discordEnabled || string.IsNullOrWhiteSpace(_discordWebhookUrl))
                return;

            var payload = new
            {
                embeds = new[]
                {
                    new
                    {
                        title,
                        description,
                        color = 3447003, // blue
                        timestamp = DateTime.UtcNow.ToString("o")
                    }
                }
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _discordClient.PostAsync(_discordWebhookUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    logger.Error("[REPORTER] Discord webhook failed: {Status}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                logger.Error("[REPORTER] Discord error: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Standard packet report
        /// </summary>
        public void Send(
            PacketType type,
            string srcId,
            string dstId,
            Site site,
            string extra,
            ResponseType responseType = ResponseType.UNKOWN,
            string lat = null,
            string longi = null,
            bool discordOnly = false,
            bool noDiscord = false
            )
        {
            if (!enabled)
                return;

            var utcNow = DateTime.UtcNow;
            var cdtZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            var cdtTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, cdtZone);

            string timestamp = cdtTime.ToString("yyyy-MM-ddTHH:mm:ss.fffK", CultureInfo.InvariantCulture);

            if (!discordOnly)
            {
                var reportData = new
                {
                    Type = type,
                    SrcId = srcId,
                    DstId = dstId,
                    Site = site,
                    ResponseType = responseType,
                    Extra = extra,
                    Lat = lat,
                    Long = longi,
                    Timestamp = timestamp
                };

                Task.Run(() => SendHttpReportAsync(reportData));
            }

            if (!noDiscord && discordEnabled)
            {
                string discordText =
                    $"**Type:** {type}\n" +
                    $"**Src:** {srcId}\n" +
                    $"**Dst:** {dstId}\n" +
                    $"**Site:** {site?.Name}\n" +
                    $"**Response:** {responseType}\n" +
                    $"**Extra:** {extra}\n" +
                    $"**Time:** {timestamp}";

                Task.Run(() => SendDiscordAsync("WhackerLink Event", discordText));
            }
        }

        /// <summary>
        /// Site broadcast report
        /// </summary>
        public void Send(PacketType type, SITE_BCAST siteBcast)
        {
            if (!enabled)
                return;

            string discordText =
                $"**Type:** {type}\n" +
                $"**Sites:** {string.Join(", ", siteBcast.Sites)}";

            Task.Run(() => SendHttpReportAsync(new
            {
                Type = type,
                Sites = siteBcast.Sites,
                Timestamp = DateTime.UtcNow
            }));

            // not for now
            //Task.Run(() => SendDiscordAsync("Site Broadcast", discordText));
        }

        /// <summary>
        /// Status broadcast report
        /// </summary>
        public void Send(PacketType type, STS_BCAST stsBcast)
        {
            if (!enabled)
                return;

            string discordText =
                $"**Type:** {type}\n" +
                $"**Site:** {stsBcast.Site}\n" +
                $"**Status:** {stsBcast.Status}";

            Task.Run(() => SendHttpReportAsync(new
            {
                Type = type,
                Site = stsBcast.Site,
                Status = stsBcast.Status,
                Timestamp = DateTime.UtcNow
            }));

            // not for now
            //Task.Run(() => SendDiscordAsync("Status Broadcast", discordText));
        }
    }
}
