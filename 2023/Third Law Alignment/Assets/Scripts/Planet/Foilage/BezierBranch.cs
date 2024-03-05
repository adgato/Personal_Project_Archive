using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BezierBranch
{
    private Vector3 start;
    private Vector3 startNormalUp;
    private Vector3 endNormalUp;
    private Vector3 end;

    private float startRadius;
    private int splitCount;
    private Gradient branchWidth;
    private BezierTree tree;
    public Mesh mesh { get; private set; }
    private Rand.Seed seed;
    private Rand rand;

    public BezierBranch(Rand.Seed seed, Vector3 start, Vector3 startNormalUp, float endAngleOnCone, float startRadius, int splitCount, BezierTree tree)
    {
        this.seed = seed;
        rand = new Rand(seed);

        this.start = start;
        this.startNormalUp = startNormalUp;
        this.startRadius = startRadius;
        this.splitCount = splitCount;
        this.tree = tree;

        tree.localCentre += start;

        float length = Mathf.Lerp(1, 4 * Mathf.Max(1, tree.trunkRadius * tree.trunkRadius), startRadius / tree.trunkRadius);

        if (splitCount > tree.maxLayers || startRadius < 0.01f)
            return;

        tree.branches.Add(this);

        float endAngleOfCone = rand.Range(45, 65f);
        endNormalUp = Vector3.Slerp(AnglesFrom(endAngleOfCone, endAngleOnCone, startNormalUp), tree.sunDirection, Mathf.Lerp(0.5f, 0.25f, Mathf.Sin(Mathf.PI * splitCount / tree.maxLayers)));

        end = start + endNormalUp * length;

        GenerateBranch();
    }

    private void GenerateBranch()
    {
        float workingRadius = startRadius;
        int childCount = rand.Range(1, tree.maxSplits + 1);
        float[] childTimes = new float[childCount];
        for (int i = 0; i < childCount; i++)
            childTimes[i] = i == 0 ? 1 : rand.Range(0.5f, 1);
        System.Array.Sort(childTimes);

        float splitEndAngleOffset = rand.Range(0f, 360);
        GradientAlphaKey[] radiusKeys = new GradientAlphaKey[childCount + 1];
        radiusKeys[0] = new GradientAlphaKey(workingRadius, 0);
        for (int i = 0; i < childCount; i++)
        {
            radiusKeys[i + 1] = new GradientAlphaKey(workingRadius, childTimes[i]);

            float splitRadius = workingRadius * (i == childCount - 1 ? 1 : rand.Range(tree.widthLossRange));
            workingRadius = Mathf.Sqrt(workingRadius * workingRadius - splitRadius * splitRadius);

            new BezierBranch(rand.PsuedoNewSeed(), Bezier(childTimes[i]), BezierNormalUp(childTimes[i]), splitEndAngleOffset + i * 360f / childCount, splitRadius, splitCount + 1, tree);
        }
        branchWidth = new Gradient();
        branchWidth.SetKeys(new GradientColorKey[childCount + 1], radiusKeys);
    }

    public void CreateBranch(int maxCrossSections, int maxBranchEdges)
    {
        int crossSections = Mathf.FloorToInt(startRadius < 0.1f ? maxCrossSections * 0.25f : startRadius < 0.2f ? maxCrossSections * 0.75f : maxCrossSections);
        int branchEdges = Mathf.FloorToInt(startRadius < 0.1f ? maxBranchEdges * 0.25f : startRadius < 0.2f ? maxBranchEdges * 0.75f : maxBranchEdges);

        if (crossSections > 1)
            crossSections += crossSections % 2;
        else
            crossSections = 1;

        branchEdges += branchEdges % 2;

        mesh = new Mesh();

        if (startRadius < 0.01f || branchEdges <= 0)
            return;

        float uvScale = Mathf.Sqrt(startRadius / tree.trunkRadius);

        Vector3[] vertices = new Vector3[(crossSections + 1) * (branchEdges + 2)];
        Vector3[] normals = new Vector3[(crossSections + 1) * (branchEdges + 2)];
        Vector2[] uv = new Vector2[(crossSections + 1) * (branchEdges + 2)];
        for (int i = 0; i < crossSections + 1; i++)
        {
            float t = (float)i / crossSections;
            Vector3 crossSectionPos = Bezier(t) - start;
            Vector3 normalUp = BezierNormalUp(t);
            float radius = branchWidth.Evaluate(t).a * tree.trunkRadius;
            float angleOffset = i % 2 == 1 ? 0 : 180f / (branchEdges + 1);
            for (int j = 0; j < branchEdges + 2; j++)
            {
                if (j == branchEdges + 1)
                {
                    vertices[i * (branchEdges + 2) + j] = vertices[i * (branchEdges + 2)];
                    normals[i * (branchEdges + 2) + j] = normals[i * (branchEdges + 2)];
                    uv[i * (branchEdges + 2) + j] = uvScale * new Vector2(1, t);
                }
                else
                {
                    float angle = angleOffset + j * 360f / (branchEdges + 1);
                    Vector3 stepNormal = AngleAxis(angle, normalUp);
                    vertices[i * (branchEdges + 2) + j] = crossSectionPos + radius * stepNormal;
                    normals[i * (branchEdges + 2) + j] = stepNormal;
                    uv[i * (branchEdges + 2) + j] = uvScale * new Vector2(angle / 360, t);
                }
            }
        }
        List<int> triangles = new List<int>();
        for (int i = 0; i < crossSections + 1; i++)
        {
            for (int j = 0; j < branchEdges + 1; j++)
            {
                if (i != 0)
                {
                    triangles.Add((i - 1) * (branchEdges + 2) + j + 1);
                    triangles.Add(i * (branchEdges + 2) + j + 1);
                    triangles.Add(i * (branchEdges + 2) + j);
                }
                if (i != crossSections)
                {
                    triangles.Add(i * (branchEdges + 2) + j + 1);
                    triangles.Add((i + 1) * (branchEdges + 2) + j);
                    triangles.Add(i * (branchEdges + 2) + j);
                }
            }
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.SetTriangles(triangles, 0);
    }

    public void AddBranch(float leafBlockProb)
    {
        rand = new Rand(seed);

        if (startRadius > 0.01f)
        {
            CombineInstance branchMesh = new CombineInstance();
            branchMesh.transform = Matrix4x4.TRS(start - tree.transform.position, Quaternion.identity, Vector3.one);
            branchMesh.mesh = mesh;
            tree.branchMeshes.Add(branchMesh);
            if (startRadius > 0.5f)
                tree.branchCollisionMeshes.Add(branchMesh);
        }
        if (startRadius > 0.1f)
            return;

        float count = rand.Range(0f, Mathf.Min(splitCount, 10));

        if (count < 7)
            return;

        if (rand.Chance(leafBlockProb))
        {
            float t = rand.Range(rand.Range(0.25f, 0.5f), 1);
            CombineInstance leafMesh = new CombineInstance();
            Quaternion rotation = rand.quaternion;
            Vector3 position = Bezier(t) + rotation * Vector3.up * (0.5f + branchWidth.Evaluate(t).a * tree.trunkRadius);
            Vector3 scale = Vector3.one;
            leafMesh.transform = Matrix4x4.TRS(position - tree.transform.position, rotation, scale * 4.5f);
            leafMesh.mesh = tree.PrimitiveQuad;
            tree.leafBlockMeshes.Add(leafMesh);
            return;
        }

        for (int i = 0; i < count * tree.leafDensity; i++)
        {
            float t = rand.Range(rand.Range(0.25f, 0.5f), 1);
            CombineInstance leafMesh = new CombineInstance();
            Quaternion rotation = rand.quaternion;
            Vector3 position = Bezier(t) + rotation * Vector3.up * (0.5f + branchWidth.Evaluate(t).a * tree.trunkRadius);
            Vector3 scale = Vector3.one;
            leafMesh.transform = Matrix4x4.TRS(position - tree.transform.position, rotation, scale * 1.5f);
            leafMesh.mesh = tree.PrimitiveQuad;
            tree.leafMeshes.Add(leafMesh);
        }
    }

    private Vector3 Bezier(float t)
    {
        Vector3 P0 = start;
        Vector3 P1 = start + startNormalUp;
        Vector3 P2 = end - endNormalUp;
        Vector3 P3 = end;
        float s = 1 - t;
        return s * s * s * P0 + 3 * t * s * s * P1 + 3 * t * t * s * P2 + t * t * t * P3;
    }

    private Vector3 BezierNormalUp(float t)
    {
        Vector3 P0 = start;
        Vector3 P1 = start + startNormalUp;
        Vector3 P2 = end - endNormalUp;
        Vector3 P3 = end;
        float s = 1 - t;
        return -s * s * P0 + s * (s - 2 * t) * P1 + t * (2 * s - t) * P2 + t * t * P3;
    }

    private Vector3 AngleAxis(float angle, Vector3 normal)
    {
        return AnglesFrom(90, angle, normal);
    }
    private Vector3 AnglesFrom(float angleOfCone, float angleOnCone, Vector3 normal)
    {
        float t = angleOfCone * Mathf.Deg2Rad;
        float s = angleOnCone * Mathf.Deg2Rad;
        Vector3 localNormal = new Vector3(Mathf.Cos(s) * Mathf.Sin(t), Mathf.Sin(s) * Mathf.Sin(t), Mathf.Cos(t));
        return Quaternion.LookRotation(normal, Vector3.forward) * localNormal;
    }
}
