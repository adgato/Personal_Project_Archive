using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Common
{
    /// <summary>
    /// Cam probably be more efficient but this is pretty neat.
    /// </summary>
    public static string Repeat(this string str, int n)
    {
        if (n <= 0)
            return "";
        string repeat = str;
        int m = 1;
        while (n >= 2 * m)
        {
            repeat += repeat;
            m <<= 1;
        }
        return repeat + Repeat(str, n - m);
    }
}
