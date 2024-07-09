using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;
using WhackerLinkCommonLib.Models;

#nullable disable

namespace WhackerLinkServer
{
    public class Reporter
    {
        private string address;
        private int port;
        private ILogger logger;
        private readonly HttpClient _httpClient;

        public Reporter(string address, int port, ILogger logger)
        {
            this.address = address;
            this.port = port;

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri($"http://{address}:{port}")
            };

            logger.Information("Started Reporter at http://{Address}:{Port}", address, port);
        }

        /// <summary>
        /// Create and send a report async
        /// </summary>
        /// <param name="reportData"></param>
        /// <returns></returns>
        public async Task SendReportAsync(object reportData)
        {
                var json = JsonConvert.SerializeObject(reportData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                try
                {
                    var response = await _httpClient.PostAsync("/", content);
                    if (response.IsSuccessStatusCode)
                    {
#if DEBUG
                        logger.Debug("[REPORTER] Report sent");
#endif
                    }
                    else
                    {
                        logger.Error($"[REPORTER] Failed to send: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    logger.Error($"[REPORTER] Error sending report: {ex.Message}");
                }
        }

        /// <summary>
        /// Helper function to send a report
        /// </summary>
        /// <param name="type"></param>
        /// <param name="srcId"></param>
        /// <param name="dstId"></param>
        /// <param name="extra"></param>
        public void Send(PacketType type, string srcId, string dstId, string extra, ResponseType responseType = ResponseType.UNKOWN)
        {
            var utcNow = DateTime.UtcNow;

            TimeZoneInfo cdtZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            DateTime cdtTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, cdtZone);

            string timestamp = cdtTime.ToString("yyyy-MM-ddTHH:mm:ss.fffK", CultureInfo.InvariantCulture);

            var reportData = new
            {
                Type = type,
                SrcId = srcId,
                DstId = dstId,
                ResponseType = responseType,
                Extra = extra,
                Timestamp = timestamp
            };

            Task.Run(() => SendReportAsync(reportData));
        }
    }
}