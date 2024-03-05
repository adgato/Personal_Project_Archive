using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetFoilage : MonoBehaviour
{
    [SerializeField] private GameObject treePrefab;
    [SerializeField] private GameObject grassFieldPrefab;
    [SerializeField] private Material BarkMat;
    [SerializeField] private Material LeafMat;
    [SerializeField] private Material LeafBoxMat;
    [SerializeField] private Material GrassMat;
    [SerializeField] private Material SimpleGrassMat;
    private Material currentGrassMat;
    [Min(1)]
    [SerializeField] private int uniqueTreeCount;
    [Range(0, 1)]
    [SerializeField] private float treesPerSubmeshProb;
    [Range(-1, 1)]
    [SerializeField] private float minTreeGradientDot;
    [SerializeField] private Vector2 grassCutoffDotRange01;
    [SerializeField] private Vector2 grassHeightRange;
    private List<BezierTree> instantiatedTrees;
    private List<GrassField> instantiatedGrass;

    private float oceanRadius;

    private PlanetSubmesh[] submeshes;
    private Color colour1;
    private Color colour2;

    private Rand rand;

    public void Initialise(Rand.Seed seed, PlanetSubmesh[] submeshes, float radius, float oceanRadius, Color colour1, Color colour2)
    {
        rand = new Rand(seed);
        this.submeshes = submeshes;
        this.oceanRadius = oceanRadius;
        this.colour1 = colour1;
        this.colour2 = colour2;
        float surfaceScale = Mathx.Square(radius / 640);
        gameObject.ReplaceChild(0, "Prefab Holder");
        InstantiateTrees(surfaceScale);
        InstantiateGrass();
    }

    private void InstantiateTrees(float surfaceScale)
    {
        Transform treeHolder = transform.GetChild(0).gameObject.ReplaceChild(0, "Tree Holder");

        Color.RGBToHSV(colour1, out float h1, out _, out _);
        Color.RGBToHSV(colour2, out float h2, out _, out _);

        Material bark = new Material(BarkMat);
        Material leaf = new Material(LeafMat);
        Material lebx = new Material(LeafBoxMat);
        bark.SetFloat("_Hue_Offset", h1);
        leaf.SetColor("_Colour_1", Color.HSVToRGB(h1, 0.77f, 0.55f));
        leaf.SetColor("_Colour_2", Color.HSVToRGB(h2, 1, 0.55f));
        lebx.SetColor("_Colour_1", Color.HSVToRGB(h1, 0.77f, 0.55f));
        lebx.SetColor("_Colour_2", Color.HSVToRGB(h2, 1, 0.55f));
        for (int i = 0; i < treePrefab.transform.childCount; i++)
        {
            treePrefab.transform.GetChild(i).GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = bark;
            treePrefab.transform.GetChild(i).GetChild(1).GetComponent<MeshRenderer>().sharedMaterial = leaf;
            treePrefab.transform.GetChild(i).GetChild(2).GetComponent<MeshRenderer>().sharedMaterial = lebx;
        }

        BezierTree[] uniqueTrees = new BezierTree[uniqueTreeCount];
        for (int i = 0; i < uniqueTreeCount; i++)
        {
            BezierTree tree = Instantiate(treePrefab, treeHolder).GetComponent<BezierTree>();
            tree.GenerateTree(rand.PsuedoNewSeed());
            tree.RenderTree(0); //lod = 1 might be better for performance in the future?
            tree.gameObject.SetActive(false);
            uniqueTrees[i] = tree;
        }

        instantiatedTrees = new List<BezierTree>();
        float sqrOceanRadius = Mathx.Square(oceanRadius);

        float prob = treesPerSubmeshProb * 1000 / submeshes.Length * surfaceScale;

        for (int i = 0; i < submeshes.Length; i++)
        {
            //in case a tree is spawned in a different place (due to probability that the vertex array is not ordered properly), this protects the rand variable from changing state randomly.
            Rand safeRand = new Rand(rand.PsuedoNewSeed());
            if (!safeRand.Chance(prob) || submeshes[i].Surface.bounds.center.sqrMagnitude < sqrOceanRadius)
                continue;

            Vector3[] verts = submeshes[i].Surface.vertices;
            Vector3[] norms = submeshes[i].Surface.normals;

            int index = -1;
            for (int error = 0; error < 10; error++)
            {
                int j = 3 * Mathf.FloorToInt(Mathf.Lerp(0, verts.Length / 3, safeRand.value));
                if (Vector3.Dot(norms[j], verts[j].normalized) > minTreeGradientDot)
                {
                    index = j;
                    break;
                }
            }
            if (index == -1)
                continue;

            BezierTree treePrefab = uniqueTrees[rand.Range(0, uniqueTreeCount)];

            BezierTree tree = Instantiate(
                treePrefab.transform,
                submeshes[i].transform.position + verts[index] - norms[index], 
                Quaternion.LookRotation(Vector3.Cross(safeRand.normal, norms[index]), norms[index]),
                submeshes[i].transform
                ).GetComponent<BezierTree>();
            tree.gameObject.SetActive(true);
            tree.StartUpdateTreeLOD();
            instantiatedTrees.Add(tree);
        }
    }
    private void InstantiateGrass()
    {
        //cover = tall grass, everywhere
        float cover01 = rand.value;

        grassFieldPrefab.GetComponent<GrassField>().CreateGrassMeshLODs(Mathf.Lerp(grassHeightRange.x, grassHeightRange.y, cover01));

        Color.RGBToHSV(colour1, out float h1, out float s1, out float v1);
        Color.RGBToHSV(colour2, out float h2, out float s2, out float v2);

        currentGrassMat = new Material(GrassMat);
        Material simpleGrassMat = new Material(SimpleGrassMat);

        Color grassColour1 = Color.HSVToRGB(h1, Mathf.Lerp(0.25f, 0.75f, s1), 0.5f * v1);
        Color grassColour2 = Color.HSVToRGB(h2, Mathf.Lerp(0.25f, 0.75f, s2), 0.5f * v2);

        currentGrassMat.SetColor("_Colour1", grassColour1);
        currentGrassMat.SetColor("_Colour2", grassColour2);

        simpleGrassMat.SetColor("_Colour1_5", Color.Lerp(grassColour1, grassColour2, 0.5f));


        grassFieldPrefab.GetComponent<MeshRenderer>().sharedMaterial = currentGrassMat;

        float dotCutoff = Mathf.Lerp(grassCutoffDotRange01.y, grassCutoffDotRange01.x, cover01);
        instantiatedGrass = new List<GrassField>();
        for (int i = 0; i < submeshes.Length; i++)
        {
            GrassField grass = Instantiate(
                grassFieldPrefab,
                submeshes[i].transform.position,
                submeshes[i].transform.rotation,
                submeshes[i].transform
                ).GetComponent<GrassField>();
            grass.StartUpdateGrassLOD(rand.PsuedoNewSeed(), submeshes[i], oceanRadius, dotCutoff);
            instantiatedGrass.Add(grass);
        }
    }

    public void SetPlayerPosition(Vector3 playerPos)
    {
        currentGrassMat.SetVector("_FlattenPos", playerPos);
    }
}
