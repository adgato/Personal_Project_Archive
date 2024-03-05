using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

//
// Isosurface mesh builder with the marching cubes algorithm
//
public sealed class PlanetMeshBuilder : System.IDisposable
{
    public Mesh mesh { get; private set; }
    private Mesh simpleMesh;

    GraphicsBuffer vertexBuffer;
    GraphicsBuffer indexBuffer;
    ComputeBuffer voxels;

    ComputeBuffer triangleTable;
    ComputeBuffer counterBuffer;

    readonly int voxelsPerEdge;
    readonly int submeshesPerEdge;
    int triangleBudget;
    readonly int dims;

    float target;
    float sampleRadius;
    float scale;

    Vector3Int cameraSubmesh;
    int[] submeshLODs;
    int bestLOD;

    ComputeShader compute;

    public PlanetMeshBuilder(int voxelsPerEdge, int submeshesPerEdge, int triangleBudget, float target, float sampleRadius, float scale, ComputeShader compute)
    {
        this.voxelsPerEdge = voxelsPerEdge;
        this.submeshesPerEdge = submeshesPerEdge;
        this.triangleBudget = triangleBudget;
        this.compute = compute;
        this.target = target;
        this.sampleRadius = sampleRadius;
        this.scale = scale;
        dims = voxelsPerEdge / submeshesPerEdge;

        AllocateBuffers();
        mesh = new Mesh();
        simpleMesh = null;

        AllocateMesh(3 * triangleBudget);
    }

    public void SetBuffers(ComputeBuffer voxels)
    {
        this.voxels = voxels;
        submeshLODs = new int[submeshesPerEdge * submeshesPerEdge * submeshesPerEdge];
    }

    public Mesh[] BuildCollisionMeshes()
    {
        Mesh[] collisionMeshes = new Mesh[submeshesPerEdge * submeshesPerEdge * submeshesPerEdge];

        compute.SetBuffer(0, "TriangleTable", triangleTable);
        compute.SetBuffer(0, "Counter", counterBuffer);
        compute.SetBuffer(0, "Voxels", voxels);
        compute.SetBuffer(0, "VertexBuffer", vertexBuffer);
        compute.SetBuffer(0, "IndexBuffer", indexBuffer);

        compute.SetInt("VoxelsPerEdge", voxelsPerEdge);
        compute.SetInt("MaxTriangle", triangleBudget);
        compute.SetFloat("Isovalue", target);
        compute.SetFloat("SampleRadius", sampleRadius);
        compute.SetFloat("ScaleRadius", scale);

        for (int x = 0; x < submeshesPerEdge; x++)
            for (int y = 0; y < submeshesPerEdge; y++)
                for (int z = 0; z < submeshesPerEdge; z++)
                {
                    counterBuffer.SetCounterValue(0);
                    UpdateSubmesh(x, y, z, 1, true);
                    BuildCollisionMesh(ref collisionMeshes[z + submeshesPerEdge * (y + submeshesPerEdge * x)]);
                }

        return collisionMeshes;
    }

    //this is called after all zeroweights have moved with their respective planet and before they move relative
    /// <returns>True / false depending on whether anything was updated.</returns>
    public bool UpdateCollisionMeshesEnabled(IEnumerable<ZeroWeight> collidingObjects, PlanetSubmesh[] submeshes, bool[] nullMeshes, Vector3 planetPos)
    {
        Vector3Int[] collidingSubmesh = collidingObjects.Select(x => Vector3x.FloorToInt(WorldToGridPos(x.Position, planetPos))).ToArray();
        Vector3Int playerSubmesh = Vector3x.FloorToInt(WorldToGridPos(PlayerRobotWeight.Player.Position, planetPos));

        int i = 0;
        bool anyChange = false;
        for (int x = 0; x < submeshesPerEdge; x++)
            for (int y = 0; y < submeshesPerEdge; y++)
                for (int z = 0; z < submeshesPerEdge; z++)
                    if (!nullMeshes[z + submeshesPerEdge * (y + submeshesPerEdge * x)])
                        anyChange |= submeshes[i++].UpdatePlayerDist(collidingSubmesh, playerSubmesh);
        return anyChange;
    }

    /// <returns>True / false depending on whether anything was updated.</returns>
    public bool UpdateIsosurface(Vector3 cameraPos, Vector3 planetCentre, bool forceUpdate)
    {

        Vector3 cameraGridPos = WorldToGridPos(cameraPos, planetCentre);
        Vector3Int cameraSubmesh = Vector3x.FloorToInt(cameraGridPos);

        if (cameraSubmesh == this.cameraSubmesh && !forceUpdate)
            return false;

        this.cameraSubmesh = cameraSubmesh;

        int worstLOD = Vector3x.Any(cameraGridPos < (Vector3x)Vector3.zero) || Vector3x.Any(cameraGridPos > (Vector3x)(Vector3.one * submeshesPerEdge)) ? (sampleRadius > 128 ? 4 : 2) : 16;
        bestLOD = worstLOD;

        compute.SetBuffer(0, "TriangleTable", triangleTable);
        compute.SetBuffer(0, "Counter", counterBuffer);
        compute.SetBuffer(0, "Voxels", voxels);
        compute.SetBuffer(0, "VertexBuffer", vertexBuffer);
        compute.SetBuffer(0, "IndexBuffer", indexBuffer);

        compute.SetInt("VoxelsPerEdge", voxelsPerEdge);
        compute.SetInt("MaxTriangle", triangleBudget);
        compute.SetFloat("Isovalue", target);
        compute.SetFloat("SampleRadius", sampleRadius);
        compute.SetFloat("ScaleRadius", scale);

        bool anyChange = false;

        for (int x = 0; x < submeshesPerEdge; x++)
        {
            int dx = Mathf.Abs(x - cameraSubmesh.x);
            for (int y = 0; y < submeshesPerEdge; y++)
            {
                int dy = Mathf.Abs(y - cameraSubmesh.y);
                for (int z = 0; z < submeshesPerEdge; z++)
                {
                    int maxDiff = Mathf.Max(dx, dy, Mathf.Abs(z - cameraSubmesh.z));

                    int i = x + submeshesPerEdge * (y + submeshesPerEdge * z);
                    int lod = Mathf.Min(worstLOD, maxDiff < 4 ? 1 : maxDiff < 8 ? 2 : 16);
                    bestLOD = Mathf.Min(bestLOD, lod);

                    if (lod == submeshLODs[i])
                        continue;

                    anyChange = true;
                    submeshLODs[i] = lod;
                }
            }
        }

        if (!anyChange && !forceUpdate)
            return false;

        counterBuffer.SetCounterValue(0);
        for (int x = 0; x < submeshesPerEdge; x++)
            for (int y = 0; y < submeshesPerEdge; y++)
                for (int z = 0; z < submeshesPerEdge; z++)
                    UpdateSubmesh(x, y, z, submeshLODs[x + submeshesPerEdge * (y + submeshesPerEdge * z)], false);

        if (counterBuffer.GetCounterValue() >= triangleBudget)
        {
            Debug.LogError("Error: triangle budget has been reached, increasing triangle budget by 1.25x for this planet");
            triangleBudget = triangleBudget * 4 / 3;
            ReleaseMesh();
            AllocateMesh(3 * triangleBudget);
            return UpdateIsosurface(cameraPos, planetCentre, true);
        }

        // Clear unused area of the buffers.
        compute.SetBuffer(1, "Counter", counterBuffer);
        compute.SetBuffer(1, "VertexBuffer", vertexBuffer);
        compute.SetBuffer(1, "IndexBuffer", indexBuffer);

        compute.DispatchThreads(1, 1024, 1, 1);

        // Bounding box
        mesh.bounds = new Bounds(Vector3.zero, 2 * sampleRadius * scale * Vector3.one);
        return true;
    }

    void UpdateSubmesh(int x, int y, int z, int lod, bool noPadding)
    {
        if (lod == bestLOD || noPadding)
        {
            int dimsLOD = dims / lod;
            compute.SetInts("GridOffset", dims * x, dims * y, dims * z);
            compute.SetInt("VoxelGridWidth", lod);
            compute.SetInts("EndID", dimsLOD, dimsLOD, dimsLOD);
            compute.DispatchThreads(0, dimsLOD, dimsLOD, dimsLOD);
            return;
        }

        //Add padding for adjacent submeshes of different LOD so that they overlap slightly reducing gaps in mesh

        int lPadding = (lod < submeshLODs[Mathf.Clamp(x - 1 + submeshesPerEdge * (y + submeshesPerEdge * z), 0, submeshLODs.Length - 1)]).ToInt();
        int rPadding = (lod < submeshLODs[Mathf.Clamp(x + 1 + submeshesPerEdge * (y + submeshesPerEdge * z), 0, submeshLODs.Length - 1)]).ToInt();
        int uPadding = (lod < submeshLODs[Mathf.Clamp(x + submeshesPerEdge * (y + 1 + submeshesPerEdge * z), 0, submeshLODs.Length - 1)]).ToInt();
        int dPadding = (lod < submeshLODs[Mathf.Clamp(x + submeshesPerEdge * (y - 1 + submeshesPerEdge * z), 0, submeshLODs.Length - 1)]).ToInt();
        int fPadding = (lod < submeshLODs[Mathf.Clamp(x + submeshesPerEdge * (y + submeshesPerEdge * (z + 1)), 0, submeshLODs.Length - 1)]).ToInt();
        int bPadding = (lod < submeshLODs[Mathf.Clamp(x + submeshesPerEdge * (y + submeshesPerEdge * (z - 1)), 0, submeshLODs.Length - 1)]).ToInt();

        int dimX = dims / lod + rPadding + lPadding;
        int dimY = dims / lod + uPadding + dPadding;
        int dimZ = dims / lod + fPadding + bPadding;

        compute.SetInts("GridOffset", dims * x - lod * lPadding, dims * y - lod * dPadding, dims * z - lod * bPadding);
        compute.SetInt("VoxelGridWidth", lod);
        compute.SetInts("EndID", dimX, dimY, dimZ);
        compute.DispatchThreads(0, dimX, dimY, dimZ);
    }

    /// <summary>
    /// Note: this will override the current mesh, so should be called before this is generated.
    /// </summary>
    /// <returns>A low res mesh of the entire planet, with no duplicate verticies</returns>
    public Mesh BuildSimple(int lod)
    {
        if (simpleMesh != null)
            return simpleMesh;

        StopWatch.Start("MESH_MAKE");

        simpleMesh = new Mesh();

        compute.SetBuffer(0, "TriangleTable", triangleTable);
        compute.SetBuffer(0, "Counter", counterBuffer);
        compute.SetBuffer(0, "Voxels", voxels);
        compute.SetBuffer(0, "VertexBuffer", vertexBuffer);
        compute.SetBuffer(0, "IndexBuffer", indexBuffer);

        compute.SetInt("VoxelsPerEdge", voxelsPerEdge);
        compute.SetInt("MaxTriangle", triangleBudget);
        compute.SetFloat("Isovalue", target);
        compute.SetFloat("SampleRadius", sampleRadius);
        compute.SetFloat("ScaleRadius", scale);

        counterBuffer.SetCounterValue(0);

        for (int x = 0; x < submeshesPerEdge; x++)
            for (int y = 0; y < submeshesPerEdge; y++)
                for (int z = 0; z < submeshesPerEdge; z++)
                    UpdateSubmesh(x, y, z, lod, true);

        int indexCount = 3 * counterBuffer.GetCounterValue();

        Vector3[] vectors = new Vector3[indexCount * 2];

        int[] tris = new int[indexCount];

        vertexBuffer.GetData(vectors, 0, 0, indexCount * 2);

        Dictionary<Vector3, int> vertsReverseLookup = new Dictionary<Vector3, int>(indexCount);
        List<Vector3> norms = new List<Vector3>(indexCount);

        int i = 0;
        for (int v = 0; v < indexCount; v++)
        {
            Vector3 vert = vectors[2 * v];
            if (!vertsReverseLookup.ContainsKey(vert))
            {
                vertsReverseLookup.Add(vert, i);
                norms.Add(vectors[2 * v + 1]);
                tris[v] = i;
                i++;
            }
            else
                tris[v] = vertsReverseLookup[vert];
        }
        simpleMesh.vertices = vertsReverseLookup.Keys.ToArray();
        simpleMesh.normals = norms.ToArray();
        simpleMesh.triangles = tris;

        StopWatch.Stop("MESH_MAKE");
        return simpleMesh;
    }

    void BuildCollisionMesh(ref Mesh collisionMesh)
    {
        int vertexCount = 3 * counterBuffer.GetCounterValue();

        if (vertexCount == 0)
            return;

        Vector3[] vectors = new Vector3[vertexCount * 2];
        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        int[] triangles = new int[vertexCount];

        vertexBuffer.GetData(vectors, 0, 0, vertexCount * 2);


        //order the verticies array so that it is mostly determinisitic
        {
            //large enough number
            float size = 2 * sampleRadius * scale / submeshesPerEdge;

            int[] order = new int[vertexCount / 3];
            for (int i = 0; i < vertexCount / 3; i++)
                order[i] = Mathf.FloorToInt(vectors[6 * i].x + vectors[6 * i].y * size + vectors[6 * i].z * Mathx.Square(size));

            int[] index = Enumerable.Range(0, vertexCount / 3).OrderBy(i => order[i]).ToArray();

            for (int i = 0; i < vertexCount / 3; i++)
            {
                int I = 3 * i;
                int ordered_index = 6 * index[i];

                vertices[I + 0] = vectors[ordered_index];
                vertices[I + 1] = vectors[ordered_index + 2];
                vertices[I + 2] = vectors[ordered_index + 4];
                normals[I + 0] = vectors[ordered_index + 1];
                normals[I + 1] = vectors[ordered_index + 3];
                normals[I + 2] = vectors[ordered_index + 5];
                triangles[I + 0] = I;
                triangles[I + 1] = I + 1;
                triangles[I + 2] = I + 2;
            }
        }

        /* unordered but faster
        for (int i = 0; i < vertexCount; i++)
        {
            vertices[i] = vectors[2 * i];
            normals[i] = vectors[2 * i + 1];
            triangles[i] = i;
        }
        */

        collisionMesh = new Mesh();
        collisionMesh.vertices = vertices;
        collisionMesh.normals = normals;
        collisionMesh.triangles = triangles;
        //collisionMesh.UploadMeshData(true); //unfortunately, need access to mesh data for foilage generation
    }

    Vector3 WorldToGridPos(Vector3 worldPos, Vector3 planetCentre) => 0.5f * submeshesPerEdge * ((worldPos - planetCentre) / (sampleRadius * scale) + Vector3.one);

    public void Dispose()
    {
        ReleaseBuffers();
        ReleaseMesh();
        Object.Destroy(mesh);
        Object.Destroy(simpleMesh);
        mesh = null;
    }

    void AllocateBuffers()
    {
        // Marching cubes triangle table
        triangleTable = new ComputeBuffer(256, sizeof(ulong));
        triangleTable.SetData(PrecalculatedData.TriangleTable);

        // Buffer for triangle counting
        counterBuffer = new ComputeBuffer(1, 4, ComputeBufferType.Counter);
    }

    void ReleaseBuffers()
    {
        triangleTable.Dispose();
        counterBuffer.Dispose();
    }

    void AllocateMesh(int vertexCount)
    {
        mesh.Clear();

        // We want GraphicsBuffer access as Raw (ByteAddress) buffers.
        mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;

        // Vertex position: float32 x 3
        var vp = new VertexAttributeDescriptor
            (VertexAttribute.Position, VertexAttributeFormat.Float32, 3);

        // Vertex normal: float32 x 3
        var vn = new VertexAttributeDescriptor
            (VertexAttribute.Normal, VertexAttributeFormat.Float32, 3);

        // Vertex/index buffer formats
        mesh.SetVertexBufferParams(vertexCount, vp, vn);
        mesh.SetIndexBufferParams(vertexCount, IndexFormat.UInt32);

        // Submesh initialization
        mesh.SetSubMesh(0, new SubMeshDescriptor(0, vertexCount),
                            MeshUpdateFlags.DontRecalculateBounds);

        // GraphicsBuffer references
        vertexBuffer = mesh.GetVertexBuffer(0);
        indexBuffer = mesh.GetIndexBuffer();
    }

    void ReleaseMesh()
    {
        vertexBuffer.Dispose();
        indexBuffer.Dispose();
    }
}

