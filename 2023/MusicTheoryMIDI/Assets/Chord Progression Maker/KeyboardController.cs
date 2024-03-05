using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Music_Theory;

public class KeyboardController : MonoBehaviour
{
    private static readonly Color[] defaultColours = new Color[12]
    {
        Color.white, Color.black, Color.white, Color.black, Color.white, Color.white, Color.black, Color.white, Color.black, Color.white, Color.black, Color.white
    };
    private static readonly int[] childMap = new int[12] { 0, 7, 1, 8, 2, 3, 9, 4, 10, 5, 11, 6 };
    private RawImage[][] noteColours;
    [SerializeField] private GameObject risingNotePrefab;
    [SerializeField] private Color hit;
    [SerializeField] private int velocity = 32;

    private Dictionary<int, RisingNote> risingNotes = new Dictionary<int, RisingNote>();

    private VirtualMIDI virtualMIDI;

    private void Awake()
    {
        virtualMIDI = new VirtualMIDI();
        noteColours = new RawImage[9][];
        for (int i = 0; i < 9; i++)
        {
            noteColours[i] = new RawImage[12];
            for (int j = 0; j < 12; j++)
                noteColours[i][j] = transform.GetChild(i).GetChild(childMap[j]).GetComponent<RawImage>();
        }
    }
    public void TurnNoteOn(params Note[] notes)
    {
        virtualMIDI.NotesOn(new Chord(notes), velocity);

        for (int i = 0; i < notes.Length; i++)
        {
            int number = notes[i].number % 12;
            int octave = notes[i].number / 12 - 2;

            RisingNote risingNote = Instantiate(risingNotePrefab, noteColours[octave][number].transform).GetComponent<RisingNote>();
            risingNote.Init(noteColours[octave][number].rectTransform.sizeDelta.y);

            if (risingNotes.ContainsKey(notes[i].number))
                risingNotes[notes[i].number].Rise();
            risingNotes[notes[i].number] = risingNote;

            noteColours[octave][number].color = hit;
        }
    }
    public void TurnNoteOff(params Note[] notes)
    {
        virtualMIDI.NotesOff(new Chord(notes), velocity);

        for (int i = 0; i < notes.Length; i++)
        {
            int number = notes[i].number % 12;
            int octave = notes[i].number / 12 - 2;
            noteColours[octave][number].color = defaultColours[number];

            if (risingNotes.ContainsKey(notes[i].number))
                risingNotes[notes[i].number].Rise();
        }
    }

    public void OnDestroy()
    {
        virtualMIDI.Dispose();
    }
}
