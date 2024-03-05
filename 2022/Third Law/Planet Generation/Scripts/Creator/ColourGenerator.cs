using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColourGenerator
{
    private Planet planet;
    private Texture2D planetTexture;
    
    private const int textureRes = 50;

    public ColourGenerator(Planet _planet)
    {
        planet = _planet;
        if (planetTexture == null)
            planetTexture = new Texture2D(textureRes, textureRes);
    }
    public void UpdateElevation(ElevationData elevationData)
    {
        planet.terrainMat.SetFloat("_minElevation", elevationData.Min);
        planet.terrainMat.SetFloat("_maxElevation", elevationData.Max);
    }
    public void UpdateColours()
    {
        float lerpRange = 0.05f;

        for (int lat = 0; lat < textureRes; lat++)
        {
            float lat01 = (float)lat / textureRes;
            KeyValuePair<float, Gradient> biomeBelow = new KeyValuePair<float, Gradient>(0, null);
            KeyValuePair<float, Gradient> biomeAbove = new KeyValuePair<float, Gradient>(2, null);

            //Get the biome gradients below and above the current latitude
            foreach (KeyValuePair<float, Gradient> biome in planet.planetValues.biomeGradients)
            {
                if (biome.Key <= lat01 && lat01 - biome.Key <= lat01 - biomeBelow.Key)
                    biomeBelow = biome;
                else if (biome.Key > lat01 && biome.Key - lat01 <= biomeAbove.Key - lat01)
                    biomeAbove = biome;
            }

            for (int alt = 0; alt < textureRes; alt++)
            {
                float alt01 = (float)alt / textureRes;
                Color pixel;
                //Lerp between the biome gradients based on the distance to the biome above
                if (biomeAbove.Key != 2 && biomeAbove.Key - lerpRange < lat01)
                    pixel = Color.Lerp(biomeBelow.Value.Evaluate(alt01), biomeAbove.Value.Evaluate(alt01), Mathf.InverseLerp(biomeAbove.Key - lerpRange, biomeAbove.Key, lat01));
                else
                    pixel = biomeBelow.Value.Evaluate(alt01);
                    

                planetTexture.SetPixel(alt, lat, pixel);
            }
        }

        planetTexture.Apply();
        
        planet.terrainMat.SetTexture("_planetTexture", planetTexture);
        planet.setPlanetValues.readonlyPlanetTexture = planet.terrainMat.GetTexture("_planetTexture");

        Random.InitState(planet.planetValues.colourSeed);

        //Choose a random pixel from the lowest latitude on the planet's texture to use as the sea color
        Color seaColour = planetTexture.GetPixel(Random.Range(0, textureRes), 0);

        Color.RGBToHSV(seaColour, out float H, out float _, out float _);

        H = Mathf.Lerp(Mathx.EndsCommon(Random.value), H, 0.75f);
        float S = Random.value;

        if (planet.TryGetComponent(out Weight planetWeight))
            planetWeight.colour = Color.HSVToRGB(H, S, 1);

        planet.atmosphere.atmosColour = Color.HSVToRGB(H, S, Random.Range(0.4f, 0.6f));
        planet.atmosphere.ringColour = Color.HSVToRGB(H, S, Mathf.Clamp01(0.2f + Random.value));

        Random.InitState(System.Environment.TickCount);
    }
    public void UpdatePlanetInfo()
    {
        planet.oceanMat.SetFloat("_radius", planet.planetValues.radius);
        planet.terrainMat.SetVector("_planetCentre", planet.transform.position);

        planet.atmosphere.planetRadius = planet.planetValues.radius;
        planet.atmosphere.maxFogRange = planet.maxNatureDist;
    }

}
