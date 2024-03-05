using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Music_Theory 
{

    class Program : MonoBehaviour
    {

        [SerializeField] private InstrumentConstructer instrument;


        void Start()
        {
            //Waveform t = Waveform.Triangle();
            //Waveform w = Waveform.Sawtooth();
            //
            //SynthSound sound = new SynthSound(new Waveform(x => Mathx.Round(t.At(x), 1) + w.At(x)))
            //{
            //    duration = 4,
            //    volume = new Waveform(x => 3.5f * Math.Pow(x, 0.1) * Math.Pow(1 - x, 2)),
            //    pitchBend = new Waveform(x => Math.Exp(-40.0 * x)),
            //    pitchBendScale = Note.SemitoneOffsetFrequency(2)
            //};

            using Instrument instrument = this.instrument.Get();

            //Scale test = new Scale(Progression.s2221131, 4, new Note("C4").frequency);

            //piano.Play(test.GetChord(Degree.I).frequencies);

            Console.WriteLine("Press Enter to start.");
            Console.WriteLine("Press Enter to start.");

            this.StartCoroutineSequence(
                CoroutineEx.WaitUntil(() => Input.GetKeyDown(KeyCode.Return)),
                instrument.Play(new Note("C4"), 0.5));

            //Scale c4Major = Scale.Ionian(new Note("C4"));
            //Scale c4Minor = Scale.Aeolian(new Note("C4"));
            //Scale c4Fridge = Scale.Phrygian(new Note("C4"));
            //Scale c4Door = Scale.Dorian(new Note("C4"));
            //Scale c4Loki = Scale.Locrian(new Note("C4"));
            //piano.Play(c4Fridge);
            //piano.Play(c4Door.GetChord(Degree.I));
            //piano.Wait();
            //piano.Play(c4Door.GetChord(Degree.IV));
            //piano.Wait();
            //piano.Play(c4Loki.GetChord(Degree.VI, 7));
            //piano.Wait();
            //piano.Play(c4Door.GetChord(Degree.I, 7, bass:(int)Natural._5));
            //piano.WaitStop();

            //piano.Play(c4Major.GetChord(Degree.I).frequencies, true);
            //piano.Wait();
            //piano.Play(c4Minor.GetChord(Degree.VII, bass: 1, octave: -1).frequencies, true);
            //piano.Wait();
            ////piano.Play(c4Major.GetChord(Degree.VI, bass: Natural._1 + c4Major.Length, octave: -1).frequencies, true);
            ////piano.Wait();
            //
            //piano.Play(c4Major.GetChord(Degree.IV, 7, add: Sharp._11 - 14).frequencies, true);
            //piano.Wait();
            //piano.Play(c4Fridge.GetChord(Degree.II, 7).frequencies, true);
            //piano.Wait();
            //piano.Play(c4Major.GetChord(Degree.I, 7).frequencies, true);
            //piano.WaitStop();
        }


    }
}
