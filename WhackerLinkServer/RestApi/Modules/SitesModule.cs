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

using Nancy;
using WhackerLinkLib.Interfaces;
using WhackerLinkLib.Models;

namespace WhackerLinkServer.RestApi.Modules
{
    /// <summary>
    /// 
    /// </summary>
    public class SitesModule : NancyModule
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="masterService"></param>
        public SitesModule(IMasterService masterService) : base("/api/sites")
        {
            Get("/query", _ =>
            {
                List<Site> sites = masterService.GetSites();

                var response = new
                {
                    sites
                };

                return Response.AsJson(response);
            });
        }
    }
}