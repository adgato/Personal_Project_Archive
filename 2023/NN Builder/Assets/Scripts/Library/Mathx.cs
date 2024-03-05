using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Mathx
{
    public static int WrapMod(int x, int maxValue) => ((x % maxValue) + maxValue) % maxValue;
    public static int WrapMod(int x, int minValue, int maxValue) => WrapMod(x - minValue, maxValue - minValue) + minValue;
    public static float WrapMod(float x, float maxValue) => x - Mathf.Floor(x / maxValue) * maxValue;
    public static float WrapMod(float x, float minValue, float maxValue) => WrapMod(x - minValue, maxValue - minValue) + minValue;

    public static float Round(float x, int dp) => Mathf.Round(x * Mathf.Pow(10, dp)) / Mathf.Pow(10, dp);
    public static string RoundPadded(float x, int dp)
    {
        string output = Round(x, dp).ToString();
        if (!output.Contains("."))
            output += ".";
        while (output.Substring(output.IndexOf('.')).Length < dp + 1)
            output += "0";
        return output;
    }
}
