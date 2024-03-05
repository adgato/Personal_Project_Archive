using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Music_Theory
{
    public struct Chord
    {
        public readonly Note[] notes;

        public Chord(params string[] noteNames)
        {
            notes = noteNames.Select(n => new Note(n)).OrderBy(n => n.number).ToArray();
        }
        public Chord(Note[] notes)
        {
            this.notes = notes.OrderBy(n => n.number).ToArray();
        }

        public override string ToString() => string.Join(" ", notes.Select(n => n.name)) + " ";
    }
}
