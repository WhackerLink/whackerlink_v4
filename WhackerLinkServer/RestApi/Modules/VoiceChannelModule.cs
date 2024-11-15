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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhackerLinkLib.Interfaces;

namespace WhackerLinkServer.RestApi.Modules
{
    /// <summary>
    /// 
    /// </summary>
    public class VoiceChannelModule : NancyModule
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="masterService"></param>
        public VoiceChannelModule(IMasterService masterService)
        {
            Get("/api/voiceChannel/query", _ =>
            {

                var response = new
                {
                    Sites = masterService.GetSites(),
                    ActiveVoiceChannels = masterService.GetVoiceChannels()
                };

                return Response.AsJson(response);
            });
        }
    }
}