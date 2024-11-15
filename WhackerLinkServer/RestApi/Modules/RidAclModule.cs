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

namespace WhackerLinkServer.RestApi.Modules
{
    /// <summary>
    /// 
    /// </summary>
    public class RidAclModule : NancyModule
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="masterService"></param>
        public RidAclModule(IMasterService masterService) : base("/api/rid")
        {
            Get("/query", _ =>
            {
                var rids = masterService.GetRidAcl();
                bool aclEnabled = masterService.GetRidAclEnabled();

                var response = new
                {
                    Enabled = aclEnabled,
                    RidAcl = rids
                };

                return Response.AsJson(response);
            });
        }
    }
}