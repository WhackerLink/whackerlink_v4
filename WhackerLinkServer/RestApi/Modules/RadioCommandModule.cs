/*
 * Copyright (C) 2024-2025 Caleb H. (K4PHP) caleb.k4php@gmail.com
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

            Post("/netfail", parameters =>
            {
                try
                {
                    var requestBody = this.Bind<NetFailRequest>();

                    if (requestBody == null)
                    {
                        return Response.AsJson(new { error = "Invalid request. 'Status' is required." }, HttpStatusCode.BadRequest);
                    }

                    NET_FAIL packet = new NET_FAIL
                    {
                        Status = requestBody.Status
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

    /// <summary>
    /// Request model for net fail request
    /// </summary>
    public class NetFailRequest
    {
        public byte Status { get; set; } = 0xFF;
    }
}


