using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Music_Theory
{
    public class SynthWave : ISoundWave
    {
        private readonly double[] amplitude;
        private readonly double[] volume;

        private readonly int sampleRate = 44100;
        private readonly int modulationRate;
        private readonly int holdTime;
        private readonly double[] balance;
        private const int channels = 2;


        private readonly WaveFormat waveFormat;

        private readonly IEnumerable<double> pitch;

        public SynthWave(SynthSound synthSound, double masterVolume, double balance = 0, int sampleRate = 44100)
        {
            this.sampleRate = sampleRate;
            this.balance = new double[2] { Math.Min(1, 1 - balance), Math.Min(1, 1 + balance) };
            double duration = synthSound.duration;
            modulationRate = Math.Max(1, (int)(duration * sampleRate));
            holdTime = (int)(Math.Clamp(synthSound.holdTime01, 0, 1) * duration * sampleRate);
            waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);

            amplitude = synthSound.amplitude.Discretise(sampleRate).ToArray();
            volume = synthSound.volume.Discretise(modulationRate).Select(i => i * masterVolume).ToArray();
            pitch = synthSound.pitchBend.Discretise(modulationRate).Select(i => (1 + i) * synthSound.pitchBendScale);
        }

        public ISoundWaveProvider GetProvider(double[] frequencies)
        {
            double[][] baseFrequencies = new double[frequencies.Length][];
            for (int i = 0; i < frequencies.Length; i++)
                baseFrequencies[i] = pitch.Select(j => frequencies[i] * j).ToArray();
            return new WaveProvider(this, baseFrequencies);
        }

        public class WaveProvider : ISoundWaveProvider
        {
            private readonly double[][] frequencies;
            private double[] positions;
            private int time;
            private bool fadeOut = false;

            private SynthWave synthWave;

            public WaveFormat WaveFormat => synthWave.waveFormat;

            public WaveProvider(SynthWave synthWave, double[][] frequencies)
            {
                this.synthWave = synthWave;
                this.frequencies = frequencies;

                fadeOut = false;

                positions = new double[frequencies.Length];
                time = 0;
            }

            public double Fade()
            {
                fadeOut = true;
                time = synthWave.holdTime;
                return (double)(synthWave.modulationRate - synthWave.holdTime) / synthWave.sampleRate;
            }

            public int Read(byte[] buffer, int offset, int count)
            {
                const int step = 4 * channels;
                for (int i = 0; i < count; i += step)
                {
                    if (time >= synthWave.modulationRate)
                        return i;

                    double sample = 0;
                    for (int j = 0; j < frequencies.Length; j++)
                    {
                        sample += synthWave.amplitude[(int)positions[j]] * synthWave.volume[time];
                        positions[j] = (positions[j] + frequencies[j][time]) % synthWave.sampleRate;
                    }
                    if (time < synthWave.holdTime || fadeOut)
                        time++;

                    for (int channel = 0; channel < channels; channel++)
                    {
                        byte[] sampleBytes = BitConverter.GetBytes((float)(sample * synthWave.balance[channel]));
                        for (int j = 0; j < 4; j++)
                            buffer[offset + i + 4 * channel + j] = sampleBytes[j];
                    }
                }
                return count;
            }
        }
    }
}
