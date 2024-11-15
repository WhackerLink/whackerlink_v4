using WhackerLinkLib.Models;

#nullable disable

namespace WhackerLinkBridge
{
    internal static class DvmUtils
    {
        internal static async Task SendToDvm(WhackerLinkBridgeApp app, byte[] audioData, VoiceChannel voiceChannel)
        {
            try
            {
                if (app._callInProgress) return;

                if (voiceChannel.DstId != app._config.system.dstId)
                    return;

                int originalPcmLength = 1600;
                int expectedPcmLength = 320;

                if (audioData.Length != originalPcmLength)
                {
                    Console.WriteLine($"Invalid PCM length: {audioData.Length}, expected: {originalPcmLength}");
                    return;
                }

                if (!app._txInProgress)
                {
                    Console.WriteLine("TX Call Start Detected");
                    app._txInProgress = true;
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

                    await app._udpAudioHandler.SendAudio(buffer);
                }

                app._txHangTimer.Stop();
                app._txHangTimer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandleWebSocketAudioData: {ex.Message}");
            }
        }

        internal static async Task HandleInboundDvm(WhackerLinkBridgeApp app)
        {
            const int chunkSize = 320;
            const int originalPcmLength = 1600;
            List<byte> audioBuffer = new List<byte>();

            while (true)
            {
                var audioData = await app._udpAudioHandler.ReceiveAudio();
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

                    if (!app._callInProgress)
                    {
                        app.SendVoiceChannelRequest(srcId.ToString(), dstId.ToString());
                        Console.WriteLine("Call Start Detected");
                        app._callInProgress = true;
                    }

                    if (app.isGranted)
                    {
                        app._webSocketHandler.SendMessage(new
                        {
                            type = (int)PacketType.AUDIO_DATA,
                            data = fullPcmData,
                            voiceChannel = app.currentVoiceChannel
                        });
                    }
                    else
                    {
                        Console.WriteLine("Voice channel not granted; skipping audio");
                    }

                    app._rxHangTimer.Stop();
                    app._rxHangTimer.Start();
                }
            }
        }
    }
}