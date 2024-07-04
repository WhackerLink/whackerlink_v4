using System;
using System.Collections.Generic;

#nullable disable

namespace WhackerLinkCommonLib.Utils
{
    public static class AudioConverter
    {
        public const int OriginalPcmLength = 1600;
        public const int ExpectedPcmLength = 320;

        public static List<byte[]> SplitToChunks(byte[] audioData)
        {
            List<byte[]> chunks = new List<byte[]>();

            if (audioData.Length != OriginalPcmLength)
            {
                Console.WriteLine($"Invalid PCM length: {audioData.Length}, expected: {OriginalPcmLength}");
                return chunks;
            }

            for (int offset = 0; offset < OriginalPcmLength; offset += ExpectedPcmLength)
            {
                byte[] chunk = new byte[ExpectedPcmLength];
                Buffer.BlockCopy(audioData, offset, chunk, 0, ExpectedPcmLength);
                chunks.Add(chunk);
            }

            return chunks;
        }

        public static byte[] CombineChunks(List<byte[]> chunks)
        {
            if (chunks.Count * ExpectedPcmLength != OriginalPcmLength)
            {
                Console.WriteLine($"Invalid number of chunks: {chunks.Count}, expected total length: {OriginalPcmLength}");
                return null;
            }

            byte[] combined = new byte[OriginalPcmLength];
            int offset = 0;

            foreach (var chunk in chunks)
            {
                Buffer.BlockCopy(chunk, 0, combined, offset, ExpectedPcmLength);
                offset += ExpectedPcmLength;
            }

            return combined;
        }
    }
}