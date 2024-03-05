using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeGen : MonoBehaviour
{
    public GameObject BranchPrefab { get; private set; }
    [SerializeField] private Material leafMatPreset;

    public GameObject root { get; private set; }

    [Range(0, 20)]
    [SerializeField]
    private float consecutiveSplits;
    [SerializeField]
    private float trunkLength;
    [SerializeField]
    private float minBranchLength;
    [SerializeField]
    private float minBranchWidth;
    [SerializeField]
    private int leafDensity;
    public Vector3 planetNormal;
    public Vector3 relativePosition;

    private float sqrMaxNatureDist;

    private List<Branch> branches;


    public void SetMaterials(Planet planet, Vector3 _relativePosition)
    {
        sqrMaxNatureDist = planet.maxNatureDist * planet.maxNatureDist;
        relativePosition = _relativePosition;

        BranchPrefab = transform.GetChild(0).GetChild(0).gameObject;
        Material branchMat = new Material(BranchPrefab.transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial);
        branchMat.SetFloat("_minElevation", planet.planetMesh.elevationData.Min);
        branchMat.SetFloat("_maxElevation", planet.planetMesh.elevationData.Max);
        branchMat.SetTexture("_planetTexture", planet.terrainMat.GetTexture("_planetTexture"));
        branchMat.SetVector("_position", relativePosition);

        BranchPrefab.transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = branchMat;

        Material leafMat = new Material(leafMatPreset);
        leafMat.SetFloat("_minElevation", planet.planetMesh.elevationData.Min);
        leafMat.SetFloat("_maxElevation", planet.planetMesh.elevationData.Max);
        leafMat.SetTexture("_planetTexture", planet.terrainMat.GetTexture("_planetTexture"));
        leafMat.SetVector("_position", transform.position);
        leafMat.SetFloat("_windDensity", planet.planetValues.windSpeed * 0.06f);

        for (int i = 0; i < BranchPrefab.transform.GetChild(1).childCount; i++)
            BranchPrefab.transform.GetChild(1).GetChild(i).GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = leafMat;
    }

    public void Init(int _layer, int seed)
    {
        BranchPrefab = transform.GetChild(0).GetChild(0).gameObject;

        gameObject.layer = _layer;
        BranchPrefab.transform.GetChild(0).gameObject.layer = _layer;
        for (int i = 0; i < BranchPrefab.transform.GetChild(1).childCount; i++)
            BranchPrefab.transform.GetChild(1).GetChild(i).GetChild(0).gameObject.layer = _layer;

        Random.InitState(seed);

        Randomise();
        GenerateTree();
        //RenderTree();

        Random.InitState(System.Environment.TickCount);
    }
    private void Randomise()
    {
        consecutiveSplits = Random.Range(10, 15);
        minBranchLength = Random.Range(0.6f, 1.4f);
        trunkLength = Random.Range(4f, 8);
        minBranchWidth = Random.Range(0.001f, 0.02f);
        
        leafDensity = 0;
    }
    private void GenerateTree()
    {
        if (transform.childCount > 1)
        {
            if (Application.isEditor)
                DestroyImmediate(transform.GetChild(1).gameObject);
            else
                Destroy(transform.GetChild(1).gameObject);
        }
            

        root = new GameObject("Root (1)");
        root.layer = gameObject.layer;
        root.transform.position = transform.position;
        root.transform.parent = transform;
        Branch.treeObj = this;
        Branch.trunkLength = trunkLength;
        Branch.planetNormal = planetNormal;
        Branch.leafDensity = leafDensity;

        Vector2 fullRange = new Vector2(-Mathf.PI, Mathf.PI);

        branches = new List<Branch>();
        
        Branch trunk = new Branch(Vector3.zero, trunkLength, 0.5f, Branch.planetNormal, fullRange, 0);
        branches.Add(trunk);
        int k = 0;
        //While there are still branches generated left to split
        while (k < branches.Count)
        {
            float angleBuffer = Random.Range(-Mathf.PI, Mathf.PI);
            for (int i = 0; i < branches[k].splitsInto; i++)
            {
                //Break if the branch is too small to split or has reached the split depth limit
                if (branches[k].branchWidth / branches[k].splitsInto < minBranchWidth)
                    break;
                else if (branches[k].consecutiveSplits > consecutiveSplits)
                    break;

                Vector3 splitPoint;
                if (i == 0)
                    splitPoint = branches[k].endPoint;
                else
                    splitPoint = Vector3.Lerp(branches[k].startPoint, branches[k].endPoint, Random.Range(Random.Range(0.25f, 0.5f), 1));

                float branchWidth = branches[k].branchWidth / branches[k].splitsInto;
                float branchLength = Random.Range(minBranchLength, branches[k].branchLength);

                Vector2 directionRange = new Vector2(angleBuffer + 2 * Mathf.PI * i / branches[k].splitsInto, 0);
                directionRange.y = directionRange.x + 2 * Mathf.PI / branches[k].splitsInto;
                Branch branch = new Branch(splitPoint, branchLength, branchWidth, branches[k].branchNormal, directionRange, branches[k].consecutiveSplits);
                branches.Add(branch);
            }
            k++;
        }
    }
    public void RenderTree()
    {
        foreach (Branch branch in branches)
        {
            branch.ShowBranch();
            //branch.AddLeaves();
        }
    }


    private IEnumerator renderatLOD;

    private IEnumerator RenderAtLOD(float time)
    {

        while (true)
        {
            yield return new WaitForSeconds(time);

            float LOD = Mathf.InverseLerp(0, sqrMaxNatureDist, (transform.position - Camera.main.transform.position).sqrMagnitude);

            foreach (Branch branch in branches)
            {
                int splits = branch.consecutiveSplits;
                //Show entire tree if close enough, or the trunk and leaf filled branches if a bit further away
                if (LOD < 0.2f || ((splits < 2 || splits > consecutiveSplits * 0.75f) && LOD < 0.95f))
                {
                    branch.ShowBranch();
                    //branch.AddLeaves();
                }
                else
                {
                    branch.HideBranch();
                }
            }
        }
    }
    void Start()
    {
        renderatLOD = RenderAtLOD(0.5f);
        StartCoroutine(renderatLOD);
        //InvokeRepeating("RenderAtLOD", Random.value, 0.5f);
    }
    void OnDisable()
    {
        StopCoroutine(renderatLOD);
    }
    void OnEnable()
    {
        renderatLOD = RenderAtLOD(0.5f);
        StartCoroutine(renderatLOD);
        //InvokeRepeating("RenderAtLOD", Random.value, 0.5f);
    }
}
