using Music_Theory;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Note Sample", menuName = "Music Maker/Audio Sample/Note")]
public class NoteSampleConstructer : SampleConstructer
{
    [SerializeField] private NoteConstructer sampleNote;
    protected override float Frequency => (float)sampleNote.Get().frequency;
}