using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TobiasErichsen.teVirtualMIDI;

namespace Music_Theory
{
    public class VirtualMIDI : System.IDisposable
    {
        static int count = 0;
        readonly TeVirtualMIDI port;
        const byte NOTE_ON = 0x90;
        const byte NOTE_OFF = 0x80;

        public VirtualMIDI()
        {
            port = new TeVirtualMIDI($"MIDI loopback {count++}");
        }

        public void NoteOn(Note note, int velocity = 64)
        {
            if (note.number >= 0 && note.number < 128)
                port.sendCommand(new byte[] { NOTE_ON, (byte)note.number, (byte)velocity });
        }
        public void NoteOff(Note note, int velocity = 64)
        {
            if (note.number >= 0 && note.number < 128)
                port.sendCommand(new byte[] { NOTE_OFF, (byte)note.number, (byte)velocity });
        }

        public void NotesOn(Chord chord, int velocity = 64)
        {
            foreach (Note note in chord.notes)
                NoteOn(note, velocity);
        }
        public void NotesOff(Chord chord, int velocity = 64)
        {
            foreach (Note note in chord.notes)
                NoteOff(note, velocity);
        }
        public void Dispose() => port.shutdown();
    }
}

