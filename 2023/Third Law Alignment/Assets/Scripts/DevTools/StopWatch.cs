using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StopWatch
{
    static Dictionary<string, System.Diagnostics.Stopwatch> stopWatches = new Dictionary<string, System.Diagnostics.Stopwatch>();
    static readonly decimal scale = 1m / System.Diagnostics.Stopwatch.Frequency;


    public static void Start(string name = "")
    {
        if (stopWatches.ContainsKey(name))
            stopWatches[name].Restart();
        else
        {
            stopWatches.Add(name, new System.Diagnostics.Stopwatch());
            stopWatches[name].Start();
        }
    }
    public static decimal Stop(string name = "", bool verbose = true)
    {
        stopWatches[name].Stop();

        decimal delta = stopWatches[name].ElapsedTicks * scale;
        stopWatches.Remove(name);
        if (verbose)
            Debug.Log(name + ": " + delta + "s");
        return delta;
    }

    public static decimal StopStart(string name = "", bool verbose = true)
    {
        decimal delta = Stop(name, verbose);
        Start(name);
        return delta;
    }
}
