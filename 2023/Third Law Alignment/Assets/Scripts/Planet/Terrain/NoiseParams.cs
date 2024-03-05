using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct NoiseParams
{
    public uint octaves;

    [Tooltip("x => min radius (noise = 0), y => min radius where noise unscaled, z => max radius where noise unscaled, w => max radius (noise = 0)")]
    public Vector4 cutoff01;

    public float persistance01; // scale the amplitude of each octave by this factor each octave
    public float frequency; // how fast the sample moves through noise space
    public float lacunarity; // scale the frequency by this factor each octave

    public Vector3 offset0; // applied to every octave
    [HideInInspector] public Vector3 offset1; // applied to every 3n + 0 octaves
    [HideInInspector] public Vector3 offset2; // applied to every 3n + 1 octaves
    [HideInInspector] public Vector3 offset3; // applied to every 3n + 2 octaves

    // remap the noise from y = x onto a line graph following these points:
    [HideInInspector] public float remap0; // (0, remap0)
    [HideInInspector] public Vector2 remap2; // (remap2.x, remap2.y) [0 < remap2.x < remap3.x]
    [HideInInspector] public Vector2 remap3; // and so on...
    [HideInInspector] public Vector2 remap4;
    [HideInInspector] public Vector2 remap5;
    [HideInInspector] public float remap1; // (1, remap1)
    // could ignore this step by doing: remap0 = 0, remap[2,3,4,5] = (0,0), remap1 = 1
}

[System.Serializable]
public class NoiseParamData
{
    public string name;
    public NoiseParams noiseParams;

    public Vector2 remapRandomRange = Vector2.up;
    public AnimationCurve RemapOffset;
    public AnimationCurve CurrentRemap;

    /// <summary>
    /// Create the noise parameters from the seed
    /// </summary>
    public void Initialise(Rand.Seed seed)
    {
        Rand rand = new Rand(seed);
        noiseParams.offset1 = Vector3x.Lerp(-9999, 9999, rand.insideUnitCube);
        noiseParams.offset2 = Vector3x.Lerp(-9999, 9999, rand.insideUnitCube);
        noiseParams.offset3 = Vector3x.Lerp(-9999, 9999, rand.insideUnitCube);

        float[] x = new float[4] { rand.value, rand.value, rand.value, rand.value };
        System.Array.Sort(x);
        noiseParams.remap0 = RemapOffset.Evaluate(0) + rand.Range(remapRandomRange);
        noiseParams.remap1 = RemapOffset.Evaluate(1) + rand.Range(remapRandomRange);
        noiseParams.remap2 = new Vector2(x[0], RemapOffset.Evaluate(x[0]) + rand.Range(remapRandomRange));
        noiseParams.remap3 = new Vector2(x[1], RemapOffset.Evaluate(x[1]) + rand.Range(remapRandomRange));
        noiseParams.remap4 = new Vector2(x[2], RemapOffset.Evaluate(x[2]) + rand.Range(remapRandomRange));
        noiseParams.remap5 = new Vector2(x[3], RemapOffset.Evaluate(x[3]) + rand.Range(remapRandomRange));
        SetRemapCurveFromData();
    }

    private void SetRemapCurveFromData()
    {
        Keyframe[] keyframes = new Keyframe[6]
        {
            new Keyframe(0, noiseParams.remap0, 0, 0, 0, 0),
            new Keyframe(noiseParams.remap2.x, noiseParams.remap2.y, 0, 0, 0, 0),
            new Keyframe(noiseParams.remap3.x, noiseParams.remap3.y, 0, 0, 0, 0),
            new Keyframe(noiseParams.remap4.x, noiseParams.remap4.y, 0, 0, 0, 0),
            new Keyframe(noiseParams.remap5.x, noiseParams.remap5.y, 0, 0, 0, 0),
            new Keyframe(1, noiseParams.remap1, 0, 0, 0, 0)
        };
        CurrentRemap = new AnimationCurve(keyframes);
    }
    public void SyncRemapData()
    {
        if (CurrentRemap == null || CurrentRemap.length != 6)
        {
            SetRemapCurveFromData();
            return;
        }

        noiseParams.remap0 = CurrentRemap.keys[0].value;
        noiseParams.remap2 = new Vector2(CurrentRemap.keys[1].time, CurrentRemap.keys[1].value);
        noiseParams.remap3 = new Vector2(CurrentRemap.keys[2].time, CurrentRemap.keys[2].value);
        noiseParams.remap4 = new Vector2(CurrentRemap.keys[3].time, CurrentRemap.keys[3].value);
        noiseParams.remap5 = new Vector2(CurrentRemap.keys[4].time, CurrentRemap.keys[4].value);
        noiseParams.remap1 = CurrentRemap.keys[5].value;
    }
}