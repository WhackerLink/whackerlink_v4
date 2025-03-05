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
* Copyright (C) 2024-2025 Caleb, K4PHP
* 
*/

#nullable disable

using WhackerLinkLib.Models;

namespace WhackerLinkServer.Models
{
    /// <summary>
    /// Defines the config file
    /// </summary>
    public class Config
    {
        public SystemConfig System { get; set; }
        public List<MasterConfig> Masters { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public class SystemConfig
        {
            public bool Debug;
        }

        /// <summary>
        /// 
        /// </summary>
        public class MasterConfig
        {
            public string Name { get; set; }
            public string Address { get; set; }
            public int Port { get; set; }
            public float PreEncodeGain { get; set; } = 1.0f;
            public bool AffilationRestricted { get; set; } = true;
            public bool NoSelfRepeat { get; set; } = true;
            public bool EnableMbeTones { get; set; } = false;
            public bool DisableSiteBcast { get; set; } = false;
            public bool DisableVchUpdates { get; set; } = false;
            public bool DisableLocationBroadcasts { get; set; } = false;
            public bool DisableLocationBroadcastsRepeats { get; set; } = true;
            public bool DisableLocBcastLogs { get; set; }
            public AuthConfig Auth { get; set; }
            public SslConfig Ssl { get; set; }
            public RestConfig Rest { get; set; }
            public ReporterConfiguration Reporter { get; set; }
            public VocoderModes VocoderMode { get; set; }
            public RidAclConfiguration RidAcl { get; set; }
            public List<Site> Sites { get; set; }
        }

        /// <summary>
        /// 
        /// </summary>
        public class AuthConfig
        {
            public bool Enabled { get; set; }
            public string Path { get; set; }
            public int ReloadInterval { get; set; }
        }

        /// <summary>
        /// 
        /// </summary>
        public class SslConfig
        {
            public bool Enabled { get; set; }
            public string CertificatePath { get; set; }
            public string CertificatePassword { get; set; }
        }

        /// <summary>
        /// 
        /// </summary>
        public class RestConfig
        {
            public bool Enabled { get; set; }
            public string Address { get; set; }
            public int Port { get; set; }
        }

        /// <summary>
        /// 
        /// </summary>
        public class ReporterConfiguration
        {
            public bool Enabled { get; set; }
            public string Address { get; set; }
            public int Port { get; set; }
        }

        /// <summary>
        /// 
        /// </summary>
        public class RidAclConfiguration
        {
            public bool Enabled { get; set; }
            public string Path { get; set; }
            public int ReloadInterval { get; set; }
        }
    }
}