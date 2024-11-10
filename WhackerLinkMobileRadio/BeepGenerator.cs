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
* Copyright (C) 2024 Caleb, K4PHP
* 
*/

using NAudio.Wave.SampleProviders;
using NAudio.Wave;

namespace WhackerLinkMobileRadio
{
    /// <summary>
    /// Classto make da beeps
    /// </summary>
    public static class BeepGenerator
    {
        private static IWavePlayer waveOut = new WaveOutEvent();
        private static SignalGenerator signalGenerator = new SignalGenerator()
        {
            Gain = 1,
            Frequency = 310,
            Type = SignalGeneratorType.Sin
        };

        /// <summary>
        /// Creates beep gen
        /// </summary>
        static BeepGenerator()
        {
            waveOut.Init(signalGenerator);
        }

        /// <summary>
        /// Beep
        /// </summary>
        /// <param name="frequency"></param>
        /// <param name="duration"></param>
        public static void Beep(double frequency, int duration)
        {
            signalGenerator.Frequency = frequency;
            waveOut.Play();
            Thread.Sleep(duration);
            waveOut.Stop();
        }

        /// <summary>
        /// Whacker tone
        /// </summary>
        public static void TptGenerate()
        {
            Beep(910, 30);
            Beep(0, 20);
            Beep(910, 30);
            Beep(0, 20);
            Beep(910, 30);
        }

        /// <summary>
        /// Bonk
        /// </summary>
        public static void Bonk()
        {
            Beep(310, 1000);
        }
    }
}