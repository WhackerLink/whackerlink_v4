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
    [Route("api/{masterName}/command")]
    public class RadioCommandController : ControllerBase
    {
        private readonly IMasterServiceRegistry _registry;
        public RadioCommandController(IMasterServiceRegistry registry)
        {
            _registry = registry;
        }

        public class InhibitRequest { public string DstId { get; set; } }
        public class NetFailRequest { public byte Status { get; set; } = 0xFF; }

        [HttpPost("inhibit")]
        public IActionResult Inhibit(
            [FromRoute] string masterName,
            [FromBody] InhibitRequest req)
        {
            if (!_registry.TryGet(masterName, out var master))
                return NotFound(new { error = $"Master '{masterName}' not found" });

            if (string.IsNullOrWhiteSpace(req?.DstId))
                return BadRequest(new { error = "DstId is required" });

            var packet = new SPEC_FUNC
            {
                Function = SpecFuncType.RID_INHIBIT,
                SrcId = Defines.FNE_LID.ToString(),
                DstId = req.DstId
            };
            master.Logger.Information(packet.ToString());
            master.BroadcastPacket(packet.GetStrData());
            return Ok(new { success = true });
        }

        [HttpPost("uninhibit")]
        public IActionResult Uninhibit(
            [FromRoute] string masterName,
            [FromBody] InhibitRequest req)
        {
            if (!_registry.TryGet(masterName, out var master))
                return NotFound(new { error = $"Master '{masterName}' not found" });

            if (string.IsNullOrWhiteSpace(req?.DstId))
                return BadRequest(new { error = "DstId is required" });

            var packet = new SPEC_FUNC
            {
                Function = SpecFuncType.RID_UNINHIBIT,
                SrcId = Defines.FNE_LID.ToString(),
                DstId = req.DstId
            };
            master.Logger.Information(packet.ToString());
            master.BroadcastPacket(packet.GetStrData());
            return Ok(new { success = true });
        }

        [HttpPost("page")]
        public IActionResult Page(
            [FromRoute] string masterName,
            [FromBody] InhibitRequest req)
        {
            if (!_registry.TryGet(masterName, out var master))
                return NotFound(new { error = $"Master '{masterName}' not found" });

            if (string.IsNullOrWhiteSpace(req?.DstId))
                return BadRequest(new { error = "DstId is required" });

            var packet = new CALL_ALRT
            {
                SrcId = Defines.FNE_LID.ToString(),
                DstId = req.DstId
            };
            master.Logger.Information(packet.ToString());
            master.BroadcastPacket(packet.GetStrData());
            return Ok(new { success = true });
        }

        [HttpPost("netfail")]
        public IActionResult NetFail(
            [FromRoute] string masterName,
            [FromBody] NetFailRequest req)
        {
            if (!_registry.TryGet(masterName, out var master))
                return NotFound(new { error = $"Master '{masterName}' not found" });

            var packet = new NET_FAIL { Status = req.Status };
            master.Logger.Information(packet.ToString());
            master.BroadcastPacket(packet.GetStrData());
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


