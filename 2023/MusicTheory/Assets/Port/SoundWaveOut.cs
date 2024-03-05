using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using System.Diagnostics;

namespace Music_Theory
{
    public class SoundWaveOut : IDisposable
    {
        private WaveOut oneShotWave;
        private ISoundWaveProvider waveProvider;

        public static SoundWaveOut Play(ISoundWaveProvider waveProvider)
        {
            SoundWaveOut soundWaveOut = new SoundWaveOut();
            soundWaveOut.oneShotWave = new WaveOut();
            soundWaveOut.oneShotWave.Init(waveProvider);
            soundWaveOut.oneShotWave.Play();
            soundWaveOut.waveProvider = waveProvider;
            return soundWaveOut;
        }

        public void Dispose()
        {
            oneShotWave.Stop();
            Task.Delay(10); //??
            oneShotWave.Dispose();
        }

        public double Fadeout() => waveProvider.Fade();
    }
}

