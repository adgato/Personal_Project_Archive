using UnityEditor;
using UnityEngine;

public class MeshGen
{
    private Planet planet;

    private Mesh[,] faceSegs;
    private int numSegs;
    private Vector2Int[,] segCoords;

    public ElevationData elevationData;

    public MeshGen(Planet _planet)
    {
        elevationData = new ElevationData();
        planet = _planet;
        numSegs = 6 * planet.splitFaces * planet.splitFaces;
    }
    private void CalculateQuadSphere()
    {
        int resolution = planet.resolution;
        int splitFaces = planet.splitFaces;

        if (resolution % splitFaces != 0)
        {
            Debug.LogError("Error: Bad Split Faces number");
            return;
        }

        int gridsize = resolution / splitFaces;
        faceSegs = new Mesh[6, numSegs / 6];
        segCoords = new Vector2Int[6, numSegs / 6];

        //For each face on the quadsphere
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                //For each segment on the quasphere face
                for (int x = 0; x < resolution; x += gridsize)
                {
                    for (int y = 0; y < resolution; y += gridsize)
                    {
                        Vector2Int coord = new Vector2Int(x, y);

                        CalcSegMesh(coord, resolution, gridsize, i, j);

                        segCoords[i * 3 + j, (x / gridsize * resolution + y) / gridsize] = coord;
                    }
                }
            }
        }
    }
    public void RecalcSegMesh(int segIndex, int resolution, int prevRes, int splitFaces)
    {
        if (resolution % splitFaces != 0)
        {
            Debug.LogError("Error: Bad Split Faces number");
            return;
        }

        int gridsize = resolution / splitFaces;
        int prevGrid = prevRes / splitFaces;

        int faceNo = segIndex * 6 / faceSegs.Length;
        int i = faceNo / 3;
        int j = faceNo % 3;

        Vector2Int coord = segCoords[faceNo, segIndex % (faceSegs.Length / 6)];

        CalcSegMesh(coord, prevRes, prevGrid, resolution, gridsize, i, j);
    }
    private void CalcSegMesh(Vector2Int coord, int resolution, int gridsize, int i, int j)
    {
        CalcSegMesh(coord, resolution, gridsize, resolution, gridsize, i, j);
    }
    private void CalcSegMesh(Vector2Int coord, int resIndex, int gridIndex, int resolution, int gridsize, int i, int j)
    {
        Vector3[] start = new Vector3[] { -Vector3.one, Vector3.one };
        Vector3[] ends = new Vector3[] { Vector3.right, Vector3.up, Vector3.forward };

        Vector3 end = 2 * (ends[j] + (i == 0 ? ends[(j + 1) % 3] : Vector3.zero)) - Vector3.one;

        (Vector3[] verts, int[] tris, Vector2[] uvs) = DivideSeg(start[i], end, !(j == 0 || (j == 1 && i == 1)), resolution, gridsize, coord);

        faceSegs[i * 3 + j, (coord.x / gridIndex * resIndex + coord.y) / gridIndex] = NewMesh(verts, tris, uvs);
    }

    private (Vector3[], int[], Vector2[]) DivideSeg(Vector3 start, Vector3 end, bool flip, int resolution, int gridsize, Vector2Int coord)
    {
        //Determine the axis of the line segment
        int axisConst = start.x == end.x ? 0 : start.y == end.y ? 1 : 2;

        Vector3[] verts = new Vector3[(gridsize + 1) * (gridsize + 1)];
        Vector2[] uvs = new Vector2[verts.Length];

        //Iterate through all of the points in the grid to calculate the vertices and UV coordinates
        for (int x = coord.x; x < gridsize + coord.x + 1; x++)
        {
            float xPos = start.x + (end.x - start.x) * x / resolution;
            float zPos = start.z;
            if (axisConst == 0)
            {
                xPos = start.x;
                zPos = start.z + (end.z - start.z) * x / resolution;
            }
            for (int y = coord.y; y < gridsize + coord.y + 1; y++)
            {
                float yPos = start.y + (end.y - start.y) * y / resolution;
                if (axisConst == 1)
                {
                    yPos = start.y;
                    zPos = start.z + (end.z - start.z) * y / resolution;
                }
                //Concentrate verticies at the centre of the face so that they do not cluster at the corners when projected onto a sphere
                float xPosI = xPos * Mathf.Sqrt(1 - (yPos * yPos + zPos * zPos) / 2 + (yPos * yPos * zPos * zPos) / 3);
                float yPosI = yPos * Mathf.Sqrt(1 - (zPos * zPos + xPos * xPos) / 2 + (zPos * zPos * xPos * xPos) / 3);
                float zPosI = zPos * Mathf.Sqrt(1 - (xPos * xPos + yPos * yPos) / 2 + (xPos * xPos * yPos * yPos) / 3);
                verts[(x - coord.x) * (gridsize + 1) + (y - coord.y)] = new Vector3(xPosI, yPosI, zPosI).normalized;
                uvs[(x - coord.x) * (gridsize + 1) + (y - coord.y)] = new Vector2(axisConst == 0 ? zPos : xPos, (axisConst == 1 ? -zPos : yPos));
            }
        }
        int[] tris = new int[gridsize * gridsize * 6];
        //Iterate through each square in the grid and add its triangles to the triangle array
        for (int x = 0; x < gridsize; x++)
        {
            for (int y = 0; y < gridsize; y++)
            {
                int n = x * (gridsize + 1) + y;
                int[] order = new int[6] { n, n + 1, n + gridsize + 2, n, n + gridsize + 2, n + gridsize + 1 };

                //Flip the triangle order if necessary
                for (int i = 0; i < 6; i++)
                {
                    int index = flip ? 5 - i : i;
                    tris[(x * gridsize + y) * 6 + i] = order[index];
                }

            }
        }
        return (verts, tris, uvs);
    }

    public void ApplyQuadSphere()
    {
        //Already generated quad sphere and saved as prefabrication to load below
        /*
        string foldername = planet.splitFaces + "_" + planet.resolution + "_" + planet.gameObject.layer;
        string editor_foldername = "Assets/Planet Generation/Scripts/Creator/Resources/" + foldername;

        if (!AssetDatabase.IsValidFolder(editor_foldername))
        {
            GameObject _segs = ReplaceChild(planet.transform.GetChild(0).gameObject, new GameObject());

            Debug.Log("Creating new folder");

            AssetDatabase.CreateFolder("Assets/Planet Generation/Scripts/Creator/Resources", planet.splitFaces + "_" + planet.resolution);

            CalculateQuadSphere();
            int i = 0;
            foreach (Mesh _seg in faceSegs)
            {
                GameObject _child = new GameObject(i.ToString());

                _child.AddComponent<MeshRenderer>();
                _child.AddComponent<MeshCollider>();
                MeshFilter meshFilter = _child.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = _seg;

                _child.transform.position = planet.transform.position;
                _child.transform.parent = _segs.transform;

                Segment _childSeg = _child.AddComponent<Segment>();
                _childSeg.SegIndex = i;

                AssetDatabase.CreateAsset(_seg, editor_foldername + "/" + i + ".mesh");
                AssetDatabase.SaveAssets();

                i++;
            }
            PrefabUtility.SaveAsPrefabAsset(_segs, editor_foldername + "/Segment.prefab");
        }
        */
        
        GameObject segs = ReplaceChild(planet.transform.GetChild(0).gameObject, Object.Instantiate(planet.planetMeshSegmentPrefabs[planet.gameObject.layer - 7]));

        for (int i = 0; i < numSegs; i++)
        {
            GameObject child = segs.transform.GetChild(i).gameObject;
            child.layer = segs.layer;

            Mesh prefabMesh = child.GetComponent<MeshFilter>().sharedMesh;
            Mesh childMesh = prefabMesh;
            //childMesh.vertices = prefabMesh.vertices;
            //childMesh.triangles = prefabMesh.triangles;
            //childMesh.uv = prefabMesh.uv;

            Segment childSeg = child.GetComponent<Segment>();

            childSeg.planet = planet;
            childSeg.SetMesh(childMesh);
            childSeg.SetRenderMaterial(planet.terrainMat);
        }
    }

    public GameObject ReplaceChild(GameObject child, GameObject new_child)
    {
        new_child.name = child.name;
        new_child.transform.parent = child.transform.parent;
        new_child.layer = child.layer;
        new_child.transform.position = child.transform.position;
        new_child.transform.SetSiblingIndex(child.transform.GetSiblingIndex());
        if (Application.isEditor)
            Object.DestroyImmediate(child);
        else
            Object.Destroy(child);
        return new_child;
    }

    static Mesh NewMesh(Vector3[] verts, int[] tris, Vector2[] uvs)
    {
        Mesh mesh = new Mesh();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;
        return mesh;
    }

    public void UpdateNoiseGPU()
    {
        elevationData.Reset(planet.planetValues.radius);

        Vector3[] wOffsets = new Vector3[planet.warpNoise.octaves];
        Vector3[] cOffsets = new Vector3[planet.continentNoise.octaves];
        Vector3[] mOffsets = new Vector3[planet.mountainNoise.octaves + planet.mountainNoise.initialExtraOctaves];
        Vector3[] rOffsets = new Vector3[planet.roughNoise.octaves];

        System.Random wPrng = new System.Random(planet.warpNoise.seed);
        System.Random cPrng = new System.Random(planet.continentNoise.seed);
        System.Random mPrng = new System.Random(planet.mountainNoise.seed);
        System.Random rPrng = new System.Random(planet.roughNoise.seed);

        for (int i = 0; i < wOffsets.Length; i++)
        {
            float x = wPrng.Next(-1000, 1000);
            float y = wPrng.Next(-1000, 1000);
            float z = wPrng.Next(-1000, 1000);
            wOffsets[i] = new Vector3(x, y, z);
        }
        for (int i = 0; i < cOffsets.Length; i++)
        {
            float x = cPrng.Next(-1000, 10000);
            float y = cPrng.Next(-1000, 1000);
            float z = cPrng.Next(-1000, 1000);
            cOffsets[i] = new Vector3(x, y, z);
        }
        for (int i = 0; i < mOffsets.Length; i++)
        {
            float x = mPrng.Next(-1000, 1000);
            float y = mPrng.Next(-1000, 1000);
            float z = mPrng.Next(-1000, 1000);
            mOffsets[i] = new Vector3(x, y, z);
        }
        for (int i = 0; i < rOffsets.Length; i++)
        {
            float x = rPrng.Next(-1000, 1000);
            float y = rPrng.Next(-1000, 1000);
            float z = rPrng.Next(-1000, 1000);
            rOffsets[i] = new Vector3(x, y, z);
        }

        ComputeShader computeShader = planet.noiseShader;

        int handle = computeShader.FindKernel("GeneratePlanetNoise");

        computeShader.SetInt("numCraters", planet.craterValues.numCraters);
        computeShader.SetFloat("ctrSmoothness", planet.craterValues.smoothness);
        computeShader.SetFloat("ctrRimWidth", planet.craterValues.rimWidth);
        computeShader.SetFloat("ctrRimSteepness", planet.craterValues.rimSteepness);

        computeShader.SetInt("wOctaves", planet.warpNoise.octaves);
        computeShader.SetFloat("wScale", planet.warpNoise.scale);
        computeShader.SetFloat("wPersistance", planet.warpNoise.persistance);
        computeShader.SetFloat("wLacunarity", planet.warpNoise.lacunarity);

        computeShader.SetInt("cOctaves", planet.continentNoise.octaves);
        computeShader.SetFloat("cScale", planet.continentNoise.scale);
        computeShader.SetFloat("cPersistance", planet.continentNoise.persistance);
        computeShader.SetFloat("cLacunarity", planet.continentNoise.lacunarity);
        computeShader.SetFloat("cDropoff", planet.continentNoise.dropoff);

        computeShader.SetInt("mOctaves", planet.mountainNoise.octaves);
        computeShader.SetFloat("mScale", planet.mountainNoise.scale);
        computeShader.SetFloat("mPersistance", planet.mountainNoise.persistance);
        computeShader.SetFloat("mLacunarity", planet.mountainNoise.lacunarity);
        computeShader.SetFloat("mDropoff", planet.mountainNoise.dropoff);
        computeShader.SetInt("mInitialExtraOctaves", planet.mountainNoise.initialExtraOctaves);

        computeShader.SetInt("rOctaves", planet.roughNoise.octaves);
        computeShader.SetFloat("rPersistance", planet.roughNoise.persistance);
        computeShader.SetFloat("rLacunarity", planet.roughNoise.lacunarity);
        computeShader.SetFloat("rStartingOctave", planet.roughNoise.startingOctave);

        computeShader.SetFloat("groundLevel", planet.planetValues.groundLevel);
        computeShader.SetFloat("seabedLevel", planet.planetValues.seabedLevel);
        computeShader.SetFloat("radius", planet.planetValues.radius);
        computeShader.SetBool("roughBed", planet.planetValues.roughBed);
        computeShader.SetBool("crackedGround", planet.planetValues.crackedGround);

        ComputeBuffer craterBuffer = new ComputeBuffer(planet.craters.Length, sizeof(float) * 5);
        craterBuffer.SetData(planet.craters);
        computeShader.SetBuffer(handle, "craters", craterBuffer);

        ComputeBuffer warpBuffer = new ComputeBuffer(wOffsets.Length, sizeof(float) * 3);
        ComputeBuffer continentBuffer = new ComputeBuffer(cOffsets.Length, sizeof(float) * 3);
        ComputeBuffer mountainBuffer = new ComputeBuffer(mOffsets.Length, sizeof(float) * 3);
        ComputeBuffer roughBuffer = new ComputeBuffer(rOffsets.Length, sizeof(float) * 3);
        warpBuffer.SetData(wOffsets);
        continentBuffer.SetData(cOffsets);
        mountainBuffer.SetData(mOffsets);
        roughBuffer.SetData(rOffsets);
        computeShader.SetBuffer(handle, "wOffsets", warpBuffer);
        computeShader.SetBuffer(handle, "cOffsets", continentBuffer);
        computeShader.SetBuffer(handle, "mOffsets", mountainBuffer);
        computeShader.SetBuffer(handle, "rOffsets", roughBuffer);

        for (int i = 0; i < numSegs; i++)
        {
            Mesh seg = planet.transform.GetChild(0).GetChild(i).GetComponent<Segment>().mesh;
            if (seg == null)
                continue;

            Vector3[] verts = seg.vertices;

            computeShader.SetInt("vertexCount", seg.vertexCount);

            ComputeBuffer vertBuffer = new ComputeBuffer(seg.vertexCount, sizeof(float) * 3);
            vertBuffer.SetData(verts);
            computeShader.SetBuffer(handle, "rwVerts", vertBuffer);

            ComputeBuffer elevationBuffer = new ComputeBuffer(seg.vertexCount, sizeof(float));
            computeShader.SetBuffer(handle, "rwWorldHeight", elevationBuffer);

            computeShader.Dispatch(handle, Mathf.CeilToInt(seg.vertexCount / 256f), 1, 1);

            vertBuffer.GetData(verts);

            float[] elevations = new float[seg.vertexCount];
            elevationBuffer.GetData(elevations);

            int j = 0;
            foreach (float elevation in elevations)
            {
                elevationData.Add(elevation);
                j++;
            }
            elevationData.AddMap(verts);

            elevationBuffer.Dispose();
            vertBuffer.Dispose();
            seg.vertices = verts;

            planet.transform.GetChild(0).GetChild(i).GetComponent<Segment>().SetMesh(seg);
            planet.transform.GetChild(0).GetChild(i).GetComponent<Segment>().UpdateMeshFilter();
        }
        elevationData.CalcStats();

        craterBuffer.Dispose();
        warpBuffer.Dispose();
        continentBuffer.Dispose();
        mountainBuffer.Dispose();
        roughBuffer.Dispose();
    }
}
