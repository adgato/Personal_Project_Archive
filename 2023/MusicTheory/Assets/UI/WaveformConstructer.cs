using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Music_Theory;

[System.Serializable]
public class WaveformConstructer : IConstructer<Waveform>
{
    [Tooltip("The domain is [0,1] and the range within [-1, 1].")]
    [SerializeField] private AnimationCurve WaveFunction = AnimationCurve.Linear(0, -1, 1, 1);

    public Waveform Get() => new Waveform(x => WaveFunction.Evaluate((float)x));
}

