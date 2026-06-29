using System;
using System.IO;
using ProjekatScada.Models.Enums;

namespace ProjekatScada.Services
{
    public static class AlarmToneGenerator
    {
        private const int SampleRate = 22050;

        public static byte[] CreateWav(AlarmSoundProfile profile)
        {
            short[] samples;
            switch (profile)
            {
                case AlarmSoundProfile.Siren:
                    samples = CreateSirenSamples(3.0);
                    break;
                case AlarmSoundProfile.Buzzer:
                    samples = CreateBuzzerSamples(3.0);
                    break;
                case AlarmSoundProfile.AlertPulse:
                    samples = CreateAlertPulseSamples(3.0);
                    break;
                default:
                    samples = CreateClassicBeepSamples(3.0);
                    break;
            }

            return BuildWavFile(samples);
        }

        public static string WriteTempWav(AlarmSoundProfile profile)
        {
            var path = Path.Combine(Path.GetTempPath(), string.Format("scada_alarm_{0}.wav", profile));
            File.WriteAllBytes(path, CreateWav(profile));
            return path;
        }

        private static short[] CreateClassicBeepSamples(double durationSeconds)
        {
            var totalSamples = (int)(SampleRate * durationSeconds);
            var samples = new short[totalSamples];
            var cycleLength = SampleRate / 2;

            for (var i = 0; i < totalSamples; i++)
            {
                var cyclePosition = i % cycleLength;
                var isOn = cyclePosition < cycleLength / 2;
                samples[i] = isOn ? GenerateSineSample(i, 880, 0.55) : (short)0;
            }

            return samples;
        }

        private static short[] CreateSirenSamples(double durationSeconds)
        {
            var totalSamples = (int)(SampleRate * durationSeconds);
            var samples = new short[totalSamples];
            var sweepLength = SampleRate;

            for (var i = 0; i < totalSamples; i++)
            {
                var sweepPosition = (i % sweepLength) / (double)sweepLength;
                var frequency = 450 + (750 * sweepPosition);
                samples[i] = GenerateSineSample(i, frequency, 0.5);
            }

            return samples;
        }

        private static short[] CreateBuzzerSamples(double durationSeconds)
        {
            var totalSamples = (int)(SampleRate * durationSeconds);
            var samples = new short[totalSamples];

            for (var i = 0; i < totalSamples; i++)
            {
                var phase = (i * 220.0 / SampleRate) % 1.0;
                samples[i] = phase < 0.5 ? (short)(short.MaxValue * 0.45) : (short)(short.MinValue * 0.45);
            }

            return samples;
        }

        private static short[] CreateAlertPulseSamples(double durationSeconds)
        {
            var totalSamples = (int)(SampleRate * durationSeconds);
            var samples = new short[totalSamples];
            var pulseLength = SampleRate / 3;

            for (var i = 0; i < totalSamples; i++)
            {
                var pulsePosition = i % pulseLength;
                var isOn = pulsePosition < pulseLength / 6 || (pulsePosition > pulseLength / 3 && pulsePosition < pulseLength / 2);
                samples[i] = isOn ? GenerateSineSample(i, 1200, 0.5) : (short)0;
            }

            return samples;
        }

        private static short GenerateSineSample(int sampleIndex, double frequency, double amplitude)
        {
            var t = sampleIndex / (double)SampleRate;
            return (short)(Math.Sin(2 * Math.PI * frequency * t) * short.MaxValue * amplitude);
        }

        private static byte[] BuildWavFile(short[] samples)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                var dataLength = samples.Length * 2;
                writer.Write(new[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
                writer.Write(36 + dataLength);
                writer.Write(new[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E' });
                writer.Write(new[] { (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(SampleRate);
                writer.Write(SampleRate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
                writer.Write(dataLength);

                foreach (var sample in samples)
                {
                    writer.Write(sample);
                }

                return stream.ToArray();
            }
        }
    }
}
