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
using Nancy.Hosting.Self;
using Nancy.TinyIoc;
using WhackerLinkLib.Interfaces;

namespace WhackerLinkServer
{
    /// <summary>
    /// Class to create a REST API server
    /// </summary>
    public class RestApiServer
    {
        private readonly NancyHost _nancyHost;
        private IMasterService _masterService;
        private string url;

        /// <summary>
        /// Creates a instance of the REST API
        /// </summary>
        /// <param name="masterService"></param>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public RestApiServer(IMasterService masterService, string address, int port)
        {
            url = $"http://{address}:{port}";
            _masterService = masterService;

            var config = new HostConfiguration { UrlReservations = new UrlReservations { CreateAutomatically = true } };
            var bootstrapper = new CustomBootstrapper(masterService);
            _nancyHost = new NancyHost(new Uri(url), bootstrapper, config);
        }

        /// <summary>
        /// Start the server
        /// </summary>
        public void Start()
        {
            _nancyHost.Start();
            _masterService.Logger.Information($"REST server started at {url}");
        }

        /// <summary>
        /// Gracefully stop the server
        /// </summary>
        public void Stop()
        {
            _nancyHost?.Stop();
            _masterService.Logger.Information($"REST server ${url} stopped.");
        }
    }

    /// <summary>
    /// Nancy custom bootstrapper
    /// </summary>
    public class CustomBootstrapper : DefaultNancyBootstrapper
    {
        private readonly IMasterService _masterService;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="masterService"></param>
        public CustomBootstrapper(IMasterService masterService)
        {
            _masterService = masterService;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="container"></param>
        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);
            container.Register(_masterService);
        }
    }
}
