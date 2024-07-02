using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using WhackerLinkBridge.Models;
using WhackerLinkCommonLib.Handlers;
using WhackerLinkCommonLib.Interfaces;
using WhackerLinkCommonLib.Models;
using WhackerLinkCommonLib.Models.IOSP;
using WhackerLinkCommonLib.Models.Radio;

#nullable disable

namespace WhackerLinkBridge
{
    public class WhackerLinkBridgeApp
    {
        private readonly Config _config;
        private readonly IWebSocketHandler _webSocketHandler;
        private readonly UdpAudioHandler _udpAudioHandler;

        private VoiceChannel currentVoiceChannel;
        private bool isGranted;
        private bool _callInProgress;
        private bool _txInProgress;
        private System.Timers.Timer _rxHangTimer;
        private System.Timers.Timer _txHangTimer;

        public WhackerLinkBridgeApp(string configPath)
        {
            _config = ConfigLoader.LoadConfig(configPath);
            _webSocketHandler = new WebSocketHandler();
            _udpAudioHandler = new UdpAudioHandler(
                _config.dvmBridge.Address,
                _config.dvmBridge.TxPort,
                "0.0.0.0",
                _config.dvmBridge.RxPort);

            _webSocketHandler.OnAudioData += HandleWebSocketAudioData;
            _webSocketHandler.OnVoiceChannelResponse += HandleVoiceChannelResponse;

            _rxHangTimer = new System.Timers.Timer(500);
            _rxHangTimer.Elapsed += OnRxHangTimeElapsed;
            _rxHangTimer.AutoReset = false;

            _txHangTimer = new System.Timers.Timer(500);
            _txHangTimer.Elapsed += OnTxHangTimeElapsed;
            _txHangTimer.AutoReset = false;
        }

        public void Start()
        {
            _webSocketHandler.Connect(_config.master.Address, _config.master.Port);
            Task.Run(() => ListenToUdpAudio());
        }

        private void HandleVoiceChannelResponse(GRP_VCH_RSP response)
        {
            if (response.Status == (int)ResponseType.GRANT)
            {
                currentVoiceChannel = new VoiceChannel
                {
                    Frequency = response.Channel,
                    SrcId = response.SrcId,
                    DstId = response.DstId
                };

                isGranted = true;
            }
            else
            {
                isGranted = false;
            }
        }

        private void SendVoiceChannelRequest(string srcId, string dstId)
        {
            var request = new
            {
                type = (int)PacketType.GRP_VCH_REQ,
                data = new GRP_VCH_REQ
                {
                    SrcId = srcId,
                    DstId = dstId,
                }
            };

            Console.WriteLine(request.ToString());
            _webSocketHandler.SendMessage(request);
        }

        private void SendVoiceChannelRelease(VoiceChannel voiceChannel)
        {
            var request = new
            {
                type = (int)PacketType.GRP_VCH_RLS,
                data = new GRP_VCH_RLS
                {
                    SrcId = voiceChannel.SrcId,
                    DstId = voiceChannel.DstId,
                    Channel = voiceChannel.Frequency
                }
            };

            _webSocketHandler.SendMessage(request);
        }

        private async Task ListenToUdpAudio()
        {
            const int chunkSize = 320;
            const int originalPcmLength = 1600;
            List<byte> audioBuffer = new List<byte>();

            while (true)
            {
                var audioData = await _udpAudioHandler.ReceiveAudio();
                if (audioData.Length < 12)
                {
                    Console.WriteLine("Invalid audio data received.");
                    continue;
                }

                var lengthBytes = audioData.Take(4).ToArray();
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(lengthBytes);
                }
                var pcmLength = BitConverter.ToInt32(lengthBytes, 0);

                if (pcmLength != chunkSize || pcmLength + 12 != audioData.Length)
                {
                    Console.WriteLine($"Mismatch in expected lengths. PCM Length: {pcmLength}, Audio Data Length: {audioData.Length}");
                    continue;
                }

                var pcm = new byte[pcmLength];
                Buffer.BlockCopy(audioData, 4, pcm, 0, pcmLength);

                var srcId = BitConverter.ToUInt32(audioData, pcmLength + 4);
                var dstId = BitConverter.ToUInt32(audioData, pcmLength + 8);

                if (BitConverter.IsLittleEndian)
                {
                    dstId = BitConverter.ToUInt32(audioData.Skip(pcmLength + 4).Take(4).Reverse().ToArray(), 0);
                    srcId = BitConverter.ToUInt32(audioData.Skip(pcmLength + 8).Take(4).Reverse().ToArray(), 0);
                }

                audioBuffer.AddRange(pcm);

                if (audioBuffer.Count >= originalPcmLength)
                {
                    var fullPcmData = audioBuffer.Take(originalPcmLength).ToArray();
                    audioBuffer.RemoveRange(0, originalPcmLength);

                    Console.WriteLine($"RX UDP CALL, srcId: {srcId}, dstId: {dstId}");

                    if (!_callInProgress)
                    {
                        SendVoiceChannelRequest(srcId.ToString(), dstId.ToString());
                        Console.WriteLine("Call Start Detected");
                        _callInProgress = true;
                    }

                    if (isGranted)
                    {
                        _webSocketHandler.SendMessage(new
                        {
                            type = (int)PacketType.AUDIO_DATA,
                            data = fullPcmData,
                            voiceChannel = currentVoiceChannel
                        });
                    }
                    else
                    {
                        Console.WriteLine("Voice channel not granted; skipping audio");
                    }

                    _rxHangTimer.Stop();
                    _rxHangTimer.Start();
                }
            }
        }

        private void OnRxHangTimeElapsed(object sender, ElapsedEventArgs e)
        {
            if (_callInProgress)
            {
                Console.WriteLine("Call End Detected");
                isGranted = false;
                SendVoiceChannelRelease(currentVoiceChannel);
                _callInProgress = false;
            }
        }

        private async void HandleWebSocketAudioData(byte[] audioData, VoiceChannel voiceChannel)
        {
            try
            {
                if (_callInProgress) return;

                if (voiceChannel.DstId != _config.system.dstId)
                    return;

                int originalPcmLength = 1600;
                int expectedPcmLength = 320;

                if (audioData.Length != originalPcmLength)
                {
                    Console.WriteLine($"Invalid PCM length: {audioData.Length}, expected: {originalPcmLength}");
                    return;
                }

                if (!_txInProgress)
                {
                    Console.WriteLine("TX Call Start Detected");
                    _txInProgress = true;
                }

                for (int offset = 0; offset < originalPcmLength; offset += expectedPcmLength)
                {
                    byte[] chunk = new byte[expectedPcmLength];
                    Buffer.BlockCopy(audioData, offset, chunk, 0, expectedPcmLength);

                    byte[] buffer = new byte[expectedPcmLength + 12];
                    byte[] lengthBytes = BitConverter.GetBytes(expectedPcmLength);
                    byte[] srcIdBytes = BitConverter.GetBytes(Convert.ToUInt32(voiceChannel.SrcId));
                    byte[] dstIdBytes = BitConverter.GetBytes(Convert.ToUInt32(voiceChannel.DstId));

                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(lengthBytes);
                        Array.Reverse(srcIdBytes);
                        Array.Reverse(dstIdBytes);
                    }

                    Buffer.BlockCopy(lengthBytes, 0, buffer, 0, 4);
                    Buffer.BlockCopy(chunk, 0, buffer, 4, expectedPcmLength);
                    Buffer.BlockCopy(dstIdBytes, 0, buffer, expectedPcmLength + 4, 4);
                    Buffer.BlockCopy(srcIdBytes, 0, buffer, expectedPcmLength + 8, 4);

                    Console.WriteLine($"TX UDP CALL, srcId: {voiceChannel.SrcId}, dstId: {voiceChannel.DstId}");

                    await _udpAudioHandler.SendAudio(buffer);
                }

                _txHangTimer.Stop();
                _txHangTimer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandleWebSocketAudioData: {ex.Message}");
            }
        }

        private void OnTxHangTimeElapsed(object sender, ElapsedEventArgs e)
        {
            if (_txInProgress)
            {
                Console.WriteLine("TX Call End Detected");
                _txInProgress = false;
            }
        }
    }
}