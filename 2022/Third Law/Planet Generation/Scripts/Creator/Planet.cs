using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Planet : MonoBehaviour
{
    public RobotWeight robotWeight; //NOTE THIS VARIABLE IS INITIALISED BY THE FIRST INTERFACE THAT ACCESSES IT

    public MeshGen planetMesh { get; private set; }
    public PlanetEffect atmosphere;
    public GameObject[] planetMeshSegmentPrefabs;
    private ColourGenerator colourGenerator;


    private MiniMap miniMap;
    public GameObject globeMapPrefab;

    public Material terrainMat { get; private set; }
    public Material oceanMat { get; private set; }
    public Craters[] craters { get; private set; }
    public readonly int resolution = 600;
    public readonly int splitFaces = 12;

    [Header("Manually Set Objects:")]
    public ComputeShader noiseShader;
    [SerializeField] private Material terrainPrefab;
    [SerializeField] private Material oceanPrefab;
    public Gradient heatGrad1;
    public Gradient heatGrad2;

    [Header("Nature:")]
    public GameObject Tree;
    public GameObject Ore;
    public GameObject Tower;

    [Tooltip("X: Start of random range Y: End inclusive")]
    public Vector2 treeCountPer500Seg;
    [Tooltip("X: Start of random range Y: End inclusive")]
    public Vector2 oreCountPer500Seg;
    [Tooltip("X: Start of random range Y: End inclusive")]
    public Vector2 grassCountPer500Seg;
    [Tooltip("X: Start of random range Y: End inclusive")]
    public Vector2 stoneCountPer500Seg;
    public float maxNatureDist;

    private float renderPlanetSegsBelowSqrDist = 100_000_000; //10_000^2
    private float dontRenderPlanetSegsAboveSqrDist = 6_400_000_000; //80_000^2

    [System.Serializable]
    public class SetPlanetValues
    {
        [Tooltip("12 lower-case letters")]
        public string masterSeed;
        
        public float seabedLevel;
        public bool roughBed;
        public bool crackedGround;
        public int mtInitialExtraOctaves;
        public float readonlyplanetRadius;
        public float readonlyTemperature;
        public Texture readonlyPlanetTexture;
    }

    [System.Serializable]
    public class SetCraterValues
    {
        public int numCraters;
        public float smoothness;
        public float rimWidth;
        public float rimSteepness;
    }

    public SetPlanetValues setPlanetValues;
    public SetCraterValues setCraterValues;
    public PlanetValues planetValues;
    public CraterValues craterValues;
    public ContinentNoiseValues continentNoise;
    public MountainNoiseValues mountainNoise;
    public WarpNoiseValues warpNoise;
    public RoughNoiseValues roughNoise;

    public PlanetSettings planetSettings { get; private set; }

    public void Init()
    {
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.layer = gameObject.layer;

        terrainMat = new Material(terrainPrefab);
        oceanMat = new Material(oceanPrefab);

        //transform.GetChild(1).GetComponent<MeshRenderer>().sharedMaterial = oceanMat;

        planetSettings = new PlanetSettings(this);
        colourGenerator = new ColourGenerator(this);

        //Set the ocean & atmosphere size to the radius of the planet
        transform.GetChild(2).localScale = Vector3.one * planetValues.radius;

        planetMesh = new MeshGen(this);
        //float start = (float)EditorApplication.timeSinceStartup;
        planetMesh.ApplyQuadSphere();
        //Debug.Log("Quadsphere generated in: " + ((float)EditorApplication.timeSinceStartup - start) + "s");

        planetSettings.SyncSettings();
    }
    public void UpdateTerrain()
    {
        //only show the ocean if roughBed is false;
        //transform.GetChild(1).gameObject.SetActive(!planetValues.roughBed);

        craters = GetCrators(craterValues);
        //float start = (float)EditorApplication.timeSinceStartup;
        planetMesh.UpdateNoiseGPU();
        //Debug.Log("Noise generated in: " + ((float)EditorApplication.timeSinceStartup - start) + "s");
        colourGenerator.UpdateElevation(planetMesh.elevationData);
    }

    public Craters[] GetCrators(CraterValues craterValues)
    {
        Random.InitState(craterValues.seed);

        Craters[] craters = new Craters[craterValues.numCraters];
        for (int i = 0; i < craterValues.numCraters; i++)
        {
            craters[i] = new Craters();
            craters[i].floorHeight = Random.Range(-1f, 0);
            craters[i].radius = 0.1f + Random.Range(0f, 1) * Random.Range(0f, 1) * 0.1f;
            craters[i].point = Random.onUnitSphere;
        }
        return craters;
    }
    private void UpdateMaterials()
    {
        planetSettings.SyncValues();
        colourGenerator.UpdateColours();
        colourGenerator.UpdatePlanetInfo();

        planetSettings.SyncValues();

        atmosphere.ResetMaterial();


        if (miniMap != null)
            miniMap.UpdatePointers();
    }
    public GameObject GenerateMiniMap(GameObject _Map)
    {
        miniMap = new MiniMap(this, _Map);
        miniMap.CreatePlanetMap();

        return miniMap.Map;
    }


    public void Create(Vector3 position, Vector3 initialVelocity)
    {
        transform.position = position;
        GetComponent<Weight>().initialVelocity = initialVelocity;

        for (int i = 0; i < transform.GetChild(0).childCount; i++)
            transform.GetChild(0).GetChild(i).GetComponent<Segment>().RemoveFoilage();

        Init();

        UpdateTerrain();
        UpdateMaterials();
    }
    public void Create(Vector3 position, Vector3 initialVelocity, System.Random prng)
    {
        transform.position = position;
        GetComponent<Weight>().initialVelocity = initialVelocity;

        for (int i = 0; i < transform.GetChild(0).childCount; i++)
            transform.GetChild(0).GetChild(i).GetComponent<Segment>().RemoveFoilage();

        Init();
        planetSettings.RandomiseTerrainSeed(prng.Next(-9999, 9999));
        planetSettings.RandomiseColourSeed(prng.Next(-9999, 9999));
        planetSettings.RandomiseEnvironmentSeed(prng.Next(-9999, 9999));
        planetSettings.SyncSettings();

        UpdateTerrain();
        UpdateMaterials();
    }


    private void Update()
    {
        if (colourGenerator != null)
            colourGenerator.UpdatePlanetInfo();

        Vector3 viewPoint = Camera.main.WorldToViewportPoint(transform.position);

        //Show the planet segments if (player is not in hive) AND (player is close enough OR player is not quite close enough but player is looking at planet) AND (if robot is within a planet then this is that planet)
        transform.GetChild(0).gameObject.SetActive(
            !CameraState.inHive 
            &&
            (transform.position.sqrMagnitude < renderPlanetSegsBelowSqrDist ||
            transform.position.sqrMagnitude < dontRenderPlanetSegsAboveSqrDist &&
            viewPoint.x > -0.1f && viewPoint.x < 1.1f && viewPoint.y > -0.1f && viewPoint.y < 1.1f && viewPoint.z > 0) 
            &&
            (robotWeight.sigWeight == null || robotWeight.sigWeight.planet == this || 
            robotWeight.sigWeight.position.sqrMagnitude > robotWeight.sigWeight.planet.atmosphere.atmosRadius * robotWeight.sigWeight.planet.atmosphere.atmosRadius)
            );

        transform.GetChild(2).gameObject.SetActive(!CameraState.inHive);
        transform.GetChild(4).gameObject.SetActive(!CameraState.inHive);
        transform.GetChild(5).gameObject.SetActive(!CameraState.inHive);
    }
}
