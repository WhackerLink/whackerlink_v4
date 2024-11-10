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
* Copyright (C) 2024 Caleb, K4PHP
* 
*/

using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhackerLinkCommonLib.Models;
using WhackerLinkServer.Models;

#nullable disable

namespace WhackerLinkServer.Managers
{
    /// <summary>
    /// Class to manage voice channels
    /// </summary>
    public class VoiceChannelManager
    {
        public List<VoiceChannel> VoiceChannels { get; private set; }

        /// <summary>
        /// Creates an instance of channel grant manager
        /// </summary>
        public VoiceChannelManager()
        {
            VoiceChannels = new List<VoiceChannel>();
        }

        /// <summary>
        /// Gets the list of active voice channels
        /// </summary>
        /// <returns></returns>
        public List<VoiceChannel> GetVoiceChannels()
        {
            return VoiceChannels;
        }

        /// <summary>
        /// Lookup voice channel by client id
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public VoiceChannel FindVoiceChannelByClientId(string clientId)
        {
            return VoiceChannels.FirstOrDefault(vc => vc.ClientId == clientId);
        }

        /// <summary>
        /// Lookup voice channel by dstid
        /// </summary>
        /// <param name="dstId"></param>
        /// <returns></returns>
        public VoiceChannel FindVoiceChannelByDstId(string dstId)
        {
            return VoiceChannels.FirstOrDefault(vc => vc.DstId == dstId);
        }

        /// <summary>
        /// Helper to add voicee channel to the list
        /// </summary>
        /// <param name="voiceChannel"></param>
        public void AddVoiceChannel(VoiceChannel voiceChannel)
        {
            if (!IsVoiceChannelActive(voiceChannel))
            {
                VoiceChannels.Add(voiceChannel);
            }
            else
            {
                Console.WriteLine($"VoiceChannel with Frequency: {voiceChannel.Frequency} already active. Skipping...");
            }
        }

        /// <summary>
        /// Helper to remove the voice channel from the list
        /// </summary>
        /// <param name="frequency"></param>
        public void RemoveVoiceChannel(string frequency)
        {
            VoiceChannels.RemoveAll(vc => vc.Frequency == frequency);
        }

        /// <summary>
        /// Helper to remove voice channel by clientid
        /// </summary>
        /// <param name="clientId"></param>
        public void RemoveVoiceChannelByClientId(string clientId)
        {
            VoiceChannels.RemoveAll(vc => vc.ClientId == clientId);
        }

        /// <summary>
        /// Helper to removee channell by dst id
        /// </summary>
        /// <param name="dstId"></param>
        public void RemoveVoiceChannelByDstId(string dstId)
        {
            VoiceChannels.RemoveAll(vc => vc.DstId == dstId);
        }

        /// <summary>
        /// Helper to check if voice channel is active
        /// </summary>
        /// <param name="voiceChannel"></param>
        /// <returns></returns>
        public bool IsVoiceChannelActive(VoiceChannel voiceChannel)
        {
            return VoiceChannels.Any(vc => vc.Frequency == voiceChannel.Frequency);
        }

        /// <summary>
        /// Checks if dst id is active
        /// </summary>
        /// <param name="dstId"></param>
        /// <returns></returns>
        public bool IsDestinationActive(string dstId)
        {
            return VoiceChannels.Any(vc => vc.DstId == dstId);
        }

        /// <summary>
        /// Checks if is active
        /// </summary>
        /// <param name="dstId"></param>
        /// <returns></returns>
        public bool IsTransmissionActiveForDstId(string dstId)
        {
            return VoiceChannels.Any(vc => vc.DstId == dstId && vc.IsActive);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Active VoiceChannels:");

            foreach (var voiceChannel in VoiceChannels)
            {
                sb.AppendLine($"DstId: {voiceChannel.DstId}, SrcId: {voiceChannel.SrcId}, Frequency: {voiceChannel.Frequency}");
            }

            return sb.ToString();
        }
    }
}