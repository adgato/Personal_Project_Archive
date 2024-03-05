using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Music_Theory;

public class Keyboard : MonoBehaviour
{
    private static readonly Dictionary<KeyCode, int> noteLookup = new Dictionary<KeyCode, int>
        {
            { KeyCode.Z, 0 },
            { KeyCode.S, 1 },
            { KeyCode.X, 2 },
            { KeyCode.D, 3 },
            { KeyCode.C, 4 },
            { KeyCode.V, 5 },
            { KeyCode.G, 6 },
            { KeyCode.B, 7 },
            { KeyCode.H, 8 },
            { KeyCode.N, 9 },
            { KeyCode.J, 10 },
            { KeyCode.M, 11 }
        };

    [SerializeField] private KeyboardController keyboardController;

    private int octave = 3;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Comma))
        {
            octave--;
            //ClearReceipt();
        }
        else if (Input.GetKeyDown(KeyCode.Period))
        {
            octave++;
            //ClearReceipt();
        }
        octave = Mathf.Clamp(octave, 0, 9);

        foreach (int number in GetNotesDown())
            keyboardController.TurnNoteOn(new Note(number + octave * 12 + 24, Degree.Accidental.natural));
        foreach (int number in GetNotesUp())
            keyboardController.TurnNoteOff(new Note(number + octave * 12 + 24, Degree.Accidental.natural));
    }

    IEnumerable<int> GetNotesDown() => noteLookup.Keys.Where(key => Input.GetKeyDown(key)).Select(key => noteLookup[key]);
    IEnumerable<int> GetNotesUp() => noteLookup.Keys.Where(key => Input.GetKeyUp(key)).Select(key => noteLookup[key]);
}
