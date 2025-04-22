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
using System.Collections.Concurrent;
using System.Threading;
using NWaves.Filters;
using NWaves.Filters.Base;
using NWaves.Filters.BiQuad;
using NWaves.Filters.Butterworth;
using NWaves.Signals;
using Serilog;
using WhackerLinkServer.Models;

namespace WhackerLinkServer
{
    /// <summary>
    /// 
    /// </summary>
    public class VocoderManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, (MBEDecoder Decoder, MBEEncoder Encoder, IFilter[] filter)> vocoderInstances;
        private readonly object lockObj = new object();
        private bool disposed = false;
        private readonly ILogger logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        public VocoderManager(ILogger logger)
        {
            this.logger = logger;
            vocoderInstances = new ConcurrentDictionary<string, (MBEDecoder, MBEEncoder, IFilter[] filter)>();
        }

        /// <summary>
        /// Retrieves or creates a vocoder instance for a given channel.
        /// </summary>
        public (MBEDecoder Decoder, MBEEncoder Encoder, IFilter[] Filters) GetOrCreateVocoder(string channelId, VocoderModes mode)
        {
            if (disposed) throw new ObjectDisposedException(nameof(VocoderManager));

            return vocoderInstances.GetOrAdd(channelId, _ =>
            {
                logger.Information("Created new vocoder instance for channel {ChannelId}", channelId);
                return CreateVocoderInstance(mode);
            });
        }

        /// <summary>
        /// Removes and disposes of a vocoder instance.
        /// </summary>
        public void RemoveVocoder(string channelId)
        {
            if (vocoderInstances.TryRemove(channelId, out var instance))
            {
                lock (lockObj)
                {
                    //instance.Decoder.Dispose();
                    //instance.Encoder.Dispose();
                    logger.Information("Removed vocoder instance for channel {ChannelId}", channelId);
                }
            }
        }

        private (MBEDecoder, MBEEncoder, IFilter[]) CreateVocoderInstance(VocoderModes mode)
        {
            try
            {
                MBE_MODE mbeMode = (mode == VocoderModes.IMBE) ? MBE_MODE.IMBE_88BIT : MBE_MODE.DMR_AMBE;

                var decoder = new MBEDecoder(mbeMode);
                var encoder = new MBEEncoder(mbeMode);

                // Define Filters
                //var notchFilter = new NotchFilter(2500);
                //var lowPassFilter = new NWaves.Filters.Butterworth.LowPassFilter(3400 / 8000.0, 8); // Removes high-frequency noise
                //var highPassFilter = new NWaves.Filters.Butterworth.HighPassFilter(300 / 8000.0, 8); // Removes low-frequency hum
               // var bandPassFilter = new NWaves.Filters.Butterworth.BandPassFilter(250 / 8000.0, 3000 / 8000.0, 8); // Restricts to human speech range

                return (decoder, encoder, new IFilter[] {  });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return (null, null, null);
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                foreach (var instance in vocoderInstances.Values)
                {
                    //instance.Decoder.Dispose();
                    //instance.Encoder.Dispose();
                }
                vocoderInstances.Clear();
                disposed = true;
            }
        }
    }
}
