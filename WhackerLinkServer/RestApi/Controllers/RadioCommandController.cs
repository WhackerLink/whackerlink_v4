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

using Microsoft.AspNetCore.Mvc;
using WhackerLinkLib.Interfaces;
using WhackerLinkLib.Models;
using WhackerLinkLib.Models.IOSP;

namespace WhackerLinkServer.RestApi.Modules
{
    [ApiController]
    [Route("api/command")]
    public class RadioCommandController : ControllerBase
    {
        readonly IMasterService _svc;
        public RadioCommandController(IMasterService svc) => _svc = svc;

        public class InhibitRequest { public string DstId { get; set; } }
        public class NetFailRequest { public byte Status { get; set; } = 0xFF; }

        [HttpPost("inhibit")]
        public IActionResult Inhibit([FromBody] InhibitRequest req)
        {
            if (string.IsNullOrWhiteSpace(req?.DstId))
                return BadRequest(new { error = "DstId is required" });

            var packet = new SPEC_FUNC
            {
                Function = SpecFuncType.RID_INHIBIT,
                SrcId = Defines.FNE_LID.ToString(),
                DstId = req.DstId
            };
            _svc.Logger.Information(packet.ToString());
            _svc.BroadcastPacket(packet.GetStrData());
            return Ok(new { success = true });
        }

        [HttpPost("uninhibit")]
        public IActionResult Uninhibit([FromBody] InhibitRequest req)
        {
            if (string.IsNullOrWhiteSpace(req?.DstId))
                return BadRequest(new { error = "DstId is required" });

            var packet = new SPEC_FUNC
            {
                Function = SpecFuncType.RID_UNINHIBIT,
                SrcId = Defines.FNE_LID.ToString(),
                DstId = req.DstId
            };
            _svc.Logger.Information(packet.ToString());
            _svc.BroadcastPacket(packet.GetStrData());
            return Ok(new { success = true });
        }

        [HttpPost("netfail")]
        public IActionResult NetFail([FromBody] NetFailRequest req)
        {
            var packet = new NET_FAIL { Status = req.Status };
            _svc.Logger.Information(packet.ToString());
            _svc.BroadcastPacket(packet.GetStrData());
            return Ok(new { success = true });
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


