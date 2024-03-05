using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Music_Theory;


[CreateAssetMenu(fileName = "Sampled Instrument", menuName = "Music Maker/Instrument/Sampled")]
public class SampledInstrumentConstructer : InstrumentConstructer
{
    [SerializeField] private SampleConstructer sample;

    public override Instrument Get() => new Instrument(sample.Get());
}