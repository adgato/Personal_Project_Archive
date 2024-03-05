using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Music_Theory
{

    public class SampledWave : ISoundWave
    {
        private readonly double baseFrequency;
        private readonly double fadeOutTime;

        private readonly double sampleDuration;

        private int channels => waveFormat.Channels;
        private readonly WaveFormat waveFormat;

        private readonly float[] amplitude;

        public SampledWave(string filename, double fadeOutTime, double sampleFrequency)
        {
            baseFrequency = sampleFrequency;
            this.fadeOutTime = fadeOutTime;

            using AudioFileReader audioFile = new AudioFileReader(filename);

            sampleDuration = audioFile.TotalTime.TotalSeconds;

            waveFormat = audioFile.WaveFormat;

            int sampleCount = (int)(audioFile.Length / (audioFile.WaveFormat.BitsPerSample / 8));
            amplitude = new float[sampleCount * channels];
            audioFile.Read(amplitude, 0, sampleCount * channels);
        }

        public SampledWave(UnityEngine.AudioClip audioClip, double fadeOutTime, double sampleFrequency)
        {
            baseFrequency = sampleFrequency;
            this.fadeOutTime = fadeOutTime;

            sampleDuration = audioClip.length;

            waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(audioClip.frequency, audioClip.channels);

            amplitude = new float[audioClip.samples];
            audioClip.GetData(amplitude, 0);
        }

        public ISoundWaveProvider GetProvider(double[] frequencies)
        {
            double[] pitch = new double[frequencies.Length];
            for (int i = 0; i < frequencies.Length; i++)
                pitch[i] = frequencies[i] / baseFrequency;

            return new WaveProvider(this, pitch);
        }

        public class WaveProvider : ISoundWaveProvider
        {
            private readonly double[] pitch;
            private readonly int step;
            private readonly int fadeDuration = 1;
            private readonly SampledWave wave;
            public WaveFormat WaveFormat => wave.waveFormat;

            private int fadeTime = int.MaxValue;
            private int offTime = 0;
            private int time;

            public WaveProvider(SampledWave sampledWave, double[] pitch)
            {
                wave = sampledWave;
                this.pitch = pitch;
                offTime = (int)(sampledWave.sampleDuration / pitch.Max() * WaveFormat.SampleRate);
                fadeDuration = (int)(sampledWave.fadeOutTime * WaveFormat.SampleRate);
                step = 4 * sampledWave.channels;
            }

            public double Fade()
            {
                fadeTime = time;
                offTime = Math.Min(offTime, time + (int)(wave.fadeOutTime * WaveFormat.SampleRate) + 1);
                return wave.fadeOutTime + 1;
            }

            public int Read(byte[] buffer, int offset, int count)
            {
                for (int i = 0; i < count; i += step)
                {
                    if (time >= offTime)
                        return i;

                    double v = time > fadeTime ? Math.Cos(Math.Max(0, 0.5 * Math.PI * (time - fadeTime) / fadeDuration)) : 1;
                    float volume = (float)(v * v);

                    for (int channel = 0; channel < wave.channels; channel++)
                    {
                        float sample = 0;
                        for (int j = 0; j < pitch.Length; j++)
                        {
                            double position = time * pitch[j];
                            float t = (float)(position - Math.Floor(position));
                            int pos = wave.channels * (int)position + channel;

                            if (pos + wave.channels >= wave.amplitude.Length)
                                continue;

                            sample += wave.amplitude[pos] * (1 - t) + wave.amplitude[pos + wave.channels] * t;
                        }

                        byte[] sampleBytes = BitConverter.GetBytes(sample * volume);
                        for (int j = 0; j < 4; j++)
                            buffer[offset + i + 4 * channel + j] = sampleBytes[j];
                    }
                    time++;
                }
                return count;
            }
        }
    }

}
