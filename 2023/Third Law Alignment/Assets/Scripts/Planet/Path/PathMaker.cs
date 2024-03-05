using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class PathMaker
{
    [SerializeField] private float maxPathGradientAngle;
    [SerializeField] private int numPOIs;
    //[SerializeField] private int includeAllBelowLength;
    [SerializeField] private int maxPathLength;
    public Vector3[] PlanetPOIs { get; private set; }
    public Mesh[] PlanetPathMeshes { get; private set; }

    [SerializeField] private float rayCastHeight = 10;
    [SerializeField] private int resolution = 5;
    [SerializeField] private float pathInnerWidth = 1;
    [SerializeField] private float pathOuterWidth = 2;
    [SerializeField] private float pathHeight = 0.3f;

    private float oceanRadius;

    /// <summary>
    /// Neat wrapper for an integer list that represents a path.
    /// The path can also be reversed without reordering the list.
    /// </summary>
    private struct Path : System.IComparable
    {
        private List<int> nodes;
        private bool reversed;
        public int Length { get; private set; }
        public int Start { get; private set; }
        public int Destination { get; private set; }
        public bool Available { get; private set; }
        public void Reverse()
        {
            reversed ^= true;
            if (Available)
            {
                Start = nodes[reversed ? ^1 : 0];
                Destination = nodes[reversed ? 0 : ^1];
            }
        }
        public int GetNode(int pathIndex) => nodes[reversed ? ^(pathIndex + 1) : pathIndex];

        public Path(List<int> path)
        {
            nodes = path;
            Length = path.Count;
            Available = true;
            reversed = false;
            Start = nodes[0];
            Destination = nodes[^1];
        }

        public int CompareTo(object obj)
        {
            return Length - ((Path)obj).Length;
        }
    }

    private struct BezierPath 
    {
        Vector3[] P;
        Vector3[] H;
        float anchorWeight; 

        public BezierPath(Vector3[] keyPoints, float anchorWeight)
        {
            P = keyPoints;
            this.anchorWeight = anchorWeight;

            H = new Vector3[P.Length];
            for (int n = 0; n < H.Length; n++)
                H[n] = GetAnchor(n);
        }
        public Vector3 Evaluate(float t)
        {
            int n = Mathf.Clamp(Mathf.FloorToInt(t * (P.Length - 1)), 0, P.Length - 2);
            float t01 = t <= 0 ? 0 : t >= 1 ? 1 : Mathx.Frac(t * (P.Length - 1));
            return B(t01, n);
        }

        private Vector3 B(float t, int n)
        {
            float tt = t * t;
            float ttt = tt * t;
            float s = 1 - t;
            float ss = s * s;
            float sss = ss * s;

            return sss * P[n] + 3 * ss * t * (P[n] + H[n]) + 3 * s * tt * (P[n + 1] - H[n + 1]) + ttt * P[n + 1];
        }
        private Vector3 Bdir(float t, int n)
        {
            float tt = t * t;
            float s = 1 - t;
            float ss = s * s;

            return -ss * P[n] + s * (s - 2 * t) * (P[n] + H[n]) + t * (2 * s - t) * (P[n + 1] - H[n + 1]) + tt * P[n + 1];
        }

        private Vector3 GetAnchor(int n)
        {
            if (n == 0 || n == P.Length - 1)
                return Vector3.zero;
            Vector3 projection = Vector3.ProjectOnPlane(P[n + 1] - P[n], Normal(n));
            return projection.normalized * Mathf.Min(projection.sqrMagnitude, anchorWeight);
        }

        private Vector3 Normal(int n)
        {
            return ((P[n - 1] - P[n]).normalized + (P[n + 1] - P[n]).normalized).normalized;
        }

        public Mesh MakePath(Vector3 planetCentre, float rayCastHeight, int resolution, float pathInnerWidth, float pathOuterWidth, float pathHeight)
        {
            int splits = P.Length - 1;

            Vector3[] verts = new Vector3[4 * splits * resolution + 8];
            Vector3[] norms = new Vector3[4 * splits * resolution + 8];
            Vector2[] uvs = new Vector2[4 * splits * resolution + 8];

            int[] tris = new int[18 * splits * resolution + 24];

            //Debug
            Color col = Rand.stream.ColourHSV(0, 1, 1, 1, 1, 1);

            Vector3 pathDir = Vector3.forward;

            for (int n = 0; n < splits; n++)
            {
                Debug.DrawLine(B(0, n), B(1, n), col, 60);
                for (int r = 0; r < resolution + (n == splits - 1 ? 1 : 0); r++)
                {
                    float t = (float)r / resolution;
                    Vector3 pathCentre = B(t, n);
                    if (r == 0 && n == 0)
                        pathDir = Bdir(Mathf.Clamp(t, 0.01f, 0.99f), n).normalized;
                    else
                        pathDir = Vector3.RotateTowards(pathDir, Bdir(Mathf.Clamp(t, 0.01f, 0.99f), n).normalized, Mathf.PI / 4, 0);
                    Vector3 normalUp = pathCentre;
                    Vector3 pathCross = Vector3.Cross(normalUp, pathDir).normalized;

                    int i = 4 * (n * resolution + r);

                    verts[i + 0] = pathCentre - pathCross * pathOuterWidth;
                    verts[i + 2] = pathCentre + pathCross * pathOuterWidth;

                    for (int p = 0; p < 2; p++)
                    {
                        Vector3 vertNormalUp = verts[i + 2 * p].normalized;
                        if (Physics.Raycast(planetCentre + verts[i + 2 * p] + vertNormalUp * rayCastHeight, -vertNormalUp, out RaycastHit hitInfo, rayCastHeight * 2))
                            verts[i + 2 * p] = hitInfo.point - planetCentre;
                        else
                            verts[i + 2 * p] -= vertNormalUp * rayCastHeight;
                    }
                    pathCross = (verts[i + 2] - verts[i]).normalized;
                    pathCentre = (verts[i] + verts[i + 2]) * 0.5f;
                    normalUp = Vector3.Project(normalUp, Vector3.Cross(pathDir, pathCross)).normalized;

                    verts[i + 1] = pathCentre - pathCross * pathInnerWidth + normalUp * pathHeight;
                    verts[i + 3] = pathCentre + pathCross * pathInnerWidth + normalUp * pathHeight;

                    norms[i + 0] = normalUp;
                    norms[i + 2] = normalUp;
                    norms[i + 1] = normalUp;
                    norms[i + 3] = normalUp;

                    //to indicate where to colour the path
                    uvs[i + 1] = Vector2.right;
                    uvs[i + 3] = Vector2.right;

                    if (r == resolution)
                    {
                        verts[i + 4] = pathCentre + (pathDir - pathCross) * (pathOuterWidth - pathInnerWidth);
                        verts[i + 5] = pathCentre + (pathDir + pathCross) * (pathOuterWidth - pathInnerWidth);
                        norms[i + 4] = normalUp;
                        norms[i + 5] = normalUp;

                        int j = 18 * splits * resolution + 12;
                        tris[j] = i;
                        tris[j + 1] = i + 4;
                        tris[j + 2] = i + 1;

                        tris[j + 3] = i + 1;
                        tris[j + 4] = i + 4;
                        tris[j + 5] = i + 5;

                        tris[j + 6] = i + 1;
                        tris[j + 7] = i + 5;
                        tris[j + 8] = i + 3;

                        tris[j + 9] = i + 2;
                        tris[j + 10] = i + 3;
                        tris[j + 11] = i + 5;
                    }
                    else
                    {
                        int j = 18 * (n * resolution + r);
                        for (int o = 0; o < 2; o++)
                        {
                            int jo = j + 6 * o;
                            int io = i + 2 * o;

                            tris[jo] = io;
                            tris[jo + 1] = io + (o == 0 ? 5 : 1);
                            tris[jo + 2] = io + (o == 0 ? 1 : 5);

                            tris[jo + 3] = io;
                            tris[jo + 4] = io + (o == 0 ? 4 : 5);
                            tris[jo + 5] = io + (o == 0 ? 5 : 4);
                        }
                        tris[j + 12] = i + 1;
                        tris[j + 13] = i + 5;
                        tris[j + 14] = i + 7;

                        tris[j + 15] = i + 1;
                        tris[j + 16] = i + 7;
                        tris[j + 17] = i + 3;
                    }
                    if (n == 0 && r == 0)
                    {
                        i = 4 * splits * resolution + 6;
                        verts[i + 0] = pathCentre + (-pathDir - pathCross) * (pathOuterWidth - pathInnerWidth);
                        verts[i + 1] = pathCentre + (-pathDir + pathCross) * (pathOuterWidth - pathInnerWidth);
                        norms[i + 0] = normalUp;
                        norms[i + 1] = normalUp;

                        int j = 18 * splits * resolution;
                        tris[j] = i;
                        tris[j + 1] = 0;
                        tris[j + 2] = 1;

                        tris[j + 3] = i;
                        tris[j + 4] = 1;
                        tris[j + 5] = 3;

                        tris[j + 6] = i;
                        tris[j + 7] = 3;
                        tris[j + 8] = i + 1;

                        tris[j + 9] = i + 1;
                        tris[j + 10] = 3;
                        tris[j + 11] = 2;
                    }
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = verts;
            mesh.normals = norms;
            mesh.uv = uvs;
            mesh.triangles = tris;
            //mesh.UploadMeshData(true);
            return mesh;
        }
    }


    /// <summary>
    /// If the player cannot walk from one vertex to another exclude the vertex. 
    /// Note that since a low res mesh is being used, this may not be completely accurate.
    /// </summary>
    private bool ExcludeVertex(Vector3 vertex, Vector3 normal)
    {
        float dot = Vector3.Dot(vertex, normal);
        float vertexMagnitude = vertex.magnitude;
        return vertexMagnitude < oceanRadius || dot < 0 || Mathf.Acos(dot / vertexMagnitude) * Mathf.Rad2Deg > maxPathGradientAngle;
    }

    private HashSet<int> GetBlackList(Mesh setMesh)
    {
        HashSet<int> blackList = new HashSet<int>(setMesh.vertexCount);
        Vector3[] verts = setMesh.vertices;
        Vector3[] norms = setMesh.normals;
        for (int i = 0; i < setMesh.vertexCount; i++)
        {
            if (ExcludeVertex(verts[i], norms[i]))
                blackList.Add(i);
        }
        return blackList;
    }

    private List<int> GetPOIs(ref int count, int vertexCount, Rand.Seed seed, HashSet<int> blackList)
    {
        Rand rand = new Rand(seed);
        count = Mathf.Min(count, vertexCount - blackList.Count);

        List<int> POIs = new List<int>(count);

        int[] vertIdxs = Enumerable.Range(0, vertexCount).ToArray();
        rand.Shuffle(vertIdxs);

        foreach (int i in vertIdxs)
        {
            if (blackList.Contains(i))
                continue;
            POIs.Add(i);
            if (POIs.Count == count)
                break;
        }
        return POIs;
    }

    /// <param name="setMesh">It is assumed that each vertex of the mesh is unique.</param>
    private List<int>[] CreateAdjacencyList(Mesh setMesh, HashSet<int> blackList)
    {
        int[] tris = setMesh.triangles;

        //blacklisted verticies can not be traversed to in the adjacency list.
        List<int>[] adjacencyList = new List<int>[setMesh.vertexCount];
        for (int i = 0; i < adjacencyList.Length; i++)
            adjacencyList[i] = new List<int>(6); //can at most have 1 neighbour in each cardinal direction
        for (int i = 0; i < tris.Length; i += 3)
        {
            int idx0 = tris[i];
            int idx1 = tris[i + 1];
            int idx2 = tris[i + 2];
            if (!blackList.Contains(idx0))
            {
                adjacencyList[idx1].Add(idx0);
                adjacencyList[idx2].Add(idx0);
            }
            if (!blackList.Contains(idx1))
            {
                adjacencyList[idx0].Add(idx1);
                adjacencyList[idx2].Add(idx1);
            }
            if (!blackList.Contains(idx2))
            {
                adjacencyList[idx0].Add(idx2);
                adjacencyList[idx1].Add(idx2);
            }
        }
        return adjacencyList;
    }


    /// <summary>
    /// Performs a breadth first seach to find the shortest paths to all the destinations.
    /// </summary>
    private Path[] ShortestPaths(List<int>[] adjacencyList, int from, int toStartIndex, Dictionary<int, int> toReverseLookup)
    {
        int numNodes = adjacencyList.Length;

        Queue<int> queue = new Queue<int>();
        HashSet<int> visited = new HashSet<int>();
        int[] depth = new int[numNodes];
        int[] parent = new int[numNodes];
        System.Array.Fill(parent, -1); // Initialize parent array with -1 indicating no parent.

        queue.Enqueue(from);
        visited.Add(from);

        Path[] shortestPaths = new Path[toReverseLookup.Count];

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();

            if (depth[current] > maxPathLength)
                continue;

            if (toReverseLookup.ContainsKey(current))
            {
                int i = toReverseLookup[current] - toStartIndex;
                // Reconstruct the shortest path
                if (shortestPaths[i].Available)
                    Debug.LogWarning("Warning: destination has been visited twice");
                List<int> pathList = new List<int>();
                int node = current;
                while (node != from)
                {
                    pathList.Add(node);
                    node = parent[node];
                }
                pathList.Add(from);
                shortestPaths[i] = new Path(pathList);
                shortestPaths[i].Reverse(); //reverse because we added in reverse order.

                //Check if all paths have been set.
                bool allSet = true;
                foreach (Path path in shortestPaths)
                {
                    if (!path.Available)
                    {
                        allSet = false;
                        break;
                    }
                }
                if (allSet)
                    return shortestPaths;
            }

            foreach (int neighbor in adjacencyList[current])
            {
                if (!visited.Contains(neighbor))
                {
                    queue.Enqueue(neighbor);
                    visited.Add(neighbor);
                    parent[neighbor] = current;
                    depth[neighbor] = depth[current] + 1;
                }
            }
        }

        //Not all paths found
        return shortestPaths;
    }

    /// <summary>
    /// Generates a list of paths between points of interest (POI) on the planet.
    /// The paths include all of length smaller than both a specified length and the longest in the POI MST, plus the POI MST of the paths.
    /// </summary>
    /// <returns>The list of paths to be added to the planet.</returns>
    private void BuildPathNetwork(Mesh setMesh, int numPOIs, Rand.Seed seed, out List<Path> pathGraph, out List<int> POIs)
    {
        HashSet<int> blackList = GetBlackList(setMesh);

        List<int>[] adjacencyList = CreateAdjacencyList(setMesh, blackList);

        POIs = GetPOIs(ref numPOIs, adjacencyList.Length, seed, blackList);

        //make adjacency matrix between POIs.
        Path[][] adjacencyMatrixPOI = new Path[numPOIs][];
        for (int i = 0; i < numPOIs; i++)
            adjacencyMatrixPOI[i] = new Path[numPOIs];

        Dictionary<int, int> POIReverseLookup = new Dictionary<int, int>();
        for (int i = 0; i < numPOIs; i++)
            POIReverseLookup.Add(POIs[i], i);
        Dictionary<int, int> toReverseLookup = new Dictionary<int, int>(POIReverseLookup);

        for (int i = 0; i < numPOIs - 1; i++)
        {
            int offset = i + 1;
            toReverseLookup.Remove(POIs[i]);
            Path[] fromIto = ShortestPaths(adjacencyList, POIs[i], offset, toReverseLookup);
            for (int j = offset; j < numPOIs; j++)
            {
                adjacencyMatrixPOI[j][i] = adjacencyMatrixPOI[i][j] = fromIto[j - offset];
                adjacencyMatrixPOI[j][i].Reverse();
            }
        }

        pathGraph = new List<Path>();

        HashSet<int> visited = new HashSet<int>(numPOIs);
        SortedSet<Path> workingDestinations = new SortedSet<Path>();

        if (POIs.Count == 0)
            return;

        visited.Add(POIs[0]);
        int nextDestination = POIs[0];

        while (visited.Count < numPOIs)
        {
            foreach (Path path in adjacencyMatrixPOI[POIReverseLookup[nextDestination]])
            {
                if (path.Available)
                    workingDestinations.Add(path);
                //if (path.Length < includeAllBelowLength)
                //    pathGraph.Add(path);
            }
            workingDestinations.RemoveWhere(path => visited.Contains(path.Destination));

            if (workingDestinations.Count == 0)
                break;

            Path nextPath = workingDestinations.Min();
            nextDestination = nextPath.Destination;

            pathGraph.Add(nextPath);
            visited.Add(nextDestination);
        }
    }

    void CleanUpPaths(List<Path> pathNetwork, Vector3[] verts, out List<BezierPath> cleanPathNetwork)
    {
        HashSet<int> uniqueIndices = new HashSet<int>();
        cleanPathNetwork = new List<BezierPath>();

        float smoothness = 3;

        foreach (Path path in pathNetwork)
        {
            if (path.Length < 3)
                return;
            List<int> keyIndices = new List<int>(path.Length);
            int prevIdx = 0;
            int thisIdx = path.GetNode(0);
            int nextIdx = path.GetNode(1);
            keyIndices.Add(thisIdx);
            uniqueIndices.Add(thisIdx);
            for (int i = 1; i < path.Length - 1; i++)
            {
                prevIdx = thisIdx;
                thisIdx = nextIdx;
                nextIdx = path.GetNode(i + 1);

                //smooth out the path
                if (Vector3.Dot(verts[thisIdx] - verts[prevIdx], verts[nextIdx] - verts[thisIdx]) < 0 || (verts[nextIdx] - verts[thisIdx]).sqrMagnitude < 1)
                {
                    i++;
                    thisIdx = nextIdx;
                    nextIdx = path.GetNode(Mathf.Min(i + 1, path.Length - 1));
                }

                //cut the path wherever paths overlap (not cross)
                if (uniqueIndices.Contains(thisIdx) && uniqueIndices.Contains(nextIdx))
                {
                    if (nextIdx != thisIdx)
                        keyIndices.Add(nextIdx);
                    if (keyIndices.Count > 2)
                        cleanPathNetwork.Add(new BezierPath(keyIndices.Select(i => verts[i]).ToArray(), smoothness));
                    keyIndices.Clear();
                }
                else
                {
                    keyIndices.Add(thisIdx);
                    uniqueIndices.Add(thisIdx);
                }
            }
            if (nextIdx != thisIdx)
            {
                keyIndices.Add(nextIdx);
                uniqueIndices.Add(nextIdx);
            }
            if (keyIndices.Count > 2)
                cleanPathNetwork.Add(new BezierPath(keyIndices.Select(i => verts[i]).ToArray(), smoothness));
        }
    }

    public void MakePaths(Mesh setMesh, Vector3 planetCentre, float oceanRadius, Rand.Seed seed)
    {
        StopWatch.Start("NETWORK");

        this.oceanRadius = oceanRadius;

        BuildPathNetwork(setMesh, numPOIs, seed, out List<Path> pathNetwork, out List<int> POIidxs);


        Vector3[] verts = setMesh.vertices;

        CleanUpPaths(pathNetwork, verts, out List<BezierPath> cleanPathNetwork);

        PlanetPOIs = new Vector3[POIidxs.Count];
        for (int i = 0; i < POIidxs.Count; i++)
            PlanetPOIs[i] = verts[POIidxs[i]];

        PlanetPathMeshes = new Mesh[cleanPathNetwork.Count];
        for (int i = 0; i < cleanPathNetwork.Count; i++)
        {
            //GameObject test = new GameObject();
            //test.AddComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            //test.AddComponent<MeshFilter>().sharedMesh = test.AddComponent<MeshCollider>().sharedMesh = path.MakePath(planetCentre, 10, 5, 1, 2, 1);
            PlanetPathMeshes[i] = cleanPathNetwork[i].MakePath(planetCentre, rayCastHeight, resolution, pathInnerWidth, pathOuterWidth, pathHeight);
        }


        StopWatch.Stop("NETWORK");
    }
}
