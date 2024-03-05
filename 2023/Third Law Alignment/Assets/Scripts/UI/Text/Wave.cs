using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Wave
{
    private float time = 0;

    [SerializeField] private float amplitude = 2;
    [SerializeField] private float wavespeed = 5;
    [SerializeField] private float wavelength = 25;
    [Min(0)]
    [SerializeField] private float damping = 0;

    private int ticker;

    public Wave() { }
    public Wave(float amplitude, float wavespeed, float wavelength, float damping = 0)
    {
        SetParameters(amplitude, wavespeed, wavelength, damping);
    }
    public void SetParameters(float amplitude, float wavespeed, float wavelength, float damping = 0)
    {
        this.amplitude = amplitude;
        this.damping = damping;
        this.wavelength = wavelength;
        this.wavespeed = wavespeed;
    }

    public void AddTime(float deltaTime)
    {
        time += wavespeed * deltaTime;
        ticker = Time.frameCount;
    }
    public void ResetTime()
    {
        time = 0;
        ticker = Time.frameCount;
    }

    public float Displacement(float alongwave)
    {
        if (ticker != Time.frameCount)
            AddTime(Time.deltaTime);

        float angle = time + alongwave / wavelength;
        return amplitude * Mathf.Exp(-damping * time) * Mathf.Sin(angle);
    }
}
