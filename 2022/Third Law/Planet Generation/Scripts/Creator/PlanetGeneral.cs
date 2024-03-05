using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Mathx
{
    //Continous function where a random value01 is more likely to be closer to 0.5 than 0 or 1: https://www.desmos.com/calculator/3rani8m1v0
    public static float MiddleCommon(float value01)
    {
        if (value01 > 1 || value01 < 0)
        {
            Debug.LogError("Error: float parameter \"value01\" in function \"MiddleCommon\" must be between 0 and 1 inclusive");
            return -1;
        }
            
        return value01 < 0.5f ? 0.5f * Mathf.Sin(value01 * Mathf.PI) : 1 - 0.5f * Mathf.Sin(value01 * Mathf.PI);
    }

    //Continous function where a random value01 is more likely to be closer to 0 or 1 than 0.5: https://www.desmos.com/calculator/3rani8m1v0
    public static float EndsCommon(float value01)
    {
        if (value01 > 1 || value01 < 0)
        {
            Debug.LogError("Error: float parameter \"value01\" in function \"MiddleCommon\" must be between 0 and 1 inclusive");
            return -1;
        }

        return 0.5f * Mathf.Sin((value01 - 0.5f) * Mathf.PI) + 0.5f;
    }

    private static List<char> alphaIndexLookup = new List<char>(26) { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };
    public static string IntToAlpha26(int i)
    {
        string alpha = "";
        alpha += alphaIndexLookup[i % 26];
        i /= 26;
        if (i != 0)
            alpha += IntToAlpha26(i);
        return alpha;
    }
    public static int Alpha26ToInt(string alpha)
    {
        List<int> alphaInts = new List<int>(alpha.Length);
        foreach (char _a in alpha)
        {
            int index = alphaIndexLookup.IndexOf(_a);
            if (index < 0)
                Debug.LogError("Error: " + _a + " is not a lower case letter, using 'z' instead");
            alphaInts.Add(index);
        }

        int Alpha26ToInt(int q, int n)
        {
            return n < alpha.Length ? Alpha26ToInt(q * 26 + alphaInts[alpha.Length - (n + 1)], n + 1) : q;
        }

        return Alpha26ToInt(0, 0);
    }
}
