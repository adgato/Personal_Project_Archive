using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetSettings
{
    private Planet planet;
    private PlanetValues planetValues;
    private CraterValues craterValues;
    private ContinentNoiseValues continentNoise;
    private MountainNoiseValues mountainNoise;
    private WarpNoiseValues warpNoise;
    private RoughNoiseValues roughNoise;

    public PlanetSettings(Planet _planet)
    {
        planet = _planet;
        continentNoise.octaves = 3;
        mountainNoise.octaves = 3;
        warpNoise.octaves = 3;
        roughNoise.octaves = 4;
        roughNoise.startingOctave = 5;

        planetValues.terrainSeed = Mathx.Alpha26ToInt(planet.setPlanetValues.masterSeed.Substring(0, 4));
        planetValues.colourSeed = Mathx.Alpha26ToInt(planet.setPlanetValues.masterSeed.Substring(4, 4));
        planetValues.environmentSeed = Mathx.Alpha26ToInt(planet.setPlanetValues.masterSeed.Substring(8, 4));
        planetValues.seabedLevel = planet.setPlanetValues.seabedLevel;

        mountainNoise.initialExtraOctaves = planet.setPlanetValues.mtInitialExtraOctaves;

        craterValues.numCraters = planet.setCraterValues.numCraters;
        craterValues.smoothness = planet.setCraterValues.smoothness;
        craterValues.rimWidth = planet.setCraterValues.rimWidth;
        craterValues.rimSteepness = planet.setCraterValues.rimSteepness;
    }
    public void SyncSettings()
    {
        planetValues.crackedGround = planet.setPlanetValues.crackedGround;

        GetTerrain();
        GetGradients();

        SyncValues();
    }

    public void RandomiseTerrainSeed(int seed = int.MaxValue)
    {
        if (seed == int.MaxValue)
            seed = Random.Range(-9999, 9999);
        Random.InitState(seed);
        planetValues.terrainSeed = Random.Range(0, 456975); //(26^4 - 1) is zzzz in alpha26 (and thus the max value the seed can take)
        Random.InitState(System.Environment.TickCount);
    }
    public void RandomiseColourSeed(int seed = int.MaxValue)
    {
        if (seed == int.MaxValue)
            seed = Random.Range(-9999, 9999);
        Random.InitState(seed);
        planetValues.colourSeed = Random.Range(0, 456975); //(26^4 - 1) is zzzz in alpha26 (and thus the max value the seed can take)
        Random.InitState(System.Environment.TickCount);
    }
    public void RandomiseEnvironmentSeed(int seed = int.MaxValue)
    {
        if (seed == int.MaxValue)
            seed = Random.Range(-9999, 9999);
        Random.InitState(seed);
        planetValues.environmentSeed = Random.Range(0, 456975); //(26^4 - 1) is zzzz in alpha26 (and thus the max value the seed can take)
        Random.InitState(System.Environment.TickCount);
    }

    private void GetTerrain()
    {
        Random.InitState(planetValues.terrainSeed);

        planetValues.radius = Random.Range(250, 1000);
        planet.transform.GetChild(3).localScale = planetValues.radius * Vector3.one;

        continentNoise.seed = Random.Range(-9999, 9999);
        mountainNoise.seed = Random.Range(-9999, 9999);
        warpNoise.seed = Random.Range(-9999, 9999);
        craterValues.seed = Random.Range(-9999, 9999);
        roughNoise.seed = Random.Range(-9999, 9999);

        planetValues.groundLevel = Random.Range(1, 1.02f);
        planetValues.windSpeed = Random.Range(0.5f, 2);

        continentNoise.scale = Random.Range(0.1f, 2.5f);
        continentNoise.persistance = Random.Range(0.1f, 0.3f);
        continentNoise.lacunarity = Random.Range(5f, continentNoise.persistance > 0.2f ? 7.5f : 10);
        continentNoise.dropoff = Random.Range(0.1f, 0.2f);

        mountainNoise.scale = Random.Range(0.1f, 2);
        mountainNoise.persistance = Random.Range(0.1f, 0.15f);
        mountainNoise.lacunarity = Random.Range(5f, 10);
        mountainNoise.dropoff = Random.Range(0.25f, 0.35f);

        warpNoise.scale = Random.Range(0f, 2);
        warpNoise.persistance = Random.Range(0f, 1);
        warpNoise.lacunarity = Random.Range(1f, 3);

        roughNoise.persistance = Random.Range(0.5f, 0.6f);
        roughNoise.lacunarity = Random.Range(1.5f, 1.7f);

        Random.InitState(System.Environment.TickCount);
    }

    private void GetGradients()
    {
        Random.InitState(planetValues.colourSeed);

        planetValues.temperature = Mathx.MiddleCommon(Mathf.InverseLerp(10000, 50000, (Object.FindObjectOfType<SunGenSystem>().transform.position - planet.transform.position).magnitude));
        planetValues.roughBed = planetValues.temperature < 0.2f || planetValues.temperature > 0.8f || Random.value < 0.5f;

        //Zero if the planet has a rough bed (idea being there is no large water supply on the planet).
        planet.treeCountPer500Seg = planetValues.roughBed ? new Vector2(0, 0) : new Vector2(0.1f, 0.9f);
        planet.grassCountPer500Seg = planetValues.roughBed ? new Vector2Int(0, 1) : new Vector2Int(2, 6);

        int numBiomes = 3;
        planetValues.biomeGradients = new Dictionary<float, Gradient>(3);
        float heatLerp = Random.value;
        float biomeHeat = planetValues.temperature;

        for (int i = 0; i < numBiomes; i++)
        {
            planetValues.biomeGradients.Add(i == 0 ? 0 : Random.value, NewBiomeGradient(Mathf.Clamp01(biomeHeat + Random.Range(-0.1f, 0.1f)), Mathf.Clamp01(heatLerp + Random.Range(-0.1f, 0.1f))));
        }

        Random.InitState(System.Environment.TickCount);
    }

    private Gradient NewBiomeGradient(float biomeTemperature, float heatLerp)
    {
        Color sunColour = Object.FindObjectOfType<SunGenSystem>().transform.GetChild(1).GetComponent<MeshRenderer>().sharedMaterial.GetColor("_sunColour");

        Gradient heatGrad = new Gradient();
        GradientColorKey[] heatColourKeys = new GradientColorKey[4];
        GradientAlphaKey[] heatAlphaKeys = new GradientAlphaKey[4];
        int i = 0;
        //Blend between the two fixed heat gradients by heatLerp and store in heatGrad
        foreach (float _t in new float[4] { 0, 0.4f, 0.6f, 1 })
        {
            float t = Mathf.Clamp01(_t + Random.Range(-0.1f, 0.1f));
            heatColourKeys[i].color = Color.Lerp(planet.heatGrad1.Evaluate(t), planet.heatGrad2.Evaluate(t), heatLerp);
            heatColourKeys[i].time = t;
            heatAlphaKeys[i].alpha = 1;
            i++;
        }
        heatGrad.SetKeys(heatColourKeys, heatAlphaKeys);

        float lowerMean01 = planet.planetMesh.elevationData.lowerMean01;
        float upperMean01 = planet.planetMesh.elevationData.upperMean01;

        Gradient gradient = new Gradient();

        int keys = 6;
        //Initial time is smaller if terrain is more common at lower altitudes, this concentrates more colours at lower altitudes if necessary
        float time = Mathf.Lerp(0, lowerMean01, Random.value * Random.value);
        //Want at least one new colour beyond the upperMean
        float minTimeInc = (upperMean01 - time) / (keys - 1);
        //Want no new colours in the top 20% of altitudes, we will manually add these
        float maxTimeInc = (0.8f - time) / (keys - 1);

        float temperature = biomeTemperature;
        //Temperature decreases with altitude
        float temperatureInc = Mathf.Min(0.2f, (1 - temperature)) / (keys - 1);

        GradientColorKey[] colourKeys = new GradientColorKey[keys + 2];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[keys + 2];
        for (int j = 0; j < keys; j++)
        {
            colourKeys[j].time = time;

            //A colour at a time on the gradient is a 50% mix of the appropriate temperature colour for the time and a random colour (makes planets more visually unique while still being coherent)
            colourKeys[j].color = Color.Lerp(heatGrad.Evaluate(temperature), RandomColourVarient(sunColour, 0.25f), 0.5f);
            alphaKeys[j].alpha = 1;

            time += Random.Range(minTimeInc, maxTimeInc);
            temperature += Random.Range(0, temperatureInc);
            temperature = Mathf.Clamp(temperature, 0, 1);
        }
        //Manually add colours in the top 20% of altitudes (to resemble the cold white tips at the top of mountains)
        colourKeys[keys].time = 0.8f;
        colourKeys[keys].color = colourKeys[keys - 1].color;
        alphaKeys[keys].alpha = 1;
        colourKeys[keys + 1].time = 0.85f;
        colourKeys[keys + 1].color = Color.Lerp(heatGrad.Evaluate(Mathf.Clamp01(biomeTemperature + 0.5f)), RandomColourVarient(sunColour, 0.25f), 0.5f);
        alphaKeys[keys + 1].alpha = 1;

        gradient.SetKeys(colourKeys, alphaKeys);
        return gradient;
    }

    private Color RandomColourVarient(Color original, float lerp)
    {
        Color.RGBToHSV(original, out float h, out _, out _);
        float newH = Random.value * (1 - lerp) + h * lerp;
        return Color.HSVToRGB(newH, Random.value, Random.value);
    }

    public void SyncValues()
    {
        planet.setPlanetValues.masterSeed = Mathx.IntToAlpha26(planetValues.terrainSeed) + Mathx.IntToAlpha26(planetValues.colourSeed) + Mathx.IntToAlpha26(planetValues.environmentSeed);
        while (planet.setPlanetValues.masterSeed.Length < 12)
            planet.setPlanetValues.masterSeed = "a" + planet.setPlanetValues.masterSeed;


        planet.atmosphere.environmentSeed = planetValues.environmentSeed;

        planet.transform.GetComponent<Weight>().SetMass(10 + 20 * Mathf.InverseLerp(0.4f, 6, planet.atmosphere.density), planetValues.radius);
        planet.setPlanetValues.roughBed = planetValues.roughBed;

        planet.planetValues = planetValues;
        planet.craterValues = craterValues;
        planet.continentNoise = continentNoise;
        planet.mountainNoise = mountainNoise;
        planet.warpNoise = warpNoise;
        planet.roughNoise = roughNoise;

        planet.setPlanetValues.readonlyplanetRadius = planetValues.radius;
        planet.setPlanetValues.readonlyTemperature = planetValues.temperature;
    }
}
