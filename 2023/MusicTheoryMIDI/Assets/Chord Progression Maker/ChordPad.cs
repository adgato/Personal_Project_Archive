using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Music_Theory;

public class ChordPad : MonoBehaviour
{
    struct NoteData
    {
        public RectTransform pad;
        public RawImage key;
        public bool on;
    }
    private NoteData[][][] pads;

    private static readonly int[] childMap = new int[12] { 0, 1, 2, 3, 4, 5, 6, 0, 1, 3, 4, 5 };

    [SerializeField] private KeyboardController keyboardController;

    [SerializeField] private NoteConstructer tonic;

    List<Note> notesOn = new List<Note>();

    int extent;
    int bass;

    // Start is called before the first frame update
    void Start()
    {
        pads = new NoteData[7][][];
        for (int i = 0; i < 7; i++)
        {
            pads[i] = new NoteData[3][];
            for (int j = 0; j < 3; j++)
            {
                pads[i][j] = new NoteData[12];
                for (int k = 0; k < 12; k++)
                {
                    pads[i][j][k].pad = transform.GetChild(i).GetChild(j).GetChild(k).GetComponent<RectTransform>();
                    pads[i][j][k].key = pads[i][j][k].pad.GetComponent<RawImage>();
                    pads[i][j][k].on = false;
                }

            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        bool pressed = false;
        for (int mode = 0; mode < 7; mode++)
            for (int octave = 0; octave < 3; octave++)
                for (int key = 11; key >= 0; key--)
                {
                    int degreeNum = childMap[key];
                    Degree degree = new Degree(degreeNum, key >= 7 ? Degree.Accidental.sharp : Degree.Accidental.natural);
                    Note[] GetNotes() => new Scale(Scale.Diatonic, Scale.IonianMode + mode, tonic.Get()).GetChord(degree, extent, octave: octave, bass: bass).notes;

                    if (!pressed && MouseInRect(pads[mode][octave][key].pad))
                    {
                        pressed = true;
                        Color.RGBToHSV(pads[mode][octave][key].key.color, out float h, out float s, out float _);
                        Color col = Color.HSVToRGB(h, s, 1);
                        col.a = 1;
                        pads[mode][octave][key].key.color = col;

                        extent =
                            Input.GetKey(KeyCode.Alpha9) ? 9 :
                            Input.GetKey(KeyCode.Alpha7) ? 7 :
                            Input.GetKey(KeyCode.Alpha5) ? 5 :
                            Input.GetKey(KeyCode.Alpha3) ? 3 :
                            Input.GetKey(KeyCode.Alpha1) ? 1 : extent;

                        bass =
                            Input.GetKey(KeyCode.Alpha8) ? 4 :
                            Input.GetKey(KeyCode.Alpha6) ? 3 :
                            Input.GetKey(KeyCode.Alpha4) ? 2 :
                            Input.GetKey(KeyCode.Alpha2) ? 1 :
                            Input.GetKey(KeyCode.Alpha0) ? 0 : bass;

                        bool down =
                            Input.GetKey(KeyCode.Alpha9) ||
                            Input.GetKey(KeyCode.Alpha7) ||
                            Input.GetKey(KeyCode.Alpha5) ||
                            Input.GetKey(KeyCode.Alpha3) ||
                            Input.GetKey(KeyCode.Alpha1) ||
                            Input.GetKey(KeyCode.Alpha8) ||
                            Input.GetKey(KeyCode.Alpha6) ||
                            Input.GetKey(KeyCode.Alpha4) ||
                            Input.GetKey(KeyCode.Alpha2) ||
                            Input.GetKey(KeyCode.Alpha0);

                        bool up =
                            Input.GetKeyUp(KeyCode.Alpha9) ||
                            Input.GetKeyUp(KeyCode.Alpha7) ||
                            Input.GetKeyUp(KeyCode.Alpha5) ||
                            Input.GetKeyUp(KeyCode.Alpha3) ||
                            Input.GetKeyUp(KeyCode.Alpha1) ||
                            Input.GetKeyUp(KeyCode.Alpha8) ||
                            Input.GetKeyUp(KeyCode.Alpha6) ||
                            Input.GetKeyUp(KeyCode.Alpha4) ||
                            Input.GetKeyUp(KeyCode.Alpha2) ||
                            Input.GetKeyUp(KeyCode.Alpha0);

                        if (up)
                        {
                            pads[mode][octave][key].on = false;
                            keyboardController.TurnNoteOff(notesOn.ToArray());
                            notesOn.Clear();
                        }

                        if (!down || pads[mode][octave][key].on)
                            continue;

                        Note[] notes = GetNotes();

                        pads[mode][octave][key].on = true;
                        keyboardController.TurnNoteOff(notesOn.ToArray());
                        notesOn.Clear();

                        keyboardController.TurnNoteOn(notes);
                        notesOn.AddRange(notes);
                    }
                    else
                    {
                        if (pads[mode][octave][key].on)
                        {
                            Note[] notesOff = GetNotes();
                            HashSet<Note> notesOffHashSet = notesOff.ToHashSet();
                            pads[mode][octave][key].on = false;
                            keyboardController.TurnNoteOff(notesOff);
                            notesOn.RemoveAll(x => notesOffHashSet.Contains(x));
                        }

                        if (key >= 7)
                        {
                            Color.RGBToHSV(pads[mode][octave][key].key.color, out float h, out float s, out float _);
                            pads[mode][octave][key].key.color = Color.HSVToRGB(h, s, 0.2f);
                        }
                        else
                        {
                            Color col = pads[mode][octave][key].key.color;
                            col.a = 0.2f;
                            pads[mode][octave][key].key.color = col;
                        }
                    }
                }
                    
    }

    public static bool MouseInRect(RectTransform rectTransform)
    {
        return rectTransform.rect.Contains(rectTransform.InverseTransformPoint(Input.mousePosition));
    }
}
