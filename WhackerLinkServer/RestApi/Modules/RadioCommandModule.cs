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
* Copyright (C) 2025 Caleb, K4PHP
* 
*/

using Nancy;
using Nancy.ModelBinding;
using WhackerLinkLib.Interfaces;
using WhackerLinkLib.Models;
using WhackerLinkLib.Models.IOSP;

namespace WhackerLinkServer.RestApi.Modules
{
    /// <summary>
    /// 
    /// </summary>
    public class RadioCommandModule : NancyModule
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="masterService"></param>
        public RadioCommandModule(IMasterService masterService) : base("/api/command")
        {
            Post("/inhibit", parameters =>
            {
                try
                {
                    var requestBody = this.Bind<InhibitRequest>();

                    if (requestBody == null || string.IsNullOrWhiteSpace(requestBody.DstId))
                    {
                        return Response.AsJson(new { error = "Invalid request. 'DstId' is required." }, HttpStatusCode.BadRequest);
                    }

                    SPEC_FUNC packet = new SPEC_FUNC
                    {
                        Function = SpecFuncType.RID_INHIBIT,
                        SrcId = Defines.FNE_LID.ToString(),
                        DstId = requestBody.DstId,
                    };

                    masterService.Logger.Information(packet.ToString());

                    masterService.BroadcastPacket(packet.GetStrData());

                    return Response.AsJson(new { success = true });
                }
                catch (Exception ex)
                {
                    return Response.AsJson(new { error = ex.Message }, HttpStatusCode.InternalServerError);
                }
            });

            Post("/uninhibit", parameters =>
            {
                try
                {
                    var requestBody = this.Bind<InhibitRequest>();

                    if (requestBody == null || string.IsNullOrWhiteSpace(requestBody.DstId))
                    {
                        return Response.AsJson(new { error = "Invalid request. 'DstId' is required." }, HttpStatusCode.BadRequest);
                    }

                    SPEC_FUNC packet = new SPEC_FUNC
                    {
                        Function = SpecFuncType.RID_UNINHIBIT,
                        SrcId = Defines.FNE_LID.ToString(),
                        DstId = requestBody.DstId,
                    };

                    masterService.Logger.Information(packet.ToString());

                    masterService.BroadcastPacket(packet.GetStrData());

                    return Response.AsJson(new { success = true });
                }
                catch (Exception ex)
                {
                    return Response.AsJson(new { error = ex.Message }, HttpStatusCode.InternalServerError);
                }
            });
        }
    }

    /// <summary>
    /// Request model for inhibit command
    /// </summary>
    public class InhibitRequest
    {
        public string DstId { get; set; }
    }
}
