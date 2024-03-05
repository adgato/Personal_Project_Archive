using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Music_Theory
{
    public struct Note
    {
        private static readonly Dictionary<string, int> noteLookup = new Dictionary<string, int>
        {
            {"C", 60}, 
            {"C#", 61}, {"Db", 61}, 
            {"D", 62}, 
            {"D#", 63}, {"Eb", 63},
            {"E", 64}, 
            {"F", 65}, 
            {"F#", 66}, {"Gb", 66}, 
            {"G", 67}, 
            {"G#", 68}, {"Ab", 68}, 
            {"A", 69}, 
            {"A#", 70}, {"Bb", 70}, 
            {"B", 71}
        };

        public readonly string name;
        public readonly int number;
        public readonly double frequency;
        public readonly Degree.Accidental accidental;

        public Note(string name)
        {
            this.name = name;
            accidental = name.Contains("#") ? Degree.Accidental.sharp : name.Contains("b") ? Degree.Accidental.flat : Degree.Accidental.natural;
            number = NameToNumber(name);
            frequency = NumberToFrequency(number);
        }
        public Note(int number, Degree.Accidental accidental)
        {
            this.number = number;
            this.accidental = accidental;
            name = NumberToName(number, accidental);
            frequency = NumberToFrequency(number);
        }

        private static int NameToNumber(string note)
        {
            string noteName = "";
            int octave = 0;

            for (int i = 0; i < note.Length; i++)
            {
                if (note[i] == '-' || char.IsNumber(note[i]))
                {
                    octave = int.Parse(note.Substring(i));
                    break;
                }
                else
                    noteName += note[i];
            }

            return noteLookup[noteName] + (octave - 4) * 12;
        }
        private static string NumberToName(int number, Degree.Accidental accidental)
        {
            int semitoneOffset = number % 12;
            int octave = number / 12 - 1;
            string noteName = accidental == Degree.Accidental.flat ? 
                noteLookup.Last(x => x.Value == 60 + semitoneOffset).Key : 
                noteLookup.First(x => x.Value == 60 + semitoneOffset).Key;

            return $"{noteName}{octave}";
        }
        private static double NumberToFrequency(int number) => SemitoneOffsetFrequency(number - 69) * 440.0;
        public static double SemitoneOffsetFrequency(int number) => Math.Pow(2, number / 12.0);
    }
}
