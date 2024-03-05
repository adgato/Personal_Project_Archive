using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Music_Theory;

[System.Serializable]
public class NoteConstructer : IConstructer<Note>
{
    public enum Letter { C = 0, D = 2, E = 4, F = 5, G = 7, A = 9, B = 11 }
    [SerializeField] private Letter chroma;
    [SerializeField] private Degree.Accidental accidental;
    [SerializeField] private int octave;
    public Note Get() => new Note((int)chroma + (int)accidental + octave * 12 + 24, accidental);
}
