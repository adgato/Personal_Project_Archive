using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

public class GrassField : MonoBehaviour
{
    [System.Serializable]
    private struct GrassLOD
    {
        public int lodGridDist;
        [Min(1)]
        public int grassRes;
    }

    [SerializeField] private ComputeShader GrassBuilder;

    GraphicsBuffer vertexBuffer;
    GraphicsBuffer indexBuffer;
    MeshFilter meshFilter;
    MeshRenderer meshRenderer;

    [SerializeField] private Rand.Seed seed;
    [SerializeField] private int lodGridDist;
    [SerializeField] private int keepMeshDataGridDist;
    [Min(1)]
    [SerializeField] private int grassRes;
    [SerializeField] private PlanetSubmesh Field;
    private static Mesh GrassMeshLOD;

    [SerializeField] private float bladeWidth = 0.5f;
    [SerializeField] private float bladeWidthDropoff = 2;

    [SerializeField] private int maxCPUVertexCount = 1000;
    [SerializeField] private int maxKeepLoadedVertexCount = 10000;

    [SerializeField] private int maxGrassPerTriangle;
    [SerializeField] private int grassPerUnitArea;

    private float dotCutoff;
    private float oceanRadius;

    [SerializeField] private float updateRate = 1;
    private Coroutine UpdateCoroutine;

    private Rand rand;

    /// <summary>
    /// Pros: fastest. Cons: produces largest mesh.
    /// </summary>
    private void CreateGrassFieldGPU()
    {
        rand = new Rand(seed);

        Vector3[] verts = Field.Surface.vertices; //this makes accessing the vertices MUCH faster

        Rand.ValueBuffer values = rand.GetValueBuffer(101);
        int fieldSeed = rand.Range(0, 20000);

        List<int> triangleID = new List<int>(verts.Length / 3 * maxGrassPerTriangle);
        for (int i = 0; i < verts.Length; i += 3)
        {
            Vector3 v0 = verts[i];

            if (v0.sqrMagnitude < Mathx.Square(oceanRadius - 20))
                continue;

            Vector3 v1 = verts[i + 1];
            Vector3 v2 = verts[i + 2];

            Vector3 v01 = v1 - v0;
            Vector3 v02 = v2 - v0;
            Vector3 up = v0.normalized;
            Vector3 normal = Vector3.Cross(v01, v02);
            float twoArea = normal.magnitude;
            normal /= twoArea;
            float gradient = Mathf.InverseLerp(dotCutoff, 1, Vector3.Dot(normal, up));
            if (gradient == 0)
                continue;

            int blades = Mathf.Min(maxGrassPerTriangle, Mathf.CeilToInt(grassPerUnitArea * twoArea * gradient * Mathf.Lerp(0.5f, 1, values.Next())));
            for (int j = 0; j < blades; j++)
                triangleID.Add(i);
        }
        int grassCount = triangleID.Count;
        if (grassCount == 0)
            return;

        Mesh grassBladeMesh = GrassMeshLOD;
        Mesh grassFieldMesh = meshFilter.sharedMesh = new Mesh();

        int vertexCount = grassCount * grassBladeMesh.vertexCount;
        int indexCount = 3 * grassCount * (grassBladeMesh.vertexCount - 2);

        AllocateMesh(grassFieldMesh, vertexCount, indexCount);

        ComputeBuffer GrassVerts = new ComputeBuffer(grassBladeMesh.vertexCount, sizeof(float) * 3);
        ComputeBuffer GrassTris = new ComputeBuffer(grassBladeMesh.vertexCount - 2, sizeof(int) * 3);
        ComputeBuffer GrassUVs = new ComputeBuffer(grassBladeMesh.vertexCount, sizeof(float) * 2);

        ComputeBuffer Values = new ComputeBuffer(101, sizeof(float));
        ComputeBuffer Verts = new ComputeBuffer(Field.Surface.vertexCount, sizeof(float) * 3);
        ComputeBuffer TriangleID = new ComputeBuffer(grassCount, sizeof(int));

        GrassVerts.SetData(grassBladeMesh.vertices);
        GrassTris.SetData(grassBladeMesh.triangles);
        GrassUVs.SetData(grassBladeMesh.uv);
        Values.SetData(values.GetArray());
        Verts.SetData(verts);
        TriangleID.SetData(triangleID);

        GrassBuilder.SetBuffer(0, "GrassVerts", GrassVerts);
        GrassBuilder.SetBuffer(0, "GrassTris", GrassTris);
        GrassBuilder.SetBuffer(0, "GrassUVs", GrassUVs);

        GrassBuilder.SetBuffer(0, "Values", Values);
        GrassBuilder.SetBuffer(0, "Verts", Verts);
        GrassBuilder.SetBuffer(0, "TriangleID", TriangleID);

        GrassBuilder.SetBuffer(0, "VertexBuffer", vertexBuffer);
        GrassBuilder.SetBuffer(0, "IndexBuffer", indexBuffer);

        GrassBuilder.SetInt("grassVertsLength", grassBladeMesh.vertexCount);
        GrassBuilder.SetInt("grassCount", grassCount);
        GrassBuilder.SetInt("fieldSeed", fieldSeed);
        
        GrassBuilder.DispatchThreads(0, grassCount, 1, 1);

        GrassVerts.Dispose();
        GrassTris.Dispose();
        GrassUVs.Dispose();
        Values.Dispose();
        Verts.Dispose();
        TriangleID.Dispose();

        grassFieldMesh.bounds = Field.Surface.bounds;
    }

    private void OnDestroy()
    {
        if (UpdateCoroutine != null)
            StopCoroutine(UpdateCoroutine);
        UnGenerate();
    }


    void AllocateMesh(Mesh mesh, int vertexCount, int indexCount)
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

        // Vertex uv: float32 x 2
        var vu = new VertexAttributeDescriptor
            (VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2);

        // Vertex/index buffer formats
        mesh.SetVertexBufferParams(vertexCount, vp, vn, vu);
        mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt32);

        // Submesh initialization
        mesh.SetSubMesh(0, new SubMeshDescriptor(0, indexCount),
                            MeshUpdateFlags.DontRecalculateBounds);

        // GraphicsBuffer references
        vertexBuffer = mesh.GetVertexBuffer(0);
        indexBuffer = mesh.GetIndexBuffer();
    }

    /// <summary>
    /// Pros: fastest for small meshes. Cons: becomes much slower for large meshes.
    /// </summary>
    private void CreateGrassFieldCPU()
    {
        rand = new Rand(seed);

        Vector3[] verts = Field.Surface.vertices; //this makes accessing the vertices MUCH faster
        Rand.ValueBuffer values = rand.GetValueBuffer(101);
        int fieldSeed = rand.Range(0, 20000);
        float[] valueArray = values.GetArray();

        int triangleCount = Field.Surface.vertexCount / 3;
        
        int maxTRSLength = triangleCount * maxGrassPerTriangle;

        List<CombineInstance> grassBladesLOD = new List<CombineInstance>(maxTRSLength);

        for (int i = 0; i < verts.Length; i += 3)
        {
            Vector3 v0 = verts[i];

            if (v0.sqrMagnitude < Mathx.Square(oceanRadius - 20))
                continue;

            Vector3 v1 = verts[i + 1];
            Vector3 v2 = verts[i + 2];

            Vector3 v01 = v1 - v0;
            Vector3 v02 = v2 - v0;
            Vector3 up = v0.normalized;
            Vector3 normal = Vector3.Cross(v01, v02);
            float twoArea = normal.magnitude;
            normal /= twoArea;
            float gradient = Mathf.InverseLerp(dotCutoff, 1, Vector3.Dot(normal, up));
            if (gradient == 0)
                continue;

            int blades = Mathf.Min(maxGrassPerTriangle, Mathf.CeilToInt(grassPerUnitArea * twoArea * gradient * Mathf.Lerp(0.5f, 1, values.Next())));

            for (int k = 0; k < blades; k++)
            {
                int seed = fieldSeed + 3 * grassBladesLOD.Count;

                float a01 = valueArray[seed * 7 % 101];
                float b01 = valueArray[(seed + 1) * 7 % 101];
                Vector3 p = v0;
                if (a01 + b01 < 1)
                    p += v01 * a01 + v02 * b01;
                else
                    p += v01 * (1 - a01) + v02 * (1 - b01);

                Matrix4x4 TRS = Matrix4x4.TRS(p, Quaternion.FromToRotation(Vector3.up, p.normalized) * Quaternion.Euler(0, valueArray[(seed + 2) * 7 % 101] * 360, 0), Vector3.one);

                CombineInstance combineInstance = new CombineInstance { transform = TRS, mesh = GrassMeshLOD };
                grassBladesLOD.Add(combineInstance);
            }
        }

        Mesh grassFieldMesh = meshFilter.sharedMesh = new Mesh();
        grassFieldMesh.indexFormat = IndexFormat.UInt32;
        grassFieldMesh.CombineMeshes(grassBladesLOD.ToArray(), true, true, false);
        grassFieldMesh.UploadMeshData(true);
    }

    public void CreateGrassMeshLODs(float bladeHeight)
    {
        GrassMeshLOD = CreateGrassMesh(grassRes, bladeHeight);
    }
    private Mesh CreateGrassMesh(int grassRes, float bladeHeight)
    {
        Vector3[] verts = new Vector3[2 * grassRes + 1];
        Vector3[] norms = new Vector3[2 * grassRes + 1];
        Vector2[] uv = new Vector2[2 * grassRes + 1];
        int[] tris = new int[6 * grassRes - 3];

        verts[0] = new Vector3(0, bladeHeight, 0);
        norms[0] = Vector3.forward;
        uv[0] = new Vector2(0.5f, 1);        

        for (int i = 0; i < grassRes; i++)
        {
            int i2 = 2 * i;
            int i6 = i == 0 ? 0 : 6 * i - 3;

            //https://www.desmos.com/calculator/8kn2hrxdtr
            float y = bladeHeight * (1 - Mathx.Square((i + 1f) / grassRes));
            float x = bladeWidth * Mathx.Tanh((bladeHeight - y) / bladeWidthDropoff);
            verts[i2 + 1] = new Vector3(-x, y, 0);
            verts[i2 + 2] = new Vector3(x, y, 0);
            norms[i2 + 1] = Vector3.forward;
            norms[i2 + 2] = Vector3.forward;
            uv[i2 + 1] = new Vector2(0, y / bladeHeight);
            uv[i2 + 2] = new Vector2(1, y / bladeHeight);

            tris[i6 + 0] = i2 + 1;
            tris[i6 + 1] = i2 + 0;
            tris[i6 + 2] = i2 + 2;
            if (i > 0)
            {
                tris[i6 + 3] = i2 + 1;
                tris[i6 + 4] = i2 - 1;
                tris[i6 + 5] = i2 + 0;
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = verts;
        mesh.normals = norms;
        mesh.uv = uv;
        mesh.triangles = tris;
        mesh.bounds = new Bounds(0.5f * bladeHeight * Vector3.up, new Vector3(bladeWidth * Mathx.Tanh(bladeHeight / bladeWidthDropoff), bladeHeight, 0));
        return mesh;
    }
    public void StartUpdateGrassLOD(Rand.Seed seed, PlanetSubmesh submesh, float oceanRadius, float dotCutoff)
    {
        this.dotCutoff = dotCutoff;
        this.seed = seed;
        this.oceanRadius = oceanRadius;
        Field = submesh;

        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();

        bool keepLoaded = Field.Surface.vertexCount < maxKeepLoadedVertexCount;
        if (keepLoaded)
            Generate();
        UpdateCoroutine = StartCoroutine(UpdateGrassLOD(keepLoaded));
    }
    private void Generate()
    {
        if (Field.Surface.vertexCount < maxCPUVertexCount)
            CreateGrassFieldCPU();
        else
            CreateGrassFieldGPU();
    }
    private void UnGenerate()
    {
        if (vertexBuffer != null)
            vertexBuffer.Dispose();
        if (indexBuffer != null)
            indexBuffer.Dispose();
        if (meshFilter.sharedMesh != null)
            Destroy(meshFilter.sharedMesh);
    }

    private IEnumerator UpdateGrassLOD(bool keepLoaded)
    {
        bool generated = keepLoaded;
        while (true)
        {
            if (Physics.Raycast(PlayerRobotWeight.Player.Position, transform.position - PlayerRobotWeight.Player.Position, out RaycastHit hitInfo, lodGridDist, LayerMask.NameToLayer("ZeroWeight"))
                && hitInfo.collider != Field.meshCollider && Field.PlayerDist > lodGridDist)
            {
                yield return new WaitForSeconds(3 * updateRate);
                continue;
            }
            for (int i = 0; i < 3; i++)
            {
                bool show = Field.PlayerDist <= lodGridDist;
                bool keepMeshData = keepLoaded || Field.PlayerDist <= keepMeshDataGridDist;

                meshRenderer.enabled = show;
                if (!generated && show)
                {
                    Generate();
                    generated = true;
                }
                else if (generated && !keepMeshData)
                {
                    UnGenerate();
                    generated = false;
                }
                yield return new WaitForSeconds(updateRate);
            }
        }
    }
}
