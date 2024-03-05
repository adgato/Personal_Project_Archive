using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Music_Theory;

[CreateAssetMenu(fileName = "Synth Instrument", menuName = "Music Maker/Instrument/Synth")]
public class SynthInstrumentConstructer : InstrumentConstructer
{
    [SerializeField] private WaveformConstructer amplitude;
    [Min(0)]
    [SerializeField] private float masterVolume = 1;
    [Range(-1, 1)]
    [SerializeField] private float balance;
    [SerializeField] private int sampleRate = 44100;
    [Header("Advanced Settings")]
    [SerializeField] private WaveformConstructer volume;
    [SerializeField] private WaveformConstructer pitchBend;
    [Tooltip("In seconds.")]
    [Min(0)]
    [SerializeField] private float minDuration = 0;
    [Tooltip("The point on the volume curve to wait at until as the frequency is held on.")]
    [Range(0, 1)]
    [SerializeField] private float holdTime = 0.5f;
    [SerializeField] private float pitchBendScale = 1;

    public override Instrument Get()
    {
        SynthSound synthSound = new SynthSound(amplitude.Get())
        {
            volume = volume.Get(),
            pitchBend = pitchBend.Get(),
            holdTime01 = holdTime,
            duration = minDuration,
            pitchBendScale = pitchBendScale
        };
        return new Instrument(new SynthWave(synthSound, masterVolume, balance, sampleRate));
    }
}
