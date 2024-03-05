using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlanetMaterial : MonoBehaviour
{
    [SerializeField] private GameObject pathPrefab;
    [SerializeField] private CustomScreenEffects screenEffects;
    [SerializeField] private Material planetTerrainMat;
    private PlanetWeight planetWeight;
    public Color biomeColour1 { get; private set; }
    public Color biomeColour2 { get; private set; }
    private float temperature01; //0 for hot, 1 for cold
    private float planetHue01;

    private Coroutine ShiftWhiteBalance;


    private Rand rand;

    // Start is called before the first frame update
    public void Initialise(Rand.Seed seed, Mesh[] planetPaths, float planetRadius, float temperature01)
    {
        this.temperature01 = temperature01;
        planetWeight = transform.parent.GetComponent<PlanetWeight>();
        GetComponent<MeshRenderer>().sharedMaterial = planetTerrainMat = new Material(planetTerrainMat);
        planetTerrainMat.SetFloat("_Radius", planetRadius);

        rand = new Rand(seed);
        Randomise();

        StartCoroutine(UpdateWhiteBalance());

        InstantiatePaths(planetPaths);
    }


    private IEnumerator UpdateWhiteBalance()
    {
        bool wasClosest = false;
        float whiteBalanceStrength = 50;
        while (true)
        {
            if (PlayerRobotWeight.Player.EqualsClosest(planetWeight))
            {
                if (!wasClosest)
                {
                    wasClosest = true;
                    if (ShiftWhiteBalance != null)
                        StopCoroutine(ShiftWhiteBalance);
                    ShiftWhiteBalance = StartCoroutine(VolumeLerp(Mathf.Cos(2 * Mathf.PI * planetHue01), -Mathf.Sin(2 * Mathf.PI * planetHue01), whiteBalanceStrength));
                }
            }
            else if (PlayerRobotWeight.Player.Closest == null && wasClosest)
            {
                wasClosest = false;
                if (ShiftWhiteBalance != null)
                    StopCoroutine(ShiftWhiteBalance);
                ShiftWhiteBalance = StartCoroutine(VolumeLerp(0, 0, 0)); //no planet closest, reset to 0
            }
            else if (PlayerRobotWeight.Player.Closest != null)
            {
                wasClosest = false;
                if (ShiftWhiteBalance != null)
                {
                    StopCoroutine(ShiftWhiteBalance);
                    ShiftWhiteBalance = null;
                }
            }
            yield return new WaitForSeconds(1);
        }
    }

    private void OnDestroy()
    {
        if (screenEffects.CameraVolume.TryGet(out WhiteBalance whiteBalance))
        {
            whiteBalance.temperature.value = 0;
            whiteBalance.tint.value = 0;
        }
    }

    private void InstantiatePaths(Mesh[] pathMeshes)
    {
        Transform holder = gameObject.ReplaceChild(1, "Path Meshes");
        pathPrefab.GetComponent<MeshRenderer>().sharedMaterial = planetTerrainMat;
        for (int i = 0; i < pathMeshes.Length; i++)
        {
            GameObject pathObject = Instantiate(pathPrefab, holder);
            pathObject.GetComponent<MeshFilter>().sharedMesh = pathObject.GetComponent<MeshCollider>().sharedMesh = pathMeshes[i];
        }
    }

    private void Randomise()
    {
        planetHue01 = (rand.Chance(0.6f) ? temperature01 * 0.5f : 1 - temperature01 * 0.5f); //more differentiable colours for 0 < hue < 0.5
        biomeColour1 = GetRandomThermalColour(0.1f);
        biomeColour2 = GetRandomThermalColour(0.3f);

        planetTerrainMat.SetColor("_Colour_1", biomeColour1);
        planetTerrainMat.SetColor("_Colour_2", biomeColour2);

        planetTerrainMat.SetFloat("_Biome_Offset", rand.Range(-9999, 9999));

        planetTerrainMat.SetFloat("_Steep_Hue_Shift", rand.Chance(0.5f) ? 0.2f : -0.2f);
        planetTerrainMat.SetFloat("_Altitude_Hue_Shift", rand.Chance(0.5f) ? 0.2f : -0.2f);
    }

    IEnumerator VolumeLerp(float whiteBalanceTemperature, float whiteBalanceTint, float whiteBalanceStrength)
    {
        float lerpTime = 3;
        float start = Time.time;
        if (!screenEffects.CameraVolume.TryGet(out WhiteBalance whiteBalance))
            yield break;

        while (Time.time < start + lerpTime)
        {
            float t = Mathf.InverseLerp(start, start + lerpTime, Time.time);

            whiteBalance.temperature.value = Mathf.Lerp(whiteBalance.temperature.value, whiteBalanceTemperature * whiteBalanceStrength, t);
            whiteBalance.tint.value = Mathf.Lerp(whiteBalance.tint.value, whiteBalanceTint * whiteBalanceStrength, t);

            screenEffects.temperature = Mathf.InverseLerp(-whiteBalanceStrength, whiteBalanceStrength, whiteBalance.temperature.value);
            
            yield return null;
        }
    }

    Color GetRandomThermalColour(float variation)
    {
        float hue = Mathx.Mod(rand.Range(-1f, 1f) * variation + planetHue01, 1);
        float sat = rand.Range(0.1f, 1f);
        float val = Mathf.Sqrt(rand.Range(0.2f, 1f));

        return Color.HSVToRGB(hue, sat, val);
    }
}
