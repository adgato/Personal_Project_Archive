using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BezierTree : MonoBehaviour
{
    [System.Serializable]
    private struct TreeLOD
    {
        public float lodDistance;

        public int maxBranchEdges;
        [Tooltip("Should be even")]
        public int maxCrossSections;
        [Range(0, 1)]
        public float leafBlockProb;
    }

    [SerializeField] private Rand.Seed seed;
    [SerializeField] private TreeLOD[] treeLODs;

    public Vector3 sunDirection = Vector3.up;
    [Min(1)]
    public int maxSplits = 2;
    public Vector2 widthLossRange = new Vector2(0.5f, 0.7f);
    [Range(0, 2)]
    public float leafDensity = 1;

    public float trunkRadius = 3;
    [Range(0, 2)]
    public float treeScale = 1;
    public int maxLayers = 16;
    public Mesh PrimitiveQuad;
    public List<BezierBranch> branches;
    public List<CombineInstance> branchCollisionMeshes;
    public List<CombineInstance> branchMeshes;
    public List<CombineInstance> leafMeshes;
    public List<CombineInstance> leafBlockMeshes;
    private Rand rand;

    private Coroutine UpdateCoroutine;

    [SerializeField] private float updateRate = 1;

    [HideInInspector] public Vector3 localCentre;

    public void GenerateTree(Rand.Seed seed)
    {
        this.seed = seed;
        rand = new Rand(seed);
        treeScale = rand.Range(0.5f, 1);
        branches = new List<BezierBranch>();
        localCentre = Vector3.zero;
        new BezierBranch(rand.PsuedoNewSeed(), transform.position, sunDirection, rand.Range(0, 360f), trunkRadius * treeScale * treeScale, 0, this);
        localCentre *= 0.5f / branches.Count;
    }
    public void GenerateTree() => GenerateTree(seed);

    public void RenderTree(int meshColliderLOD = -1)
    {
        for (int lod = 0; lod < treeLODs.Length; lod++)
            RenderTreeLOD(lod, lod == meshColliderLOD);
    }

    private void RenderTreeLOD(int lod, bool makeMeshCollider)
    {
        branchCollisionMeshes = new List<CombineInstance>();
        branchMeshes = new List<CombineInstance>();
        leafMeshes = new List<CombineInstance>();
        leafBlockMeshes = new List<CombineInstance>();

        foreach (BezierBranch branch in branches)
        {
            branch.CreateBranch(treeLODs[lod].maxCrossSections, treeLODs[lod].maxBranchEdges);
            branch.AddBranch(treeLODs[lod].leafBlockProb);
        }

        if (makeMeshCollider)
        {
            Mesh branchCollisionMesh = transform.GetChild(0).GetChild(0).GetComponent<MeshCollider>().sharedMesh = new Mesh();
            branchCollisionMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
            branchCollisionMesh.CombineMeshes(branchCollisionMeshes.ToArray(), true, true, false);
        }

        Mesh branchMesh = transform.GetChild(lod).GetChild(0).GetComponent<MeshFilter>().sharedMesh = new Mesh();
        branchMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        branchMesh.CombineMeshes(branchMeshes.ToArray(), true, true, false);

        Mesh leafMesh = transform.GetChild(lod).GetChild(1).GetComponent<MeshFilter>().sharedMesh = new Mesh();
        leafMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        leafMesh.CombineMeshes(leafMeshes.ToArray(), true, true, false);

        Mesh leafBlockMesh = transform.GetChild(lod).GetChild(2).GetComponent<MeshFilter>().sharedMesh = new Mesh();
        leafBlockMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
        leafBlockMesh.CombineMeshes(leafBlockMeshes.ToArray(), true, true, false);

        branchCollisionMeshes.Clear();
        branchMeshes.Clear();
        leafMeshes.Clear();
        leafBlockMeshes.Clear();
    }

    //void Start()
    //{
    //    GenerateTree();
    //    for (int lod = 0; lod < treeLODs.Length; lod++)
    //        RenderTree(lod);
    //    StartCoroutine(UpdateTreeLOD());
    //}

    private void OnDestroy()
    {
        if (UpdateCoroutine != null)
            StopCoroutine(UpdateCoroutine);
    }

    public void StartUpdateTreeLOD()
    {
        UpdateCoroutine = StartCoroutine(UpdateTreeLOD());
    }
    public void RemoveTree()
    {
        StopCoroutine(UpdateCoroutine); 
        for (int lod = 0; lod < treeLODs.Length; lod++)
        {
            transform.GetChild(lod).GetChild(0).GetComponent<MeshFilter>().sharedMesh.Clear();
            transform.GetChild(lod).GetChild(1).GetComponent<MeshFilter>().sharedMesh.Clear();
            transform.GetChild(lod).GetChild(2).GetComponent<MeshFilter>().sharedMesh.Clear();
            transform.GetChild(lod).gameObject.SetActive(false);
        }
    }

    private IEnumerator UpdateTreeLOD()
    {
        while (true)
        {
            float sqrDist = (PlayerRobotWeight.Player.Position - transform.position).sqrMagnitude;
            bool higherEnabled = false;
            for (int lod = 0; lod < treeLODs.Length; lod++)
            {
                transform.GetChild(lod).gameObject.SetActive(!higherEnabled && sqrDist < Mathx.Square(treeLODs[lod].lodDistance));
                higherEnabled |= transform.GetChild(lod).gameObject.activeSelf;
            }
            yield return new WaitForSeconds(updateRate);
        }
    }
}
