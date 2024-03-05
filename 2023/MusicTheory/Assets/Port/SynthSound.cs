using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Music_Theory
{
    public class SynthSound
    {
        public readonly Waveform amplitude;
        public Waveform volume;
        public Waveform pitchBend;
        /// <summary>
        /// In seconds..
        /// </summary>
        public double duration;
        public double holdTime01;
        public double pitchBendScale;

        public SynthSound(Waveform amplitude)
        {
            this.amplitude = amplitude;
            volume = Waveform.Constant(1);
            pitchBend = Waveform.Constant(1);
            holdTime01 = 0.5;
            duration = 0;
            pitchBendScale = 1;
        }
    }
}
