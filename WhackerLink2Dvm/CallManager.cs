using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using fnecore.P25;
using WhackerLinkLib.Models;
using WhackerLinkLib.Vocoder;
using WhackerLinkServer.Models;

namespace WhackerLink2Dvm
{
    /// <summary>
    /// Holds all info about a single call
    /// </summary>
    public class CallInfo
    {
        public uint SrcId { get; }
        public uint DstId { get; }
        public uint txStreamId { get; set; }
        public VoiceChannel VoiceChannel { get; set; }
        public SlotStatus[] Status { get; set; } = new SlotStatus[3];
        public DateTime StartTime { get; }

        public byte[] netLDU1 = new byte[9 * 25];
        public byte[] netLDU2 = new byte[9 * 25];
        public uint p25SeqNo = 0;
        public byte p25N = 0;
        public bool ignoreCall = false;

        public byte callAlgoId = P25Defines.P25_ALGO_UNENCRYPT;

#if WINDOWS
        public VocoderManager ExtFullRateVocoder = null;
        public VocoderManager ExtHalfRateVocoder = null;
#endif

        public List<byte[]> accumulatedChunks = new List<byte[]>();

        public CallInfo(uint srcId, uint dstId)
        {
            SrcId = srcId;
            DstId = dstId;
            Status = new SlotStatus[3];
            Status[2] = new SlotStatus();  // P25
            StartTime = DateTime.UtcNow;
            VoiceChannel = null;

#if !NOVODODE && WINDOWS
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);

            bool hasVocoder = false;

            if (File.Exists(Path.Combine(new string[] { Path.GetDirectoryName(path), "AMBE.DLL" })))
            {
                WhackerLink2Dvm.logger.Information($"Using DVSI USB Vocoder");
                hasVocoder = true;
            }
            else if (File.Exists(Path.Combine(new string[] { Path.GetDirectoryName(path), "res1033.dll" })))
            {
                WhackerLink2Dvm.logger.Information($"Using DSP INI Vocoder in DMR AMBE");
                WhackerLink2Dvm.logger.Warning("P25 will NOT work due to enabled vocoder not supporting IMBE/Full rate");
                hasVocoder = true;
            }
            else if (File.Exists(Path.Combine(new string[] { Path.GetDirectoryName(path), "libvocoder.dll" })))
            {
                WhackerLink2Dvm.logger.Information($"Using DVM Vocoder");
                hasVocoder = true;
            }

            if (hasVocoder)
            {
                ExtFullRateVocoder = new VocoderManager();
                ExtHalfRateVocoder = new VocoderManager(false);
            }
            else
            {
                throw new DllNotFoundException("No vocoder DLL is present!");
            }
#endif
        }
    }

    /// <summary>
    /// Manages active calls
    /// </summary>
    public class CallManager
    {
        // SrcId → CallInfo
        private readonly ConcurrentDictionary<uint, CallInfo> _calls = new();

        // DstId → SrcId
        private readonly ConcurrentDictionary<uint, uint> _dstToSrc = new();

        /// <summary>
        /// Tracks status objects by slot-type
        /// </summary>
        public ConcurrentDictionary<uint, SlotStatus> Status { get; }
            = new ConcurrentDictionary<uint, SlotStatus>();

        /// <summary>
        /// Only IDs in this list will ever get a CallInfo.
        /// </summary>
        public IReadOnlyCollection<uint> AllowedGroups { get; }

        public CallManager(IEnumerable<uint> allowedGroups)
        {
            AllowedGroups = allowedGroups.ToList().AsReadOnly();
        }

        public CallInfo GetOrCreateCall(uint srcId, uint dstId)
        {
            if (!AllowedGroups.Contains(dstId))
                return null;

            if (_dstToSrc.TryGetValue(dstId, out var existingSrc))
                return GetCallInfo(existingSrc);

            if (!StartCall(srcId, dstId))
                return null;

            return GetCallInfoFromDst(dstId);
        }

        /// <summary>
        /// Starts a new call from srcId to the given dstIds
        /// </summary>
        public bool StartCall(uint srcId, uint dstId)
        {
            if (_calls.ContainsKey(srcId) || _dstToSrc.ContainsKey(dstId))
                return false;

            var info = new CallInfo(srcId, dstId);
            if (!_calls.TryAdd(srcId, info))
                return false;

            _dstToSrc[dstId] = srcId;

            return true;
        }

        /// <summary>
        /// Adds a new DstId to an existing call
        /// </summary>
        public bool AddDstToCall(uint srcId, uint dstId)
        {
            if (!_calls.TryRemove(srcId, out var info))
                return false;

            _dstToSrc.TryRemove(info.DstId, out _);
            return true;
        }

        /// <summary>
        /// Ends the call for the given srcId, removing all associated maps
        /// </summary>
        public bool EndCall(uint srcId)
        {
            if (!_calls.TryRemove(srcId, out var call))
                return false;

            _dstToSrc.TryRemove(call.DstId, out _);

            return true;
        }

        /// <summary>
        /// Gets the <see cref="CallInfo"> for a given srcId
        /// </summary>
        public CallInfo GetCallInfo(uint srcId)
            => _calls.TryGetValue(srcId, out var info) ? info : null;

        /// <summary>
        /// Gets the <see cref="CallInfo"/> for a given dstId
        /// </summary>
        public CallInfo GetCallInfoFromDst(uint dstId)
        {
            if (_dstToSrc.TryGetValue(dstId, out var srcId))
                return GetCallInfo(srcId);

            return null;
        }

        /// <summary>
        /// Looks up which srcId owns the given dstId
        /// </summary>
        public uint? GetSrcForDst(uint dstId)
            => _dstToSrc.TryGetValue(dstId, out var src) ? src : 0;

        /// <summary>
        /// Get all active calls
        /// </summary>
        public IEnumerable<CallInfo> GetAllActiveCalls()
            => _calls.Values;
    }
}
