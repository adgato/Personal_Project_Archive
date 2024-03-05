using Music_Theory;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Frequency Sample", menuName = "Music Maker/Audio Sample/Frequency")]
public class FrequencySampleConstructer : SampleConstructer
{
    [SerializeField] private float sampleFrequency;
    protected override float Frequency => sampleFrequency;
}