using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NebulaSphere : MonoBehaviour
{
    [SerializeField] private float radius;
    [SerializeField] private int cloudCount;
    [SerializeField] private float opacity;
    [SerializeField] private Color colourA;
    [SerializeField] private Color colourB;
    [SerializeField] private float minSize;
    [SerializeField] private float maxSize;
    [SerializeField] private Material nebulaCloudPrefab;

    // Start is called before the first frame update
    void Start()
    {
        GameObject nebulaRoot = new GameObject("Clouds");
        nebulaRoot.transform.parent = transform;

        Mesh plane = CreatePlaneMesh();

        Material nebulaCloud = new Material(nebulaCloudPrefab);
        nebulaCloud.SetColor("_darkColour", colourA);
        nebulaCloud.SetColor("_lightColour", colourB);
        nebulaCloud.SetFloat("_opacity", opacity);

        for (int i = 0; i < cloudCount; i++)
        {
            GameObject _cloud = new GameObject("Nebula Cloud " + i);
            _cloud.transform.parent = nebulaRoot.transform;

            MeshFilter meshFilter = _cloud.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = plane;

            MeshRenderer meshRenderer = _cloud.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = nebulaCloud;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            _cloud.transform.localPosition = transform.position + Random.onUnitSphere * radius;
            _cloud.transform.localRotation = Quaternion.LookRotation(_cloud.transform.localPosition - transform.position);
            _cloud.transform.localScale *= Random.Range(minSize, maxSize);
        }
    }
    private Mesh CreatePlaneMesh()
    {
        Vector3[] verts = new Vector3[4]
        {
            new Vector3(-1, -1, 0),
            new Vector3(-1, 1, 0),
            new Vector3(1, -1, 0),
            new Vector3(1, 1, 0)
        };
        int[] tris = new int[6]
        {
            0, 1, 3,
            0, 3 ,2
        };
        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(0, 1),
            new Vector2(1, 0),
            new Vector2(1, 1)
        };

        Mesh plane = new Mesh();
        plane.vertices = verts;
        plane.triangles = tris;
        plane.uv = uv;
        plane.RecalculateNormals();

        return plane;
    }
}
