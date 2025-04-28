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

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WhackerLinkLib.Interfaces;

namespace WhackerLinkServer
{
    /// <summary>
    /// REST API Server
    /// </summary>
    public class RestApiServer
    {
        private readonly WebApplication _app;
        private readonly IMasterService _masterService;
        private readonly string _url;

        /// <summary>
        /// Creates an instance of <see cref="RestApiServer"/>
        /// </summary>
        /// <param name="masterService"></param>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public RestApiServer(IMasterService masterService, string address, int port)
        {
            _masterService = masterService;
            _url = $"http://{address}:{port}";

            var builder = WebApplication.CreateBuilder();

            builder.WebHost.UseUrls(_url);

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.SetMinimumLevel(LogLevel.Error);

            builder.Services.AddSingleton(_masterService);

            builder.Services.AddControllers();

            _app = builder.Build();

            _app.MapControllers();
        }

        /// <summary>
        /// Start the REST server
        /// </summary>
        public void Start()
        {
            _masterService.Logger.Information($"REST server starting at {_url}");
            _app.Start();
        }

        /// <summary>
        /// Shut down the REST server
        /// </summary>
        public void Stop()
        {
            _app.StopAsync().GetAwaiter().GetResult();
            _masterService.Logger.Information($"REST server at {_url} stopped");
        }
    }
}