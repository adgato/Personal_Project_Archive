using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Music_Theory
{
    /// <summary>The domain is [0,1] and the range within [-1, 1].</summary>
    public class Waveform
    {
        private readonly Func<double, double> WaveFunction;
        private readonly bool guarenteedRange = true;

        /// <param name="WaveFunction">The domain should be [0,1], and the range will be scaled to be lie within [-1, 1] (if it doesn't already).</param>
        public Waveform(Func<double, double> WaveFunction)
        {
            this.WaveFunction = WaveFunction;
            guarenteedRange = false;
        }

        private Waveform(Func<double, double> WaveFunction, bool guarenteedRange)
        {
            this.WaveFunction = WaveFunction;
            this.guarenteedRange = guarenteedRange;
        }

        public IEnumerable<double> Discretise(int resolution)
        {
            Func<double, double> WaveFunction = this.WaveFunction;
            IEnumerable<double> discreteWave = Enumerable.Range(0, resolution).Select(i => WaveFunction((double)i / resolution));
            if (guarenteedRange)
                return discreteWave;
            double max = Math.Max(1, discreteWave.Select(i => Math.Abs(i)).Max());
            return discreteWave.Select(i => i / max);
        }

        public double At(double x) => WaveFunction(x);

        public static Waveform Sine() => new Waveform(x => Math.Sin(2.0 * Math.PI * x), true);

        public static Waveform Triangle() => new Waveform(x => Math.Abs(4 * Mathx.Mod(x - 0.25, 1.0) - 2.0) - 1.0, true);

        public static Waveform Square() => new Waveform(x => 1.0 - 2.0 * Math.Round(Mathx.Mod(x, 1.0)), true);

        public static Waveform Sawtooth() => new Waveform(x => 2 * Mathx.Mod(x, 1.0) - 1, true);

        public static Waveform Noise() => new Waveform(x => Mathx.Random.NextDouble(), true);

        public static Waveform Constant(double value) => new Waveform(x => value, Math.Abs(value) <= 1);

        //public static Waveform Average(Waveform[] waveforms) => new Waveform(i => waveforms.Sum(wave => wave.WaveFunction(i)) / waveforms.Length, true);

        public static Waveform operator *(Waveform a, Waveform b) => new Waveform(x => a.WaveFunction(x) * b.WaveFunction(x), a.guarenteedRange && b.guarenteedRange);
        public static Waveform operator +(Waveform a, Waveform b) => new Waveform(x => a.WaveFunction(x) + b.WaveFunction(x), false);
        public static Waveform operator -(Waveform a, Waveform b) => new Waveform(x => a.WaveFunction(x) - b.WaveFunction(x), false);
    }
}
