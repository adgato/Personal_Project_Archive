using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Music_Theory
{
    public interface ISoundWave
    {
        ISoundWaveProvider GetProvider(double[] frequencies);
    }
    public interface ISoundWaveProvider : IWaveProvider
    {
        /// <returns>The delta time by which the sound will have stopped.</returns>
        double Fade();
    }
}
