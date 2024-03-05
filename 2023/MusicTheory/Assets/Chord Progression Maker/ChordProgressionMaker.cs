using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Music_Theory;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ChordProgressionMaker : MonoBehaviour
{
    public struct SaveData
    {
        public ProgressionConstructer scale;
        public NoteConstructer tonic;
        public ChordBuilder[] chordProgression;
    }
    public SaveData GetSaveData()
    {
        return new SaveData()
        {
            scale = scale,
            tonic = tonic,
            chordProgression = chordProgression
        };
    }
    public void LoadSaveData(SaveData saveData)
    {
        scale = saveData.scale;
        tonic = saveData.tonic;
        chordProgression = saveData.chordProgression;
    }

    [SerializeField] KeyboardController keyboardController;
    [SerializeField] float noteOnTime;
    [SerializeField] float noteOffTime;

    public string progressionSaveName;

    public ProgressionConstructer scale;
     
    public NoteConstructer tonic;
    public ChordBuilder[] chordProgression;

    [System.Serializable]
    public class ChordBuilder
    {
        [ReadOnly] [SerializeField] private string chord;
        [HideInInspector] private Chord value;

        [SerializeField] private int mode;
        [SerializeField] private DegreeConstructer.DegreeSelection root;
        [Min(0)]
        [SerializeField] private int bass;
        [SerializeField] private Vector3Int eso;
        [SerializeField] private DegreeConstructer[] add;
        [SerializeField] private Interval playInterval;


        private DegreeConstructer.DegreeSelection _root;

        public void Validate(ChordProgressionMaker maker)
        {
            if (root != _root)
            {
                _root = root;
                bass = 0;
            }
            if (eso.x <= 0)
                eso.x = 5;
            if (eso.y <= 0)
                eso.y = 2;
            bass = Mathf.Min(bass, (eso.x + 1) / eso.y + add.Length - 1);
            playInterval.Validate();
            value = maker.GetMode(mode).GetChord((int)root, eso.x, eso.y, eso.z, bass, add.Select(x => x.Get()).ToArray());
            chord = value.ToString();
        }

        public Chord GetChord() => value;
        public IEnumerable<int> GetSteps() => playInterval.GetSteps();
    }
    [System.Serializable]
    public class DegreeConstructer : IConstructer<Degree>
    {
        public enum DegreeSelection { I, II, III, IV, V, VI, VII, VIII, IX, X, XI, XII }
        public DegreeSelection degree;
        public Degree.Accidental accidental;

        public static bool operator ==(DegreeConstructer a, DegreeConstructer b) => a.degree == b.degree && a.accidental == b.accidental;
        public static bool operator !=(DegreeConstructer a, DegreeConstructer b) => !(a == b);
        public override bool Equals(object obj) => this == (DegreeConstructer)obj;
        public override int GetHashCode() => (int)degree + 12 * ((int)accidental + 1);

        public Degree Get() => new Degree((int)degree, accidental);
    }
    [System.Serializable]
    public struct Interval
    {
        [SerializeField] private ProgressionConstructer playInterval;
        [SerializeField] private int playMode;

        public void Validate()
        {
            playInterval.Validate();
        }

        public IEnumerable<int> GetSteps()
        {
            string playStr = playInterval.Get().ToString()[1..];
            int len = playStr.Length;
            playStr = playStr[(playMode % len)..] + playStr[..(playMode % len)];
            return playStr.Select(i => i - (i <= '9' ? 48 : 55));
        }
    }


    private Scale GetMode(int mode) => new Scale(scale.Get(), mode, tonic.Get());

    private void OnValidate()
    {
        scale.Validate();
        foreach (ChordBuilder chordBuilder in chordProgression)
            chordBuilder.Validate(this);
    }

    public void SetScale(Progression scale)
    {
        this.scale.Set(scale);
        OnValidate();
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(PlayChords());
    }

    IEnumerator PlayChords(int i = 0)
    {
        if (chordProgression.Length > 0)
            foreach (int step in chordProgression[i % chordProgression.Length].GetSteps())
            {
                int receipt = keyboardController.TurnNoteOn(chordProgression[i % chordProgression.Length].GetChord().notes.Select(x => x.number).ToArray());
                yield return new WaitForSeconds(noteOnTime * step / 12);
                keyboardController.TurnNoteOff(receipt);
                yield return new WaitForSeconds(noteOffTime * step / 12);
            }
        else
            yield return new WaitForSeconds(noteOnTime + noteOffTime);
        yield return PlayChords(i + 1);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ChordProgressionMaker))]
public class ChordProgressionMakerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ChordProgressionMaker maker = (ChordProgressionMaker)target;
        if (GUILayout.Button("Save"))
            JsonSaver.SaveData(maker.progressionSaveName, maker.GetSaveData());
        if (GUILayout.Button("Load"))
            maker.LoadSaveData(JsonSaver.LoadData<ChordProgressionMaker.SaveData>(maker.progressionSaveName));
        if (GUILayout.Button(nameof(Scale.AlternatingOctatonic)))
            maker.SetScale(Scale.AlternatingOctatonic);
        if (GUILayout.Button(nameof(Scale.MinorHarmonic)))
            maker.SetScale(Scale.MinorHarmonic);
        if (GUILayout.Button(nameof(Scale.Gypsy1)))
            maker.SetScale(Scale.Gypsy1);
        if (GUILayout.Button(nameof(Scale.Gypsy2)))
            maker.SetScale(Scale.Gypsy2);
        if (GUILayout.Button(nameof(Scale.Neapolitan)))
            maker.SetScale(Scale.Neapolitan);
        if (GUILayout.Button(nameof(Scale.JazzMinor)))
            maker.SetScale(Scale.JazzMinor);
        if (GUILayout.Button(nameof(Scale.Diatonic)))
            maker.SetScale(Scale.Diatonic);
        if (GUILayout.Button(nameof(Scale.Pentatonic)))
            maker.SetScale(Scale.Pentatonic);
        if (GUILayout.Button(nameof(Scale.Japanese)))
            maker.SetScale(Scale.Japanese);
        base.OnInspectorGUI();
    }
}
#endif