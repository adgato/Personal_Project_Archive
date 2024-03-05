using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassFieldTest : MonoBehaviour
{
    [System.Serializable]
    private struct GrassLOD
    {
        public float lodDistance;
        [Min(1)]
        public int grassRes;
    }

    [SerializeField] private Rand.Seed seed;
    [SerializeField] private GrassLOD[] grassLODs;
    [SerializeField] private Mesh FieldMesh;
    private static Mesh[] GrassMeshLOD;

    [Tooltip("Should be in descending order, with a minimum of 1.")]
    [SerializeField] private float bladeHeight = 6;
    [SerializeField] private float bladeWidth = 0.5f;
    [SerializeField] private float bladeWidthDropoff = 2;

    [SerializeField] private float grassPerTriangle;
    [SerializeField] private float triangleCutoff;

    private Rand rand;

    // Start is called before the first frame update
    void Start()
    {
        CreateGrassField();
    }

    private void CreateGrassField()
    {
        rand = new Rand(seed);

        if (GrassMeshLOD == null || GrassMeshLOD.Length < grassLODs.Length)
            CreateGrassMeshLODs();

        List<CombineInstance>[] grassBladesLOD = new List<CombineInstance>[grassLODs.Length];
        for (int i = 0; i < grassLODs.Length; i++)
            grassBladesLOD[i] = new List<CombineInstance>();

        for (int i = 0; i < FieldMesh.triangles.Length; i += 3)
        {
            Vector3 v0 = FieldMesh.vertices[FieldMesh.triangles[i + 0]];
            Vector3 v1 = FieldMesh.vertices[FieldMesh.triangles[i + 1]];
            Vector3 v2 = FieldMesh.vertices[FieldMesh.triangles[i + 2]];
            Vector3 up = Vector3.up; //v0.normalized;

            if (Vector3.Dot(Vector3.Cross(v1 - v0, v2 - v0), up) < triangleCutoff)
                continue;

            for (int k = 0; k < Mathf.FloorToInt(grassPerTriangle); k++)
            {
                if (k == Mathf.FloorToInt(grassPerTriangle) - 1 && !rand.Chance(Mathx.Frac(grassPerTriangle)))
                    break;

                float a01 = rand.value;
                float b01 = rand.value;
                Vector3 p = v0;
                if (a01 + b01 < 1)
                    p += (v1 - v0) * a01 + (v2 - v0) * b01;
                else
                    p += (v1 - v0) * (1 - a01) + (v2 - v0) * (1 - b01);

                Matrix4x4 TRS = Matrix4x4.TRS(p, Quaternion.LookRotation(Vector3.Cross(rand.insideUnitCube * 2 - Vector3.one, up), up), Vector3.one);
                for (int j = 0; j < grassLODs.Length; j++)
                {
                    CombineInstance combineInstance = new CombineInstance { transform = TRS, mesh = GrassMeshLOD[j] };
                    grassBladesLOD[j].Add(combineInstance);
                }
            }

        }

        for (int i = 0; i < grassLODs.Length; i++)
        {
            Mesh grassBladeMesh = transform.GetChild(i).GetComponent<MeshFilter>().sharedMesh = new Mesh();
            //grassBladeMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            grassBladeMesh.CombineMeshes(grassBladesLOD[i].ToArray(), true, true, false);
        }
    }

    private void CreateGrassMeshLODs()
    {
        GrassMeshLOD = new Mesh[grassLODs.Length];
        for (int i = 0; i < grassLODs.Length; i++)
            GrassMeshLOD[i] = CreateGrassMesh(grassLODs[i].grassRes);
    }
    private Mesh CreateGrassMesh(int grassRes)
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
    public void StartUpdateGrassLOD(Rand.Seed seed, Mesh FieldMesh)
    {

    }
}
