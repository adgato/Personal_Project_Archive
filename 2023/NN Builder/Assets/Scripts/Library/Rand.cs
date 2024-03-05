using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rand
{
    private System.Random prng;

    public static Rand stream = new Rand(0);

    public float value { get { return (float)prng.NextDouble(); } }

    public Rand(int seed = -1)
    {
        if (seed == -1)
            seed = System.Environment.TickCount;
        prng = new System.Random(seed);
    }

    public bool Chance(float probability)
    {
        return value < probability;
    }

    public float Range(float min, float max)
    {
        return Mathf.Lerp(min, max, value);
    }
    public int Range(int minInc, int maxExc)
    {
        return minInc + Mathf.FloorToInt((maxExc - minInc) * value);
    }
}
