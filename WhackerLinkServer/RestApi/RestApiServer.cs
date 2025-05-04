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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using WhackerLinkLib.Interfaces;
using WhackerLinkLib.Models;
using WhackerLinkLib.Utils;

namespace WhackerLinkServer
{
    /// <summary>
    /// 
    /// </summary>
    public interface IMasterServiceRegistry
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="master"></param>
        /// <returns></returns>
        bool TryGet(string name, out IMasterService master);
    }

    /// <summary>
    /// 
    /// </summary>
    public class MasterServiceRegistry : IMasterServiceRegistry
    {
        readonly Dictionary<string, IMasterService> _map;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="masters"></param>
        public MasterServiceRegistry(IEnumerable<IMasterService> masters)
        {
            _map = masters.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="master"></param>
        /// <returns></returns>
        public bool TryGet(string name, out IMasterService master)
            => _map.TryGetValue(name, out master);
        /// <summary>
        /// Adds a new master to the registry.
        /// </summary>
        /// <param name="master">The master to add.</param>
        public void AddMaster(IMasterService master)
        {
            _map[master.Name] = master;
        }
    }

    /// <summary>
    /// REST API Server
    /// </summary>
    public class RestApiServer
    {
        private readonly WebApplication _app;
        private readonly IMasterServiceRegistry _registry;
        private readonly string _url;
        private readonly string _password;

        /// <summary>
        /// Creates an instance of <see cref="RestApiServer"/>
        /// </summary>
        /// <param name="masterService"></param>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public RestApiServer(
            IEnumerable<IMasterService> masters,
            string address,
            int port,
            string password)
        {
            _registry = new MasterServiceRegistry(masters);
            _url = $"http://{address}:{port}";
            _password = Util.ComputeSha256(password);

            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls(_url);

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.SetMinimumLevel(LogLevel.Error);

            builder.Services.AddSingleton<IMasterServiceRegistry>(_registry);

            builder.Services.AddControllers();

            _app = builder.Build();

            _app.Use(async (context, next) =>
            {
                if (!context.Request.Headers.TryGetValue(Defines.WL_REST_AUTH_HEADER, out var receivedHash))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized");
                    return;
                }

                if (!string.Equals(receivedHash, _password, StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized");
                    return;
                }

                await next.Invoke();
            });

            _app.MapControllers();
        }

        /// <summary>
        /// Adds additional masters to the MasterServiceRegistry.
        /// </summary>
        /// <param name="master">The additional masters to add.</param>
        public void AddMaster(IMasterService master)
        {
            var registry = _registry as MasterServiceRegistry;
            if (registry != null)
            {
                if (!registry.TryGet(master.Name, out _))
                {
                    // Add the new master to the registry
                    registry.AddMaster(master);
                }
            }
        }

        /// <summary>
        /// Removes a master from the MasterServiceRegistry.
        /// </summary>
        /// <param name="masterName">The name of the master to remove.</param>
        public void RemoveMaster(string masterName)
        {
            var registry = _registry as MasterServiceRegistry;
            if (registry != null)
            {
                if (registry.TryGet(masterName, out _))
                {
                    // Use a public method to remove the master instead of directly accessing the private field
                    var mapField = typeof(MasterServiceRegistry)
                        .GetField("_map", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (mapField != null)
                    {
                        var map = mapField.GetValue(registry) as Dictionary<string, IMasterService>;
                        map.Remove(masterName);
                    }
                }
            }
        }

        /// <summary>
        /// Start the REST server
        /// </summary>
        public void Start()
        {
            _app.Start();
        }

        /// <summary>
        /// Shut down the REST server
        /// </summary>
        public void Stop()
        {
            _app.StopAsync().GetAwaiter().GetResult();
        }
    }
}