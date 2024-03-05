using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class Segment : MonoBehaviour
{
    public Planet planet;
    public Mesh mesh { get; private set; }
    public float camDist { get; private set; }
    public int SegIndex;
    private bool natureGenerated;
    private bool requestTower;
    private bool generatedTower = false;

    private int natureCount;
    private int treeCountPerSeg;
    private int oreCountPerSeg;
    private int grassCountPerSeg;
    private int stoneCountPerSeg;

    public static SunGenSystem sun;
    public static Stack<GameObject> instantiatedGrass;
    public static Stack<GameObject> instantiatedStone;
    private Dictionary<int, GameObject> segmentGrass = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> segmentStone = new Dictionary<int, GameObject>();
    private bool addedFoilage = false;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    public int currentRes;

    private System.Random nPrng;

    public void SetMesh(Mesh _mesh)
    {
        mesh = _mesh;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        //mesh.Optimize(); //Omitting saves lots of time when creating mesh, but mesh is supposedly then less optimised for rendering

    }
    public void UpdateMeshFilter()
    {
        if (meshFilter == null)
            GetComponents();
        meshFilter.sharedMesh = mesh;
    }
    private void UpdateMeshCollider()
    {
        if (meshCollider == null)
            GetComponents();
        meshCollider.sharedMesh = mesh;
    }
    public void SetRenderMaterial(Material _material)
    {
        if (meshRenderer == null)
            GetComponents();
        meshRenderer.sharedMaterial = _material;
    }
    public void SetRendererVisible(bool state)
    {
        if (meshRenderer == null)
            GetComponents();
        meshRenderer.enabled = state;
    }
    private void SetColliderActive(bool state)
    {
        if (meshCollider == null)
            GetComponents();
        meshCollider.enabled = state;
    }
    private void GetComponents()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
    }

    private bool ShowNature()
    {
        return IsCameraClose(); // && !CameraState.flyingShip
    }
    private bool IsCameraClose()
    {
        camDist = mesh.bounds.SqrDistance(Camera.main.transform.position - transform.position);
        return camDist < planet.maxNatureDist * planet.maxNatureDist;
    }
    private void AddNature()
    {
        float sqrRadius = planet.planetValues.radius * planet.planetValues.radius * 0.98f;

        //Want a mesh vertex to add nature on that is not at a steep gradient and is above the ocean
        int index;
        float surfaceAngle;
        int error = 0;
        do 
        {
            index = nPrng.Next(0, mesh.vertexCount);
            surfaceAngle = Vector3.Dot(mesh.normals[index], mesh.vertices[index].normalized);
            error++;
        } while (error < 100 && (surfaceAngle < 0.35f || mesh.vertices[index].sqrMagnitude < sqrRadius));

        if (natureCount < treeCountPerSeg)
        {
            GameObject _treeObj = Instantiate(planet.Tree, transform.position + mesh.vertices[index], transform.rotation * Quaternion.LookRotation(mesh.normals[index]), transform.GetChild(0));

            _treeObj.layer = gameObject.layer;

            TreeGen _tree = _treeObj.GetComponent<TreeGen>();
            _tree.planetNormal = mesh.normals[index];

            _tree.SetMaterials(planet, mesh.vertices[index]);
            _tree.Init(gameObject.layer, nPrng.Next(-9999, 9999));
        }
        else if (natureCount < treeCountPerSeg + oreCountPerSeg)
        {

            GameObject _oreObj = Instantiate(planet.Ore, transform.position + mesh.vertices[index], transform.rotation * Quaternion.LookRotation(mesh.normals[index]), transform.GetChild(0));

            _oreObj.layer = gameObject.layer;

            OreGen _ore = _oreObj.GetComponent<OreGen>();

            _ore.SetMaterials(planet, mesh.vertices[index]);
            _ore.Init(gameObject.layer, nPrng.Next(-9999, 9999));
        }

        natureCount++;
    }

    private void AddFoilage()
    {
        if (addedFoilage)
            return;

        //Need to do this each time instead of using the nPrng since foilage can be re-added
        Random.InitState(planet.planetValues.environmentSeed + SegIndex);

        for (int i = 0; i < grassCountPerSeg; i++)
        {
            if (instantiatedGrass.Count == 0)
                break;
            GameObject grass = instantiatedGrass.Pop();
            int index;
            if (segmentGrass.Count < grassCountPerSeg)
            {
                do index = Random.Range(0, mesh.vertices.Length);
                while (segmentGrass.ContainsKey(index));
                segmentGrass.Add(index, grass);
            }
            else
            {
                int[] indicies = new int[segmentGrass.Keys.Count];
                segmentGrass.Keys.CopyTo(indicies, 0);
                index = indicies[i];
                segmentGrass[index] = grass;
            }

            grass.SetActive(Vector3.Dot(mesh.vertices[index], mesh.normals[index]) > 0.7f);

            grass.transform.parent = transform.GetChild(0);
            grass.transform.position = transform.position + mesh.vertices[index];
            grass.transform.rotation = transform.rotation * Quaternion.LookRotation(mesh.normals[index]);
            grass.transform.localScale = new Vector3(2, 2, Random.Range(1f, 2));

            Material grassMat = grass.transform.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().sharedMaterial;
            grassMat.SetFloat("_minElevation", planet.planetMesh.elevationData.Min);
            grassMat.SetFloat("_maxElevation", planet.planetMesh.elevationData.Max);
            grassMat.SetTexture("_planetTexture", planet.terrainMat.GetTexture("_planetTexture"));
            grassMat.SetVector("_position", grass.transform.position - planet.transform.position);
        }

        for (int i = 0; i < stoneCountPerSeg; i++)
        {
            if (instantiatedStone.Count == 0)
                break;
            GameObject stone = instantiatedStone.Pop();
            int index;
            if (segmentStone.Count < stoneCountPerSeg)
            {
                do index = Random.Range(0, mesh.vertices.Length);
                while (segmentStone.ContainsKey(index));
                segmentStone.Add(index, stone);
            }
            else
            {
                int[] indicies = new int[segmentStone.Keys.Count];
                segmentStone.Keys.CopyTo(indicies, 0);
                index = indicies[i];
                segmentStone[index] = stone;
            }

            stone.SetActive(Vector3.Dot(mesh.vertices[index], mesh.normals[index]) > 0.7f);
            stone.transform.parent = transform.GetChild(0);
            stone.transform.position = transform.position + mesh.vertices[index];
            stone.transform.rotation = transform.rotation * Quaternion.LookRotation(mesh.normals[index]);
            stone.transform.localScale = Vector3.one * Random.Range(0.2f, 0.4f);

            for (int j = 0; j < stone.transform.childCount; j++)
                stone.transform.GetChild(j).gameObject.SetActive(Random.value < 0.5f);

            Material stoneMat = stone.transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial;
            stoneMat.SetFloat("_minElevation", planet.planetMesh.elevationData.Min);
            stoneMat.SetFloat("_maxElevation", planet.planetMesh.elevationData.Max);
            stoneMat.SetTexture("_planetTexture", planet.terrainMat.GetTexture("_planetTexture"));
            stoneMat.SetVector("_position", stone.transform.position - planet.transform.position);
        }

        addedFoilage = true;
    }

    public void RemoveFoilage()
    {
        if (!addedFoilage)
            return;

        foreach (int key in segmentGrass.Keys)
        {
            segmentGrass[key].SetActive(false);
            segmentGrass[key].transform.parent = sun.transform.GetChild(0).GetChild(0);
            instantiatedGrass.Push(segmentGrass[key]);
        }
        foreach (int key in segmentStone.Keys)
        {
            segmentStone[key].SetActive(false);
            segmentStone[key].transform.parent = sun.transform.GetChild(0).GetChild(1);
            instantiatedStone.Push(segmentStone[key]);
        }
        segmentGrass.Clear();
        segmentStone.Clear();

        addedFoilage = false;
    }

    private bool AddTower()
    {
        if (generatedTower)
            return false;

        SetColliderActive(true);

        Vector3 localUp = mesh.vertices[mesh.vertexCount / 2].normalized;
        Vector3 localRight = Vector3.Cross(localUp, Vector3.forward).normalized;
        Vector3 localForward = Vector3.Cross(localUp, localRight).normalized;

        if (!Physics.Raycast(localUp * planet.planetMesh.elevationData.Max + transform.position, -localUp, out RaycastHit originalHit))
            return true;

        float maxHeight = planet.planetMesh.elevationData.Max - originalHit.distance;

        float square = 12;
        float steps = 3;

        //Check if terrain surrounding selected tower position is flat enough for the tower, return false if not
        for (float x = -square; x <= square; x += 2 * square / (steps - 1))
        {
            for (float z = -square; z <= square; z += 2 * square / (steps - 1))
            {
                Vector3 offset = localRight * x + localForward * z;

                Vector3 rayCastPos = localUp * planet.planetMesh.elevationData.Max + transform.position + offset;

                if (Physics.Raycast(rayCastPos, -localUp, out RaycastHit cornerHit))
                {
                    float newHeight = planet.planetMesh.elevationData.Max - cornerHit.distance;

                    if (newHeight > maxHeight)
                        maxHeight = newHeight;

                    if (Vector3.Dot(localUp, cornerHit.normal) < 0.7f)
                        return false;
                }
            }
        }
        //Add tower

        Vector3 towerPos = localUp * maxHeight + transform.position;

        GameObject _towerObj = Instantiate(planet.Tower, towerPos, Quaternion.identity, transform);

        generatedTower = true;

        _towerObj.layer = gameObject.layer;

        TowerGen _tower = _towerObj.GetComponent<TowerGen>();
        _tower.normalUp = localUp;
        _tower.Init(nPrng.Next(-9999, 9999));

        SetColliderActive(false);

        return false;
    }

    public void Start()
    {
        nPrng = new System.Random(planet.planetValues.environmentSeed + SegIndex);
        natureGenerated = false;
        natureCount = 0;

        float scaleFactor = Mathf.Pow(planet.planetValues.radius, 2) / 250000;

        float probability = scaleFactor / 144f;
        requestTower = nPrng.NextDouble() < probability;

        treeCountPerSeg = Mathf.RoundToInt(Mathf.Lerp(planet.treeCountPer500Seg.x, planet.treeCountPer500Seg.y, (float)nPrng.NextDouble() * scaleFactor));
        oreCountPerSeg = Mathf.RoundToInt(Mathf.Lerp(planet.oreCountPer500Seg.x, planet.oreCountPer500Seg.y, (float)nPrng.NextDouble() * scaleFactor));
        grassCountPerSeg = Mathf.RoundToInt(Mathf.Lerp(planet.grassCountPer500Seg.x, planet.grassCountPer500Seg.y, (float)nPrng.NextDouble() * scaleFactor));
        stoneCountPerSeg = Mathf.RoundToInt(Mathf.Lerp(planet.stoneCountPer500Seg.x, planet.stoneCountPer500Seg.y, (float)nPrng.NextDouble() * scaleFactor));

        GameObject natureHolder = new GameObject("Nature Holder (0)");
        natureHolder.layer = gameObject.layer;
        natureHolder.transform.parent = transform;

        SetColliderActive(false);

        UpdateMeshCollider();
    }
    public void Update()
    {
        if (!requestTower)
            SetColliderActive(IsCameraClose());
        if (requestTower)
            requestTower = AddTower();

        //Rather than creating all the new nature objects within 1 frame, it is much smoother to create each on a seperate frame
        else if (natureCount > 0 && natureCount < oreCountPerSeg + treeCountPerSeg && Time.frameCount % 4 == 0)
            AddNature();
        else if (ShowNature())
        {
            if (natureCount == 0)
            {
                UpdateMeshCollider();
                AddNature();
            }
            if (!natureGenerated)
            {
                natureGenerated = true;
                transform.GetChild(0).gameObject.SetActive(true);
            }

            AddFoilage();
        }
        else
        {
            if (natureGenerated)
            {
                natureGenerated = false;
                transform.GetChild(0).gameObject.SetActive(false);
            }

            RemoveFoilage();
        }
    }

    //Generate 500 instances of grass and stone on Start to improve performance
    public static void InstantiateFoilage(SunGenSystem system)
    {
        sun = system;

        int grassCount = 500;
        int stoneCount = 500;

        instantiatedGrass = new Stack<GameObject>(grassCount);
        for (int i = 0; i < grassCount; i++)
        {
            GameObject grass;
            if (i < sun.transform.GetChild(0).GetChild(0).childCount)
                grass = sun.transform.GetChild(0).GetChild(0).GetChild(i).gameObject;
            else
                grass = Instantiate(sun.Grass, sun.transform.GetChild(0).GetChild(0));

            grass.transform.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = new Material(grass.transform.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().sharedMaterial);
            grass.transform.GetChild(0).GetChild(0).gameObject.layer = 6;
            for (int j = 1; j < grass.transform.childCount; j++)
            {
                grass.transform.GetChild(j).GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = grass.transform.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().sharedMaterial;
                grass.transform.GetChild(j).GetChild(0).gameObject.layer = 6;
            }
            grass.SetActive(false);

            instantiatedGrass.Push(grass);
        }

        instantiatedStone = new Stack<GameObject>(stoneCount);
        for (int i = 0; i < stoneCount; i++)
        {
            GameObject stone;
            if (i < sun.transform.GetChild(0).GetChild(1).childCount)
                stone = sun.transform.GetChild(0).GetChild(1).GetChild(i).gameObject;
            else
                stone = Instantiate(sun.Stone, sun.transform.GetChild(0).GetChild(1));

            stone.transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = new Material(stone.transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial);
            stone.transform.GetChild(0).gameObject.layer = 6;
            for (int j = 1; j < stone.transform.childCount; j++)
            {
                stone.transform.GetChild(j).GetComponent<MeshRenderer>().sharedMaterial = stone.transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial;
                stone.transform.GetChild(j).gameObject.layer = 6;
            }
            stone.SetActive(false);

            instantiatedStone.Push(stone);
        }
    }
}
