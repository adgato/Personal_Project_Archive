using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniMap
{
    private Planet planet;
    private ElevationData elevationData;
    private Vector3[,,] faceHeightMaps;
    private Texture2D globeMap;
    public GameObject Map { get; private set; }

    public MiniMap(Planet _planet, GameObject _Map)
    {
        planet = _planet;
        ResetElevationData();

        Map = _Map;
        Mesh plane = Map.GetComponent<MeshFilter>().sharedMesh;
        Mesh mapMesh = new Mesh();
        mapMesh.vertices = plane.vertices;
        mapMesh.uv = plane.uv;
        mapMesh.triangles = plane.triangles;
        mapMesh.normals = plane.normals;
        Map.GetComponent<MeshFilter>().sharedMesh = mapMesh;
        Map.GetComponent<MeshRenderer>().sharedMaterial = new Material(Map.GetComponent<MeshRenderer>().sharedMaterial);
    }
    void ResetElevationData()
    {
        elevationData = new ElevationData();
        elevationData = planet.planetMesh.elevationData;
    }

    public void CreatePlanetMap()
    {
        //return; //Make work for GPU first
        CreateHeightMapCubeMesh();
        CreateGlobeMap();
    }
    void CreateHeightMapCubeMesh()
    {
        int segsPerFaceRow = planet.splitFaces;
        int segsPerFace = segsPerFaceRow * segsPerFaceRow;

        int vertsPerSegRow = planet.resolution / planet.splitFaces + 1;
        int vertsPerSeg = vertsPerSegRow * vertsPerSegRow;

        int vertsPerFaceRow = vertsPerSegRow * segsPerFaceRow;
        int vertsPerFace = vertsPerFaceRow * vertsPerFaceRow;

        faceHeightMaps = new Vector3[6, vertsPerFaceRow, vertsPerFaceRow];

        for (int faceNo = 0; faceNo < 6; faceNo++)
        {
            bool leftStart = faceNo == 0 || faceNo == 2 || faceNo == 5;
            bool bottomStart = faceNo == 0 || faceNo == 1;

            //Texture2D sets pixles bottom row (left to right) to top row. Therefore we need to convert the vertex arrangement of the face to this order
            //For face 0 this order is: left column (bottom to top) to right column
            //For face 1: right column (bottom to top)...

            int firstSeg = faceNo * segsPerFace;
            int bottomLeftSeg = 0;
            int bottomLeftVert = 0;

            if (leftStart && bottomStart)
            {
                bottomLeftSeg = firstSeg;
                bottomLeftVert = 0;
            }
            else if (leftStart && !bottomStart)
            {
                bottomLeftSeg = firstSeg + segsPerFaceRow - 1;
                bottomLeftVert = vertsPerSegRow - 1;
            }
            else if (!leftStart && bottomStart)
            {
                bottomLeftSeg = firstSeg + segsPerFace - segsPerFaceRow;
                bottomLeftVert = vertsPerSeg - vertsPerSegRow;
            }
            else if (!leftStart && !bottomStart)
            {
                bottomLeftSeg = firstSeg + segsPerFace - 1;
                bottomLeftVert = vertsPerSeg - 1;
            }

            for (int x = 0; x < vertsPerFaceRow; x++)
            {
                for (int y = 0; y < vertsPerFaceRow; y++)
                {
                    //Determine the segment index for the current vertex
                    int seg = bottomLeftSeg;
                    seg += x / vertsPerSegRow * segsPerFaceRow * (leftStart ? 1 : -1);
                    seg += y / vertsPerSegRow * (bottomStart ? 1 : -1);
                    Vector3[] verts = elevationData.GetMapTexture(seg);

                    //Determine the local x and y indices of the current vertex within its segment
                    int localX = x - x / vertsPerSegRow * vertsPerSegRow;
                    int localY = y - y / vertsPerSegRow * vertsPerSegRow;

                    int vert = bottomLeftVert;
                    vert += localX * vertsPerSegRow * (leftStart ? 1 : -1);
                    vert += localY * (bottomStart ? 1 : -1);

                    //Color colour = Color.Lerp(Color.black, Color.white, Mathf.InverseLerp(elevationData.Min, elevationData.Max, verts[vert].magnitude));

                    faceHeightMaps[faceNo, x, y] = verts[vert];
                }
            }
        }
    }
    void CreateGlobeMap()
    {
        List<Vector3> towerNormals = new List<Vector3>();
        foreach (TowerGen tower in planet.GetComponentsInChildren<TowerGen>())
            towerNormals.Add((tower.transform.position - planet.transform.position).normalized);
        Vector3 playerNormal = (Map.transform.position - planet.transform.position).normalized;

        int segsPerFaceRow = planet.splitFaces;

        int vertsPerSegRow = planet.resolution / planet.splitFaces + 1;

        int vertsPerFaceRow = vertsPerSegRow * segsPerFaceRow;

        int normalLength = (int)Mathf.Sqrt(Map.GetComponent<MeshFilter>().sharedMesh.vertexCount);
        int w = 800;
        int h = 400;

        globeMap = new Texture2D(w, h);

        Vector3[] vertNormal = new Vector3[Map.GetComponent<MeshFilter>().sharedMesh.vertexCount];

        //http://paulbourke.net/panorama/cubemaps/ (Converting to a spherical projection from 6 cubic environment maps, roughly a quarter down the page)
        for (int x = 0; x < w; x++)
        {
            float xNP = (float)x / w * 2 - 1;
            for (int y = 0; y < h; y++)
            {
                float yNP = (float)y / h * 2 - 1;

                //Calculate the position of the vertex at the given UV coordinates
                Vector3 pos = VertAtPolarCoord(xNP, yNP, vertsPerFaceRow);

                //Calculate the distance of the vertex from each tower and the player, and determine the minimum distance
                float distance = float.MaxValue;
                foreach (Vector3 normal in towerNormals)
                    distance = Mathf.Min(distance, Mathf.Max(Mathf.InverseLerp(0, 0.04f, (pos.normalized - normal).sqrMagnitude), 0.1f));
                distance = Mathf.Min(distance, Mathf.InverseLerp(0, 0.03f, (pos.normalized - playerNormal).sqrMagnitude));

                //The alpha channel stores the highlight splodges for each tower and player on the map
                globeMap.SetPixel(x, y, Color.Lerp(new Color(0,0,0, distance), new Color(1,1,1, distance), Mathf.InverseLerp(elevationData.Min, elevationData.Max, pos.magnitude)));
            }
        }
        //Store the normalized position of each vertex at the given UV coordinates (allows biomes to be coloured properly on map)
        for (int x = 0; x < normalLength; x++)
        {
            float xNP = (float)x / normalLength * 2 - 1;
            for (int y = 0; y < normalLength; y++)
            {
                float yNP = (float)y / normalLength * 2 - 1;

                Vector3 pos = VertAtPolarCoord(xNP, yNP, vertsPerFaceRow);

                vertNormal[y * normalLength + x] = pos;
            }
        }

        globeMap.Apply();


        Map.GetComponent<MeshFilter>().sharedMesh.normals = vertNormal;

        Map.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_map", globeMap);

        UpdatePointers();
    }
    public void UpdatePointers()
    {
        if (planet.planetValues.roughBed)
            Map.GetComponent<MeshRenderer>().sharedMaterial.EnableKeyword("_ROUGHBED");
        else
            Map.GetComponent<MeshRenderer>().sharedMaterial.DisableKeyword("_ROUGHBED");
        Map.GetComponent<MeshRenderer>().sharedMaterial.SetColor("_seaColour", planet.oceanMat.GetColor("_seaColour"));
        Map.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_planetTexture", planet.terrainMat.GetTexture("_planetTexture"));
    }

    int ClosestSeg()
    {
        (int index, float minDist) = (-1, float.MaxValue);
        for (int i = 0; i < planet.transform.GetChild(0).childCount; i++)
        {
            float thisCamDist = planet.transform.GetChild(0).GetChild(i).GetComponent<Segment>().camDist;
            if (thisCamDist < minDist)
                (index, minDist) = (i, thisCamDist);
        }
        return index;
    }

    Vector3 VertAtPolarCoord(float x, float y, int vertsPerFaceRow)
    {
        float s = x * Mathf.PI;
        float t = y * Mathf.PI / 2;

        //I created a Geogebra file to help me visualise converting a direction to a point on a 'skybox' https://www.geogebra.org/m/kwkbpd5k
        //The variable names correspond to the variable names in the file
        float a = 1 / (Mathf.Cos(t) * Mathf.Cos(s));
        float b = 1 / Mathf.Sin(t);
        float c = 1 / (Mathf.Cos(t) * Mathf.Sin(s));
        float m = Mathf.Min(Mathf.Abs(a), Mathf.Abs(b), Mathf.Abs(c));
        Vector3 P = new Vector3(1 / a, 1 / b, 1 / c);
        Vector3 M = m * P;
        Vector3 C = M * Vector3.Dot(M, P) / Mathf.Abs(Vector3.Dot(M, P));
        C = (C + Vector3.one) / 2;

        int faceNo = m == Mathf.Abs(a) ? (a > 0 ? 0 : 5) : m == Mathf.Abs(b) ? (b > 0 ? 4 : 2) : (c > 0 ? 3 : 1);
        Vector2 samplePoint = Vector2.zero;
        if (faceNo == 0)
            samplePoint = new Vector2(C.z, C.y);
        else if (faceNo == 1)
            samplePoint = new Vector2(C.x, C.y);
        else if (faceNo == 2)
            samplePoint = new Vector2(C.z, C.x);
        else if (faceNo == 3)
            samplePoint = new Vector2(1 - C.x, C.y);
        else if (faceNo == 4)
            samplePoint = new Vector2(C.z, 1 - C.x);
        else if (faceNo == 5)
            samplePoint = new Vector2(1 - C.z, C.y);

        float sampleX = samplePoint.x * (vertsPerFaceRow - 1);
        float sampleY = samplePoint.y * (vertsPerFaceRow - 1);

        Vector3 dd = faceHeightMaps[faceNo, Mathf.FloorToInt(sampleX), Mathf.FloorToInt(sampleY)];
        Vector3 du = faceHeightMaps[faceNo, Mathf.FloorToInt(sampleX), Mathf.CeilToInt(sampleY)];
        Vector3 ud = faceHeightMaps[faceNo, Mathf.CeilToInt(sampleX), Mathf.FloorToInt(sampleY)];
        Vector3 uu = faceHeightMaps[faceNo, Mathf.CeilToInt(sampleX), Mathf.CeilToInt(sampleY)];

        return Vector3.Lerp(Vector3.Lerp(dd, du, sampleY - Mathf.Floor(sampleY)), Vector3.Lerp(ud, uu, sampleY - Mathf.Floor(sampleY)), sampleX - Mathf.Floor(sampleX));
    }
}
