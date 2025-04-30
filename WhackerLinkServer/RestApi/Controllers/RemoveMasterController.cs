/*
 * Copyright (C) 2024-2025 Caleb H. (K4PHP) caleb.k4php@gmail.com
 * Copyright (C) 2025 Firav (firavdev@gmail.com)
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
using System.IO;

namespace WhackerLinkServer.RestApi.Controllers
{
    [ApiController]
    [Route("api/masters")]
    public class RemoveMasterController : ControllerBase
    {
        private readonly IMasterServiceRegistry _registry;

        public RemoveMasterController(IMasterServiceRegistry registry)
        {
            _registry = registry;
        }

        [HttpPost("{masterName}/remove")]
        public IActionResult DisableMaster([FromRoute] string masterName)
        {
            if (!_registry.TryGet(masterName, out var master))
                return NotFound(new { error = $"Master '{masterName}' not found" });

            // Stop and remove the master
            // Reflect success or failure in the status code
            bool success = Program.RemoveMaster(masterName);
            if (!success)
            {
                return StatusCode(500, new { error = $"Failed to stop and remove Master {masterName}." });
            }

            return Ok(new { success = true, message = $"Master '{masterName}' has been stopped and removed." });
        }
    }
}
