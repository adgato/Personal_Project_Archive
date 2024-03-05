using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Music_Theory;

public class KeyboardController : MonoBehaviour
{
    [SerializeField] private InstrumentConstructer instrument;
    private Instrument piano;

    private static readonly Color[] defaultColours = new Color[12]
    {
        Color.white, Color.black, Color.white, Color.black, Color.white, Color.white, Color.black, Color.white, Color.black, Color.white, Color.black, Color.white
    };
    private static readonly int[] childMap = new int[12] { 0, 7, 1, 8, 2, 3, 9, 4, 10, 5, 11, 6 };
    private RawImage[][] noteColours;
    [SerializeField] private GameObject risingNotePrefab;
    [SerializeField] private Color hit;

    private Dictionary<int, (int noteNumber, RisingNote risingNote)[]> receipts = new Dictionary<int, (int, RisingNote)[]>();

    private void Awake()
    {
        piano = instrument.Get();
        noteColours = new RawImage[9][];
        for (int i = 0; i < 9; i++)
        {
            noteColours[i] = new RawImage[12];
            for (int j = 0; j < 12; j++)
                noteColours[i][j] = transform.GetChild(i).GetChild(childMap[j]).GetComponent<RawImage>();
        }
    }
    public int TurnNoteOn(int[] noteNumbers)
    {
        int receipt = piano.NotesOn(new Chord(noteNumbers.Select(x => new Note(x, Degree.Accidental.natural)).ToArray()));

        (int, RisingNote)[] risingNotes = new (int, RisingNote)[noteNumbers.Length];

        for (int i = 0; i < noteNumbers.Length; i++)
        {
            int number = noteNumbers[i] % 12;
            int octave = noteNumbers[i] / 12 - 2;

            RisingNote risingNote = Instantiate(risingNotePrefab, noteColours[octave][number].transform).GetComponent<RisingNote>();

            risingNote.Init(noteColours[octave][number].rectTransform.sizeDelta.y);

            noteColours[octave][number].color = hit;

            risingNotes[i] = (noteNumbers[i], risingNote);
        }

        receipts.Add(receipt, risingNotes);
        return receipt;
    }
    public int TurnNoteOn(int noteNumber) => TurnNoteOn(new int[1] { noteNumber });
    public void TurnNoteOff(int receipt)
    {
        if (!receipts.ContainsKey(receipt))
            return;

        StartCoroutine(piano.NotesOff(receipt));
        foreach ((int noteNumber, RisingNote risingNote) in receipts[receipt])
        {
            int number = noteNumber % 12;
            int octave = noteNumber / 12 - 2;
            risingNote.Rise();
            noteColours[octave][number].color = defaultColours[number];
        }
        receipts.Remove(receipt);
    }
    private void OnDestroy()
    {
        piano.Dispose();
    }
}
