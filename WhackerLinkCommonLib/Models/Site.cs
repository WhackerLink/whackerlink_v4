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
* Copyright (C) 2024 Caleb, KO4UYJ
* 
*/

#nullable disable

namespace WhackerLinkCommonLib.Models
{
    public class Site
    {
        public string Name { get; set; }
        public string ControlChannel { get; set; }
        public List<string> VoiceChannels { get; set; }
        public Location Location { get; set; }
        public string SiteID { get; set; }
        public string SystemID { get; set; }
        public float Range { get; set; }
    }

    public class Location
    {
        public string X { get; set; }
        public string Y { get; set; }
        public string Z { get; set; }
    }
}