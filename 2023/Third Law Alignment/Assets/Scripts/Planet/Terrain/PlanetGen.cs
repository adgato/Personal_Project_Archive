using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlanetGen : MonoBehaviour
{
    [SerializeField] private ComputeShader terrainNoise;
    [SerializeField] private ComputeShader marchingCubes;
    [SerializeField] private GameObject submeshPrefab;
    private ComputeBuffer voxels;

    [ReadOnly] [SerializeField] private int voxelsPerEdge = 512;
    [ReadOnly] [SerializeField] private int submeshesPerEdge = 8;
    [ReadOnly] [SerializeField] private float sampleRadius;
    [SerializeField] private float scale = 5;
    public float Radius => scale * sampleRadius;
    public float OceanRadius { get; private set; }
    [SerializeField] private float targetResolution = 0.4f;

    [Range(0, 1)]
    [SerializeField] private float isoValue = 0.5f;
    [SerializeField] private NoiseParamData[] noiseParams;

    [SerializeField] private int segmeshTriangleBudget = 65536 * 16 * 4 / 3;

    public PathMaker pathMaker = new PathMaker();
    private PlanetMeshBuilder meshBuilder;
    private MeshFilter meshFilter;
    private PlanetSubmesh[] Submeshes;
    private bool[] noMeshCollider;

    private bool initialised = false;

    private Rand.Seed seed;

    public void Initialise(Rand.Seed seed, float planetRadius)
    {
        if (meshBuilder != null)
            meshBuilder.Dispose();

        this.seed = seed;
        sampleRadius = planetRadius / scale;

        //based off of: planetRadius = 640, targetResolution = 0.4f => voxelsPerEdge = 512, submeshesPerEdge = 8
        voxelsPerEdge = Mathf.ClosestPowerOfTwo(Mathf.Clamp(32, 512, Mathf.RoundToInt(2 * targetResolution * planetRadius)));
        submeshesPerEdge = Mathf.Max(1, voxelsPerEdge / 32);

        meshFilter = GetComponent<MeshFilter>();

        meshBuilder = new PlanetMeshBuilder(voxelsPerEdge, submeshesPerEdge, segmeshTriangleBudget, isoValue, sampleRadius, scale, marchingCubes);
        meshFilter.sharedMesh = meshBuilder.mesh;

        Transform holder = gameObject.ReplaceChild(0, "Collision Meshes");

        Randomise();

        CalculateVoxels();

        Mesh[] collisionMeshes = meshBuilder.BuildCollisionMeshes();
        List<PlanetSubmesh> meshColliderList = new List<PlanetSubmesh>(); 
        noMeshCollider = new bool[collisionMeshes.Length];
        for (int i = 0; i < collisionMeshes.Length; i++)
        {
            noMeshCollider[i] = collisionMeshes[i] == null;
            if (noMeshCollider[i])
                continue;
            PlanetSubmesh meshCollider = Instantiate(submeshPrefab, holder).GetComponent<PlanetSubmesh>();
            meshCollider.Init(collisionMeshes[i], new Vector3Int(i / Mathx.Square(submeshesPerEdge), i / submeshesPerEdge % submeshesPerEdge, i % submeshesPerEdge), submeshesPerEdge);
            meshColliderList.Add(meshCollider);
        }
        Submeshes = meshColliderList.ToArray();

        InitialiseOnCollisionMesh();

        for (int i = 0; i < Submeshes.Length; i++)
            Submeshes[i].DisableCollider();

        UpdateTerrain(true);

        initialised = true;
    }

    private void OnDestroy()
    {
        if (initialised)
        {
            meshBuilder.Dispose();
            voxels.Dispose();
        }

    }

    private void OnValidate()
    {
        foreach (NoiseParamData noiseParamData in noiseParams)
            noiseParamData.SyncRemapData();
    }

    void FixedUpdate()
    {
        if (ControlSaver.GamePaused || !initialised)
            return;

        UpdateTerrain(false);
    }

    public PlanetSubmesh[] GetPlanetSubmeshes() => Submeshes;

    public void UpdateTerrain(bool forceUpdate)
    {
        if (voxels == null)
            CalculateVoxels();

        meshBuilder.UpdateIsosurface(PlayerRobotWeight.Player.baseTransform.position, transform.position, forceUpdate); //PlayerRobotWeight.Player.baseTransform.position
    }

    public void UpdateColliders(IEnumerable<ZeroWeight> collidingObjects)
    {
        meshBuilder.UpdateCollisionMeshesEnabled(collidingObjects, Submeshes, noMeshCollider, transform.position);
    }

    public void Randomise()
    {
        Rand rand = new Rand(seed);
        OceanRadius = rand.Range(10, Mathf.Max(10, Radius - 150), x => Mathf.Pow(x, 0.25f));

        //so, really, each noiseParam should have a different seed, so they intefere with each other in interesting ways,
        //however, i accidentally gave them all the same seed, and didn't realise that i had done so for a month working on the planets
        //this means i am quite happy with how the planet looks with each noiseParam having the same seed
        //characteristically, it makes the planet look a lot simpler, which has its pros and cons
        //so i have decided that whether the same seed is chosen or not is random :)

        // P(i == new) = p, i = [0, 1, 2], such that
        // P(all seeds unique) = P(all seeds same)
        // P(all seeds new) + P(exactly two seeds new) = P(all seeds same)
        // p^3 + 3p^2 * (1 - p) = (1 - p)^3
        // -2p^3 + 3p^2 = 1 - 3p + 3p^2 - p^3
        // p^3 - 3p + 1 = 0
        // p = 0.347... (and two other solutions that aren't probabilities)

        for (int i = 0; i < noiseParams.Length; i++)
            if (i != 0)
                noiseParams[i].Initialise(rand.Chance(0.347f) ? rand.PsuedoNewSeed() : seed);
    }

    private void CalculateVoxels()
    {
        if (voxels != null)
            voxels.Dispose();

        voxels = new ComputeBuffer(voxelsPerEdge * voxelsPerEdge * voxelsPerEdge, sizeof(float));


        using ComputeBuffer noiseParamBuffer = new ComputeBuffer(noiseParams.Length, Marshal.SizeOf(typeof(NoiseParams)));
        noiseParamBuffer.SetData(GetNoiseParams());

        terrainNoise.SetInt("VoxelsPerEdge", voxelsPerEdge);
        terrainNoise.SetFloat("SampleRadius", sampleRadius);
        terrainNoise.SetInt("NoiseParamCount", noiseParams.Length);
        terrainNoise.SetFloat("OceanRadius01", (OceanRadius - 30) / Radius);
        terrainNoise.SetBuffer(0, "VoxelBuffer", voxels);
        terrainNoise.SetBuffer(0, "ParamBuffer", noiseParamBuffer);

        terrainNoise.DispatchThreads(0, voxelsPerEdge, voxelsPerEdge, voxelsPerEdge);
        noiseParamBuffer.Dispose();
        meshBuilder.SetBuffers(voxels);
    }

    public NoiseParams[] GetNoiseParams()
    {
        NoiseParams[] noiseParamData = new NoiseParams[noiseParams.Length];
        for (int i = 0; i < noiseParams.Length; i++)
            noiseParamData[i] = noiseParams[i].noiseParams;
        return noiseParamData;
    }

    /// <summary>
    /// Called while all the mesh colliders of the planet are active. 
    /// </summary>
    private void InitialiseOnCollisionMesh()
    {
        pathMaker.MakePaths(meshBuilder.BuildSimple(8), transform.position, OceanRadius, seed);
    }
}
