using NAudio.Wave.SampleProviders;
using NAudio.Wave;

namespace WhackerLinkMobileRadio
{
    public static class BeepGenerator
    {
        private static IWavePlayer waveOut = new WaveOutEvent();
        private static SignalGenerator signalGenerator = new SignalGenerator()
        {
            Gain = 1,
            Frequency = 310,
            Type = SignalGeneratorType.Sin
        };

        static BeepGenerator()
        {
            waveOut.Init(signalGenerator);
        }

        public static void Beep(double frequency, int duration)
        {
            signalGenerator.Frequency = frequency;
            waveOut.Play();
            Thread.Sleep(duration);
            waveOut.Stop();
        }

        public static void TptGenerate()
        {
            Beep(910, 30);
            Beep(0, 20);
            Beep(910, 30);
            Beep(0, 20);
            Beep(910, 30);
        }

        public static void Bonk()
        {
            Beep(310, 1000);
        }
    }
}