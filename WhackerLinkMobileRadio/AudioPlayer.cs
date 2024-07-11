/*
* WhackerLink - WhackerLinkMobileRadio
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

using System;
using System.IO;
using System.Media;
using System.Reflection;
using System.Windows;

#nullable disable

namespace WhackerLinkCommonLib.Utils
{
    /// <summary>
    /// Helper class to play a souund file
    /// </summary>
    public static class AudioPlayer
    {
        /// <summary>
        /// Play sound
        /// </summary>
        /// <param name="stream"></param>
        public static void PlaySound(UnmanagedMemoryStream stream)
        {
            try
            {
                using (var soundPlayer = new SoundPlayer(stream))
                {
                    soundPlayer.Play();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while playing sound: {ex.Message}");
            }
        }
    }
}