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

using System.Text;
using WhackerLinkCommonLib.Models;

#nullable disable

namespace WhackerLinkServer.Managers
{
    public class SiteManager
    {
        public List<Site> Sites { get; set; }

        /// <summary>
        /// Creates an instance of channel grant manager
        /// </summary>
        public SiteManager()
        {
            Sites = new List<Site>();
        }

        /// <summary>
        /// Gets the list of active voice channels
        /// </summary>
        /// <returns></returns>
        public List<Site> GetSites()
        {
            return Sites;
        }

        /// <summary>
        /// Helper to add a sitel to the list
        /// </summary>
        /// <param name="site"></param>
        public void AddSite(Site site)
        {
            Sites.Add(site);
        }

        /// <summary>
        /// Helper to remove the site from the list
        /// </summary>
        /// <param name="site"></param>
        public void RemoveVoiceChannel(Site site)
        {
            Sites.Remove(site);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="siteId"></param>
        /// <returns></returns>
        public Site GetSiteById(string siteId)
        {
            return Sites.FirstOrDefault(site => site.SiteID == siteId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Sites:");

            foreach (Site site in Sites)
            {
                sb.AppendLine($"CC: {site.ControlChannel}, VC Count: {site.VoiceChannels.Count()}, SiteID: {site.SiteID}, SystemID: {site.SystemID}");
            }

            return sb.ToString();
        }
    }
}