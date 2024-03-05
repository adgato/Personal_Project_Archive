using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Music_Theory
{
    public class Scale
    {
        public readonly int Length;
        private readonly Note[] scale;

        public Scale(Progression progression, int mode, Note tonic)
        {
            char[] offsets = progression.ToString().ToCharArray();
            Length = offsets.Length - 1;
            scale = new Note[Length];

            scale[0] = tonic;
            for (int i = 1; i < offsets.Length - 1; i++)
            {
                int index = Mathx.Mod(i - 1 + mode, offsets.Length - 1) + 1;
                char step = offsets[index];
                scale[i] = new Note(scale[i - 1].number + step - (step <= '9' ? 48 : 55), Degree.Accidental.sharp);
            }
            for (int i = 0; i < offsets.Length - 1; i++)
            {
                char curr = scale[i].name[0];
                if (i > 0 && scale[i - 1].name[0] == curr || i < offsets.Length - 2 && scale[i + 1].name[0] == curr)
                    scale[i] = new Note(scale[i].number, Degree.Accidental.flat);
            }
        }

        public Chord GetChord(Degree root, int extent = 5, int step = 2, int octave = 0, int bass = 0, params Degree[] add)
        {
            int count = (extent + 1) / step;
            bass %= count + add.Length;
            int rootNumber = GetNote(root).number;

            Note[] notes = new Note[count + add.Length];
            for (int i = 0; i < count + add.Length; i++)
            {
                Note note = GetNote(root + (i < count ? step * i : add[i - count]));

                int octaveShift = 0;
                if (i == bass)
                    octaveShift = (int)Math.Max(0, Math.Ceiling((note.number - rootNumber) / 12.0)) * -12;

                int num = note.number + octaveShift;
                notes[i] = new Note(num + 12 * octave, note.accidental);
            }

            return new Chord(notes);
        }
        public Chord MakeChord(params Degree[] add) => new Chord(add.Select(d => GetNote(d)).ToArray());

        public Note GetNote(Degree degree)
        {
            Note chroma = scale[Mathx.Mod(degree.degree, Length)];
            int octaveOffset = degree.degree / Length - (degree.degree < 0 ? 1 : 0);
            return new Note(chroma.number + 12 * octaveOffset + (int)degree.accidental, (new Degree(0, chroma.accidental) + degree).accidental);
        }

        // Scales below are those in 12 equal temperament from https://en.wikipedia.org/wiki/List_of_musical_scales_and_modes, plus a couple extra Japanese modes.

        /// <summary> Ionian </summary>
        public static Scale Major(Note tonic) => Ionian(tonic);
        /// <summary> Aeolian </summary>
        public static Scale NaturalMinor(Note tonic) => Aeolian(tonic);
        /// <summary> Lydian </summary>
        public static Scale Acoustic(Note tonic) => Lydian(tonic);
        /// <summary> Mixolydian </summary>
        public static Scale AdonaiMalakh(Note tonic) => Mixolydian(tonic);
        /// <summary> Locrian </summary>
        public static Scale Altered(Note tonic) => Locrian(tonic);
        /// <summary> Double Harmonic </summary>
        public static Scale Flamenco(Note tonic) => DoubleHarmonic(tonic);

        public static Scale Ionian(Note tonic) => new Scale(Diatonic, IonianMode, tonic);
        public static Scale Dorian(Note tonic) => new Scale(Diatonic, DorianMode, tonic);
        public static Scale Phrygian(Note tonic) => new Scale(Diatonic, PhrygianMode, tonic);
        public static Scale Lydian(Note tonic) => new Scale(Diatonic, LydianMode, tonic);
        public static Scale Mixolydian(Note tonic) => new Scale(Diatonic, MixolydianMode, tonic);
        public static Scale Aeolian(Note tonic) => new Scale(Diatonic, AeolianMode, tonic);
        public static Scale Locrian(Note tonic) => new Scale(Diatonic, LocrianMode, tonic);


        public static Scale Hirajoshi(Note tonic) => new Scale(Japanese, 0, tonic);
        /// <summary> Possibly incorrect name </summary>
        public static Scale Kumoi(Note tonic) => new Scale(Japanese, 1, tonic);
        public static Scale Iwato(Note tonic) => new Scale(Japanese, 2, tonic);
        /// <summary> Incorrect name, but that's what I'm calling it, can't find anything else </summary>
        public static Scale Akebono(Note tonic) => new Scale(Japanese, 3, tonic);
        public static Scale In(Note tonic) => new Scale(Japanese, 4, tonic);


        public static Scale MinorPentatonic(Note tonic) => new Scale(Pentatonic, 0, tonic);
        public static Scale Yo(Note tonic) => new Scale(Pentatonic, 2, tonic);
        public static Scale MajorPentatonic(Note tonic) => new Scale(Pentatonic, 3, tonic);

        public static Scale MelodicMinor(Note tonic) => new Scale(JazzMinor, DorianMode, tonic);
        public static Scale LydianAugmented(Note tonic) => new Scale(JazzMinor, LydianMode, tonic);
        public static Scale HalfDiminished(Note tonic) => new Scale(JazzMinor, LocrianMode, tonic);

        public static Scale LydianDiminished(Note tonic) => new Scale(Gypsy1, LydianMode, tonic);
        public static Scale Gypsy(Note tonic) => new Scale(Gypsy1, AeolianMode, tonic);

        public static Scale DoubleHarmonic(Note tonic) => new Scale(Gypsy2, IonianMode, tonic);
        public static Scale HungarianMinor(Note tonic) => new Scale(Gypsy2, LydianMode, tonic);

        public static Scale NeapolitanMajor(Note tonic) => new Scale(Neapolitan, PhrygianMode, tonic);
        public static Scale MajorLocrian(Note tonic) => new Scale(Neapolitan, LocrianMode, tonic);

        public static Scale PhrygianDominant(Note tonic) => new Scale(MinorHarmonic, 2, tonic);
        public static Scale HarmonicMinor(Note tonic) => new Scale(MinorHarmonic, 5, tonic);

        public static Scale AlternatingOctatonic1(Note tonic) => new Scale(AlternatingOctatonic, 0, tonic);
        public static Scale AlternatingOctatonic2(Note tonic) => new Scale(AlternatingOctatonic, 1, tonic);

        public static Scale Augmented(Note tonic) => new Scale(Progression.s313131, 0, tonic);
        public static Scale Bebop(Note tonic) => new Scale(Progression.s22122111, 0, tonic);
        public static Scale MajorBebop(Note tonic) => new Scale(Progression.s22121121, 0, tonic);
        public static Scale Blues(Note tonic) => new Scale(Progression.s323211, 2, tonic);
        public static Scale Chromatic(Note tonic) => new Scale(Progression.s111111111111, 0, tonic);
        public static Scale Enigmatic(Note tonic) => new Scale(Progression.s3222111, 6, tonic);
        public static Scale HarmonicMajor(Note tonic) => new Scale(Progression.s2212131, 0, tonic);
        public static Scale HungarianMajor(Note tonic) => new Scale(Progression.s2312121, 1, tonic);
        public static Scale Insen(Note tonic) => new Scale(Progression.s42321, 4, tonic);
        public static Scale Istrian(Note tonic) => new Scale(Progression.s212151, 5, tonic);
        public static Scale Persian(Note tonic) => new Scale(Progression.s2311311, 3, tonic);
        public static Scale Prometheus(Note tonic) => new Scale(Progression.s222231, 1, tonic);
        public static Scale OfHarmonics(Note tonic) => new Scale(Progression.s223311, 3, tonic);
        public static Scale Tritone(Note tonic) => new Scale(Progression.s321321, 2, tonic);
        public static Scale TwoSemitoneTritone(Note tonic) => new Scale(Progression.s411411, 1, tonic);
        public static Scale UkrainianDorian(Note tonic) => new Scale(Progression.s2213121, 1, tonic);
        public static Scale WholeTone(Note tonic) => new Scale(Progression.s222222, 0, tonic);



        public const Progression AlternatingOctatonic = Progression.s21212121;
        public const Progression MinorHarmonic = Progression.s2213121;
        public const Progression Gypsy1 = Progression.s2221311;
        public const Progression Gypsy2 = Progression.s2131131;
        public const Progression Neapolitan = Progression.s2222121;
        public const Progression JazzMinor = Progression.s2222121;
        public const Progression Diatonic = Progression.s2221221;
        public const Progression Pentatonic = Progression.s32322;
        public const Progression Japanese = Progression.s42141;
        public const int IonianMode = 4;
        public const int DorianMode = 5;
        public const int PhrygianMode = 6;
        public const int LydianMode = 7;
        public const int MixolydianMode = 8;
        public const int AeolianMode = 9;
        public const int LocrianMode = 10;
    }
}
