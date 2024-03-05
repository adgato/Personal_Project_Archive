using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetEffects : MonoBehaviour
{
    [SerializeField] private Shader effectsShader;
    private BlitMaterial blitMaterial;

    [SerializeField] private Texture2D blueNoise;
    [SerializeField] private Texture2D perlinNoise;
    [SerializeField] private Texture3D simplexNoise;
    private float k;
    [SerializeField] private float sunIntensity;
    [SerializeField] private float planetRadius = 400;

    [SerializeField] private float rayleighHeight = 50;
    [SerializeField] private float fogHeight = 30;
    [SerializeField] private float fogOpacity;
    [SerializeField] private float fogRange = 50;

    [Space]
    [Range(0, 1)] [SerializeField] private float redWavelength01 = 1;
    [Range(0, 1)] [SerializeField] private float greenWavelength01 = 0.8f;
    [Range(0, 1)] [SerializeField] private float blueWavelength01 = 0.7f;

    [Space]
    [ReadOnly] [SerializeField] private Color fog1 = Color.grey;
    [ReadOnly] [SerializeField] private Color fog2 = Color.grey;
    [ReadOnly] [SerializeField] private Color disc1 = Color.grey;
    [ReadOnly] [SerializeField] private Color disc2 = Color.grey;
    [ReadOnly] [SerializeField] private Color atmosRed = Color.red;
    [ReadOnly] [SerializeField] private Color atmosGreen = Color.green;
    [ReadOnly] [SerializeField] private Color atmosBlue = Color.blue;

    [Space]
    [SerializeField] private float numDiscs;
    [Range(0, 2)]
    [SerializeField] private int maxDiscs;
    [SerializeField] private float rings;
    [SerializeField] private Vector3 disc1normal;
    [SerializeField] private Vector2 disc1iris; //larger range is better (but > 0.5)
    [SerializeField] private Vector3 disc2normal;
    [SerializeField] private Vector2 disc2iris;

    [Space]
    [SerializeField] private float waveSpeed;
    [SerializeField] private float waveStrength;
    [SerializeField] private float waveSmoothness;
    [SerializeField] private float waveNormalScale;
    [SerializeField] private Texture2D waveNormalA;
    [SerializeField] private Texture2D waveNormalB;

    [SerializeField] private float oceanRadius;
    [SerializeField] private Color planetColour1;
    [SerializeField] private Color planetColour2;

    private Rand rand;

    //very much temporary
    [SerializeField] private Light sun;

    // Start is called before the first frame update
    public void Initialise(Rand.Seed seed, float maxPlanetRadius, float oceanRadius, Color planetColour1, Color planetColour2)
    {
        if (blitMaterial != null)
            blitMaterial.Dispose();

        blitMaterial = new BlitMaterial(new Material(effectsShader), 0);
        blitMaterial.Enable();

        this.oceanRadius = oceanRadius;
        this.planetColour1 = planetColour1;
        this.planetColour2 = planetColour2;

        rand = new Rand(seed);
        Randomise(maxPlanetRadius);
        SetMaterial();
    }
    private void Randomise(float maxPlanetRadius)
    {
        //OceanRadius = rand.Range(10, Mathf.Max(10, maxPlanetRadius - 150), x => Mathf.Pow(x, 0.25f));
        planetRadius = Mathf.Max(10, maxPlanetRadius - rand.Range(160f, 240));

        fogOpacity = Mathf.Pow(rand.value, 4); //dont really like high fog atmospheres
        rayleighHeight = Mathf.Lerp(25, 70, fogOpacity);
        fogHeight = Mathf.Lerp(20, 40, fogOpacity);
        k = Mathf.Lerp(0.001f, 0.02f, fogOpacity);
        fogOpacity = rand.Chance(0.33f) ? 0.1f : fogOpacity < 0.5f ? 0.97f : 1;

        numDiscs = rand.Range(0, maxDiscs + 1);
        disc1normal = rand.normal;
        disc2normal = rand.normal;

        float iris1x = rand.Range(0.4f, 0.8f);
        float iris2x = rand.Range(iris1x, 0.8f);
        disc1iris = new Vector2(iris1x, iris1x + rand.Range(0.1f, 0.2f));
        disc2iris = new Vector2(iris2x, iris2x + rand.Range(0.1f, 0.2f));
    }

    private void OnValidate()
    {
        if (Application.isPlaying && blitMaterial != null)
        {
            blitMaterial.Dispose();
            blitMaterial = new BlitMaterial(new Material(effectsShader), 0);
            blitMaterial.Enable();
        }
        SetMaterial();
    }

    private void OnDestroy()
    {
        if (blitMaterial != null)
            blitMaterial.Dispose();
    }

    void SetMaterial()
    {
        if (blitMaterial == null)
            return;

        Color.RGBToHSV(planetColour1, out float hue1, out float sat1, out float val1);
        Color.RGBToHSV(planetColour2, out float hue2, out float sat2, out float val2);

        Matrix4x4 skyColourMatrix = new Matrix4x4(
            Color.HSVToRGB(Mathx.Mod(hue1 - 0.500000000000f, 1), 1, 1),
            Color.HSVToRGB(Mathx.Mod(hue1 - 0.166666666667f, 1), 1, 1),
            Color.HSVToRGB(Mathx.Mod(hue1 + 0.166666666667f, 1), 1, 1),
            Vector4.zero);

        Color fogColour1 = Color.HSVToRGB(hue1, sat1 * 0.5f, val1 * 0.75f);
        Color fogColour2 = Color.HSVToRGB(hue2, sat2 * 0.5f, val2 * 0.75f); 
        Color discColour1 = Color.HSVToRGB(hue1, sat1 * 0.5f, val1 * 0.75f);
        Color discColour2 = Color.HSVToRGB(hue1, sat2 * 0.5f, val1 * 0.75f);

        //atmoshpere height at which optical depth is basically 0 giving a colour almost (but not quite) black, so atmosphere radius cannot be any smaller
        float atmosphereRadius = planetRadius - rayleighHeight * Mathf.Log(0.003f * sunIntensity);
        float fogRadius = planetRadius - fogHeight * Mathf.Log(0.003f * sunIntensity);

        //for debug purposes
        atmosRed = skyColourMatrix.GetColumn(0);
        atmosGreen = skyColourMatrix.GetColumn(1);
        atmosBlue = skyColourMatrix.GetColumn(2);
        fog1 = fogColour1;
        fog2 = fogColour2;
        disc1 = discColour1;
        disc2 = discColour2;

        blitMaterial.Material.SetFloat("k", k);
        blitMaterial.Material.SetFloat("atmosphereRadius", atmosphereRadius * 1.1f);
        blitMaterial.Material.SetFloat("fogRadius", fogRadius * 1.1f);
        blitMaterial.Material.SetFloat("planetRadius", planetRadius);
        blitMaterial.Material.SetFloat("oceanRadius", oceanRadius);
        blitMaterial.Material.SetFloat("sunIntensity", sunIntensity);
        blitMaterial.Material.SetFloat("rayleighHeight", rayleighHeight);
        blitMaterial.Material.SetFloat("fogHeight", fogHeight);
        blitMaterial.Material.SetFloat("fogRange", fogRange);
        blitMaterial.Material.SetFloat("fogOpacity", fogOpacity);

        blitMaterial.Material.SetFloat("numDiscs", numDiscs);
        blitMaterial.Material.SetFloat("rings", rings);
        blitMaterial.Material.SetColor("disc1colour", discColour1);
        blitMaterial.Material.SetVector("disc1normal", disc1normal);
        blitMaterial.Material.SetVector("disc1iris", disc1iris);
        blitMaterial.Material.SetVector("disc2colour", discColour2);
        blitMaterial.Material.SetVector("disc2normal", disc2normal);
        blitMaterial.Material.SetVector("disc2iris", disc2iris);

        blitMaterial.Material.SetFloat("waveSpeed", waveSpeed);
        blitMaterial.Material.SetFloat("waveStrength", waveStrength);
        blitMaterial.Material.SetFloat("waveSmoothness", waveSmoothness);
        blitMaterial.Material.SetFloat("waveNormalScale", waveNormalScale);
        blitMaterial.Material.SetTexture("waveNormalA", waveNormalA);
        blitMaterial.Material.SetTexture("waveNormalB", waveNormalB);

        blitMaterial.Material.SetVector("wavelengths", new Vector3(redWavelength01, greenWavelength01, blueWavelength01));

        blitMaterial.Material.SetColor("fogColour1", fogColour1);
        blitMaterial.Material.SetColor("fogColour2", fogColour2);
        blitMaterial.Material.SetMatrix("atmosphereColours", skyColourMatrix);

        blitMaterial.Material.SetTexture("BlueNoise", blueNoise);
        blitMaterial.Material.SetTexture("PerlinNoise", perlinNoise);
        blitMaterial.Material.SetTexture("SimplexNoise", simplexNoise);

        blitMaterial.Material.SetVector("planetCentre", transform.position);

        //very much temporary
        blitMaterial.Material.SetVector("sunDir", -sun.transform.forward);

    }

    public void SetPlanetPosition(Vector3 planetCentre)
    {
        blitMaterial.Material.SetVector("planetCentre", planetCentre);
        blitMaterial.ChangeLayer(Mathf.RoundToInt((PlayerRobotWeight.Player.Position - transform.position).sqrMagnitude));
    }
}
