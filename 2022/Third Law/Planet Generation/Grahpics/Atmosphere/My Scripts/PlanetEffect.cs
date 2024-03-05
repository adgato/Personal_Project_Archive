using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
[ExecuteAlways]
#endif
public class PlanetEffect : MonoBehaviour
{
    public float cameraSqrDist { get; private set; }

    public bool active;

    [Space(20)]
    public float planetRadius; //updated in ColourGenerator.UpdatePlanetInfo()
    public float maxFogRange; //updated in ColourGenerator.UpdatePlanetInfo()
    public Transform sun;

    [Space(20)]
    public float atmosRadius; 
    public Color atmosColour; //set to ocean colour in ColourGenerator.UpdateColours()
    [Tooltip("Mix between red and sun colour")]
    public Color sunsetColour;
    public int environmentSeed; //set to environment seed in PlanetSettings.SyncValues()
    [Range(0, 6)]
    public float density;

    [Space(20)]
    [Tooltip("Between 30 and (atmosRadius & planetRadius) * (1 / density)")]
    public float fogRange;
    [Range(0, 10)]
    public float noiseFreq;
    [Range(0, 0.5f)]
    public float noiseAmp;
    [Range(0, 0.5f)]
    public float noiseBlend;


    [Space(20)]
    [Range(1, 10)]
    public float dispersionPower;
    [Range(0, 1)]
    public float dispersionScale;

    public Texture2D waveNormalA;
    public Texture2D waveNormalB;
    public float waveSpeed;
    public float waveStrength;
    public float waveNormalScale;
    public float smoothness;
    public float visibleDepth;

    private int numRings = 1;
    public Color ringColour; //set to ocean colour in ColourGenerator.UpdateColours()
    private Color ringColour1; 
    private Color ringColour2; 
    private Vector3 ringNormal1;
    private Vector3 ringNormal2;
    private float rings;
    private Vector2 iris1;
    private Vector2 iris2;
    
    public Material material { get; private set; }
    [Space(20)]
    private Shader effectShader;
    [SerializeField] private Shader atmosphereShader;
    [SerializeField] private Shader cloudShader;
    [SerializeField] private Gradient atmosGradient;
    [SerializeField] private Texture2D atmosTexture;

    [SerializeField] private Texture2D blueNoise;
    [SerializeField] private Texture2D cloudMap;
    [SerializeField] private float ditherStrength;
    [SerializeField] private float ditherScale;

    private int textureRes = 100;

    public void UpdateInfo()
    {
        cameraSqrDist = (Camera.main.transform.position - transform.position).sqrMagnitude;
    }
    public Material GetMaterial()
    {
        if (atmosTexture == null || material == null)
        {
            SetShaderVars();
            if (material == null || effectShader.name != material.shader.name)
                material = new Material(effectShader);
        }

        SetMaterial();

        return material;
    }

    public void OnValidate()
    {
        ResetMaterial();
    }

    public void ResetMaterial()
    {
        SetShaderVars();
        if (material == null || effectShader.name != material.shader.name)
            material = new Material(effectShader);
        SetMaterial();
    }

    private void SetMaterial()
    {
        material.SetFloat("numDiscs", numRings);
        material.SetFloat("rings", rings);
        material.SetVector("iris1", iris1);
        material.SetVector("iris2", iris2);
        material.SetVector("disc1Normal", ringNormal1);
        material.SetVector("disc2Normal", ringNormal2);
        material.SetVector("disc1Colour", ringColour1);
        material.SetVector("disc2Colour", ringColour2);

        material.SetVector("sunCentre", sun.position);
        material.SetVector("planetCentre", transform.parent.position);
        material.SetFloat("atmosphereRadius", atmosRadius);
        material.SetFloat("planetRadius", planetRadius);

        material.SetFloat("oceanRadius", planetRadius * 
            (transform.parent.GetComponent<Planet>().planetValues.roughBed && transform.parent.GetChild(0).gameObject.activeSelf ? transform.parent.GetComponent<Planet>().planetValues.seabedLevel : 0.99f));

        material.SetFloat("fogRange", fogRange);
        material.SetTexture("fogColourRings", atmosTexture);

        material.SetFloat("noiseFreq", noiseFreq);
        material.SetFloat("noiseAmp", noiseAmp);

        material.SetTexture("CloudMap", cloudMap);

        material.SetTexture("BlueNoise", blueNoise);
        material.SetFloat("ditherScale", ditherScale);
        material.SetFloat("ditherStrength", ditherStrength);

        material.SetFloat("density", density);
        material.SetFloat("dispersionPower", dispersionPower);
        material.SetFloat("dispersionScale", dispersionScale);

        material.SetFloat("windSpeed", transform.parent.GetComponent<Planet>().planetValues.windSpeed);
        material.SetFloat("roughBed", transform.parent.GetComponent<Planet>().planetValues.roughBed ? 1 : 0);

        material.SetFloat("cloudAltitude", 700);
        material.SetFloat("cloudLyrDepth", 25);

        material.SetTexture("waveNormalA", waveNormalA);
        material.SetTexture("waveNormalB", waveNormalB);

        Color.RGBToHSV(atmosColour, out float H, out float S, out _);
        material.SetVector("oceanColour", Color.HSVToRGB(H, S, 0.5f));
        material.SetFloat("visibleDepth", visibleDepth);
        material.SetFloat("waveSpeed", waveSpeed);
        material.SetFloat("waveStrength", waveStrength);
        material.SetFloat("waveNormalScale", waveNormalScale);
        material.SetFloat("smoothness", smoothness);
    }

    private void SetShaderVars()
    {
        Random.InitState(environmentSeed);

        //Rings
        numRings = Random.Range(0, 3);

        ringColour1 = Color.Lerp(ringColour, Random.ColorHSV(), 0.1f);
        ringColour2 = Color.Lerp(ringColour, Random.ColorHSV(), 0.1f);

        ringNormal1 = Random.onUnitSphere;
        ringNormal2 = Random.onUnitSphere;

        rings = 80;

        float a = Random.value * 0.4f + 0.4f;
        float b = Random.Range(a + 0.1f, a + 0.3f);
        iris1 = new Vector2(a, b);

        a = Random.value * 0.4f + 0.4f;
        b = Random.Range(a + 0.1f, a + 0.3f);
        iris2 = new Vector2(a, b);

        float h, s, v;
        //Atmosphere
        if (FindObjectOfType<SunGenSystem>() != null)
            Color.RGBToHSV(FindObjectOfType<SunGenSystem>().transform.GetChild(1).GetComponent<MeshRenderer>().sharedMaterial.GetColor("_sunColour"), out h, out s, out v);
        else
            (h, s, v) = (0, 0, 0);
        sunsetColour = Color.HSVToRGB(h * 0.5f, s, v);

        density = Mathf.Lerp(0.4f, 6, Mathf.Pow(Mathf.Sin(Random.value * Mathf.PI / 2), 3));

        effectShader = density > 3 && Random.value < 0.5f ? cloudShader : atmosphereShader; //~25% chance of clouds

        atmosRadius = Mathf.Lerp(1.1f * planetRadius, 2 * planetRadius, Mathx.MiddleCommon(Random.value));
        fogRange = Random.Range(30, Mathf.Min(maxFogRange, atmosRadius - planetRadius));

        noiseFreq = Random.value * Random.value * 10;
        noiseAmp = Random.value * Random.value * 0.5f;
        noiseBlend = Random.value * Random.value * 0.5f;

        int numKeys = 8;

        atmosGradient = new Gradient();
        GradientColorKey[] colourKey = new GradientColorKey[numKeys];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[numKeys];

        for (int i = 0; i < numKeys; i++)
        {
            float i01 = (float)i / (numKeys - 1);

            if (i == numKeys - 1)
                colourKey[i].color = colourKey[0].color; //smooth wrap around
            else
                colourKey[i].color = Color.Lerp(atmosColour, Random.ColorHSV(), noiseBlend);

            colourKey[i].time = Mathx.MiddleCommon(i01) + Random.Range(-0.01f, 0.01f);
            alphaKeys[i].alpha = 1;
        }

        atmosGradient.SetKeys(colourKey, alphaKeys);

        Color.RGBToHSV(sunsetColour, out float lerpH, out _, out _);

        Color[] colours = new Color[textureRes * textureRes];

        //Atmosphere texture with hue on y-axis (redder hue for sunset) and latitude on x-axis (for colour bands)
        for (int hue = 0; hue < textureRes; hue++)
        {
            for (int i = 0; i < textureRes; i++)
            {
                Color.RGBToHSV(atmosGradient.Evaluate((float)i / (textureRes - 1)), out float H, out float S, out float V);

                //Color.RGBToHSV(Color.Lerp(Color.HSVToRGB(H, 1, 1), Color.HSVToRGB(lerpH, 1, 1), (float)hue / (textureRes - 1)), out H, out _, out _);
                //H = Mathf.Lerp(H, lerpH, 1 - Mathf.Cos(Mathf.PI * hue / (textureRes - 1)));

                H = Mathf.Lerp(H, lerpH, (float)hue / (textureRes - 1));// ;

                colours[hue * textureRes + i] = Color.HSVToRGB(H, S, V);
            }
        }

        atmosTexture = new Texture2D(textureRes, textureRes);
        atmosTexture.SetPixels(colours);
        atmosTexture.Apply();

        Random.InitState(System.Environment.TickCount);
    }
}
