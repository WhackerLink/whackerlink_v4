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
using WhackerLinkCommonLib.Interfaces;

namespace WhackerLinkServer.RestApi.Modules
{
    /// <summary>
    /// 
    /// </summary>
    public class AffiliationsModule : NancyModule
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="masterService"></param>
        public AffiliationsModule(IMasterService masterService)
        {
            Get("/api/affiliations", _ =>
            {
                var affiliations = masterService.GetAffiliations();
                return Response.AsJson(affiliations);
            });
        }
    }
}