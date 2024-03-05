using Music_Theory;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SampleConstructer : ScriptableObject, IConstructer<SampledWave>
{
    [SerializeField] private AudioClip sample;

    [Min(0)]
    [SerializeField] private float fadeOutTime = 0.5f;

    protected abstract float Frequency { get; }

    public SampledWave Get() => new SampledWave(sample, fadeOutTime, Frequency);
}