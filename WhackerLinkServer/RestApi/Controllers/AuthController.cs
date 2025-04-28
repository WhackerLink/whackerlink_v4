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
using WhackerLinkServer.Managers;

namespace WhackerLinkServer.RestApi.Modules
{
    [ApiController]
    [Route("api/{masterName}/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IMasterServiceRegistry _registry;
        public AuthController(IMasterServiceRegistry registry) => _registry = registry;

        [HttpPost("reload")]
        public IActionResult Reload([FromRoute] string masterName)
        {
            if (!_registry.TryGet(masterName, out var master))
                return NotFound(new { error = $"Master '{masterName}' not found" });

            AuthKeyFileManager authManager = master.GetAuthManager();
            authManager.ReloadAuthFile();
            return Ok(new { success = true });
        }
    }
}
