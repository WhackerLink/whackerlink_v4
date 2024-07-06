using System;
using System.IO;
using System.Media;
using System.Reflection;
using System.Windows;

#nullable disable

namespace WhackerLinkCommonLib.Utils
{
    public static class AudioPlayer
    {
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