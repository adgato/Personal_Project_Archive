using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using UnityEngine;

namespace Music_Theory
{
    public enum Print { Write, WriteLine, Hide }
    public class Instrument : IDisposable
    {
        private struct Counter
        {
            private int v;
            public int Increment() => v++;
        }

        private int receiptCount;
        private readonly Dictionary<int, SoundWaveOut> wavesOut;
        private readonly ISoundWave soundWave;

        public Instrument(ISoundWave soundWave)
        {
            this.soundWave = soundWave;

            wavesOut = new Dictionary<int, SoundWaveOut>();
        }

        public void Dispose()
        {
            foreach (SoundWaveOut soundWaveOut in wavesOut.Values)
                soundWaveOut.Dispose();
        }

        public void Stop() => Dispose();

        public int NotesOn(double[] frequencies)
        {
            int receipt = receiptCount;
            receiptCount++;
            wavesOut.Add(receipt, SoundWaveOut.Play(soundWave.GetProvider(frequencies)));
            return receipt;
        }

        public int NotesOn(Note note, Print print = Print.Hide) => NotesOn(new Chord(new Note[1] { note }), print);

        public int NotesOn(Chord chord, Print print = Print.Hide)
        {
            int receipt = NotesOn(chord.notes.Select(n => n.frequency).ToArray());

            if (print == Print.Hide)
                return receipt;

            Console.Write(chord.ToString());
            if (print == Print.WriteLine)
                Console.WriteLine();
            return receipt;
        }

        public IEnumerator Play(Note note, double holdTime, Print print = Print.Write) => Play(new Chord(new Note[1] { note }), holdTime, print);
        public IEnumerator Play(Chord chord, double holdTime, Print print = Print.WriteLine)
        {
            int receipt = NotesOn(chord, print);
            yield return new WaitForSecondsRealtime((float)holdTime);
            yield return NotesOff(receipt);
        }

        public IEnumerator Play(Scale scale, double holdTime = 0.4, Print print = Print.Write)
        {
            float sleep = (float)Math.Max(0, holdTime);
            for (int i = 0; i <= scale.Length; i++)
            {
                Note note = scale.GetNote(i);
                int receipt = NotesOn(note, print);
                yield return new WaitForSecondsRealtime(sleep);
                yield return NotesOff(receipt);
            }
            if (print != Print.Hide)
                Console.WriteLine();
            yield return new WaitForSecondsRealtime(sleep);
            for (int i = scale.Length; i >= 0; i--)
            {
                Note note = scale.GetNote(i);
                int receipt = NotesOn(note, print);
                yield return new WaitForSecondsRealtime(sleep);
                yield return NotesOff(receipt);
            }
            if (print != Print.Hide)
                Console.WriteLine();
        }

        public IEnumerator NotesOff(int receipt)
        {
            SoundWaveOut soundWaveOut = wavesOut[receipt];
            wavesOut.Remove(receipt);
            yield return new WaitForSecondsRealtime((float)soundWaveOut.Fadeout());
            soundWaveOut.Dispose();
        }
    }
}
