using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class CustomLineRenderer : MonoBehaviour
{
    public Material sharedMaterial => meshRenderer == null ? GetComponent<MeshRenderer>().sharedMaterial : meshRenderer.sharedMaterial;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    List<Vector3> verticies;

    private void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer.enabled = false;

        verticies = new List<Vector3>();
    }

    public void SetMaterial(Material mat)
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = mat;
    }

    public void Clear()
    {
        verticies.Clear();
        meshRenderer.enabled = false;
    }


    public void AddPosition(Vector3 left, Vector3 right)
    {
        verticies.Add(left);
        verticies.Add(right);
    }
    public void Displace(Vector3 displacement)
    {
        for (int i = 0; i < verticies.Count; i++)
            verticies[i] += displacement;
    }

    public void UpdateVerticies()
    {
        meshFilter.sharedMesh.vertices = verticies.Select(x => transform.worldToLocalMatrix.MultiplyPoint(x)).ToArray();
    }

    public void UpdateMesh()
    {
        if (verticies.Count < 4)
            return;

        Vector2[] uv = new Vector2[verticies.Count];
        for (int i = 0; i < verticies.Count; i += 2)
        {
            uv[i] = new Vector2(1 - (float)i / uv.Length, 0);
            uv[i + 1] = new Vector2(1 - (float)i / uv.Length, 1);
        }
        int[] triangles = new int[(verticies.Count - 2) * 3];
        for (int i = 0; i < verticies.Count - 2; i += 2)
        {
            triangles[3 * i] = i;
            triangles[3 * i + 1] = i + 3;
            triangles[3 * i + 2] = i + 1;

            triangles[3 * i + 3] = i;
            triangles[3 * i + 4] = i + 2;
            triangles[3 * i + 5] = i + 3;
        }

        meshFilter.sharedMesh = new Mesh();
        UpdateVerticies();
        meshFilter.sharedMesh.triangles = triangles;
        meshFilter.sharedMesh.uv = uv;
        meshFilter.sharedMesh.RecalculateNormals();
        meshRenderer.enabled = true;
    }
}
