using System;
using System.Collections.Concurrent;
using System.Threading;
using NWaves.Filters;
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
        private readonly ConcurrentDictionary<string, (MBEDecoder Decoder, MBEEncoder Encoder, NotchFilter filter)> vocoderInstances;
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
            vocoderInstances = new ConcurrentDictionary<string, (MBEDecoder, MBEEncoder, NotchFilter filter)>();
        }

        /// <summary>
        /// Retrieves or creates a vocoder instance for a given channel.
        /// </summary>
        public (MBEDecoder Decoder, MBEEncoder Encoder, NotchFilter filter) GetOrCreateVocoder(string channelId, VocoderModes mode)
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

        private (MBEDecoder, MBEEncoder, NotchFilter) CreateVocoderInstance(VocoderModes mode)
        {
            try
            {
                MBE_MODE mbeMode = (mode == VocoderModes.IMBE) ? MBE_MODE.IMBE_88BIT : MBE_MODE.DMR_AMBE;
                return (new MBEDecoder(mbeMode), new MBEEncoder(mbeMode), new NotchFilter(2500));
            } catch(Exception ex)
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
