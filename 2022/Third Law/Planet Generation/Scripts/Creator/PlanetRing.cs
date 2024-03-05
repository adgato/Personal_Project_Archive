using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
[ExecuteAlways]
#endif
public class PlanetRing : MonoBehaviour
{
    [SerializeField] private RobotWeight robotWeight;
    [SerializeField] private GameObject ringPrefab;
    private Planet planet;
    private int numRings = 1;
    public Color ringColour; //set to ocean colour in ColourGenerator.UpdateColours()
    public int environmentSeed; //set to environment seed in PlanetSettings.SyncValues()
    public Planet[] planets;
    private bool generated = false;


    private void SetMaterial()
    {
        Random.InitState(environmentSeed);

        if (planet == null)
            transform.parent.TryGetComponent(out planet);

        for (int i = 0; i < numRings; i++)
        {
            Transform ring = transform.GetChild(0).GetChild(i);
            ring.localScale = new Vector3(10, 0, 10) * planet.planetValues.radius + Vector3.up;
            ring.rotation = Random.rotation;

            Material ringMat = new Material(ring.GetComponent<MeshRenderer>().sharedMaterial);

            ringMat.SetColor("_colour", Color.Lerp(ringColour, Random.ColorHSV(), 0.1f));

            float a = Random.value * 0.4f + 0.4f;
            float b = Random.Range(a + 0.1f, a + 0.3f);
            ringMat.SetVector("_iris", new Vector2(a, b));

            ring.GetComponent<MeshRenderer>().sharedMaterial = ringMat;
        }

        Random.InitState(System.Environment.TickCount);
    }

    public void GenerateRings()
    {
        Random.InitState(environmentSeed);
        numRings = 0;

        if (Application.isPlaying)
            Destroy(transform.GetChild(0).gameObject);
        else
            DestroyImmediate(transform.GetChild(0).gameObject);

        Transform holder = new GameObject("Holder").transform;
        holder.parent = transform;
        holder.SetAsFirstSibling();
        holder.localPosition = Vector3.zero;
        holder.localRotation = Quaternion.identity;
        holder.localScale = Vector3.one;

        for (int i = 0; i < numRings; i++)
        {
            GameObject ring = Instantiate(ringPrefab, transform.position, transform.rotation, holder);
            ring.layer = gameObject.layer;
        }

        SetMaterial();
        UpdateRings(true);
        generated = true;
    }

    private void UpdateRings(bool first)
    {
        for (int i = 0; i < numRings; i++)
        {
            Material ringMat = transform.GetChild(0).GetChild(i).GetComponent<MeshRenderer>().sharedMaterial;
            ringMat.SetVector("_sunPosition", planet.atmosphere.sun.position);
            SetPlanetsValues(ref ringMat, !first);
            transform.GetChild(0).GetChild(i).GetComponent<MeshRenderer>().sharedMaterial = ringMat;
        }
    }

    private void SetPlanetsValues(ref Material ringMat, bool update)
    {
        if (!update)
            planets = FindObjectsOfType<Planet>(false);

        ringMat.SetVector("_pThisCentre", transform.position);
        ringMat.SetFloat("_pThisRadius", transform.localScale.x * 0.099f);

        for (int i = 0; i < 4; i++)
        {
            if (i >= planets.Length)
            {
                ringMat.SetVector("_p" + (i + 1).ToString() + "Atm", new Vector2(-1, 0));
                continue;
            }

            ringMat.SetVector("_p" + (i + 1).ToString() + "Centre", planets[i].transform.position);

            ringMat.SetVector("_p" + (i + 1).ToString() + "Atm", 
                new Vector2(
                    planets[i].transform.GetChild(2).GetComponent<PlanetEffect>().atmosRadius,
                    planets[i].transform.GetChild(2).GetComponent<PlanetEffect>().density)
                );
        }
    }

    private void Update()
    {
        if (generated)
            UpdateRings(false);
    }
}
