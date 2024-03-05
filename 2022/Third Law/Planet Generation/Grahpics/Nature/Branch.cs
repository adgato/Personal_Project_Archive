using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Branch
{
    public static TreeGen treeObj;
    public static float trunkLength;
    public static int leafDensity;
    public static Vector3 planetNormal;

    private bool created = false;
    private GameObject branch;
    public Vector3 startPoint { get; private set; }
    public Vector3 endPoint { get; private set; }
    public float branchLength { get; private set; }
    public float branchWidth { get; private set; }
    public Vector3 branchNormal { get; private set; }
    public int splitsInto { get; private set; }
    public int consecutiveSplits { get; private set; }

    public Branch(Vector3 _startPos, float _branchLength, float _branchWidth, Vector3 parentNormal, Vector2 directionRange, int _consecutiveSplits)
    {
        consecutiveSplits = _consecutiveSplits + 1;

        startPoint = _startPos;
        branchLength = _branchLength;
        branchWidth = _branchWidth;
        endPoint = GetBranchEndPoint(parentNormal, directionRange);
        branchNormal = (endPoint - startPoint).normalized;
        splitsInto = Random.Range(1, 3);

        branch = new GameObject("Branch of Layer " + consecutiveSplits);
        
        branch.layer = treeObj.gameObject.layer;
        branch.transform.parent = treeObj.root.transform;
        branch.transform.localPosition = Vector3.zero;
    }
    Vector3 GetBranchEndPoint(Vector3 parentNormal, Vector2 directionRange)
    {
        float z;
        //Root branch more aligned with planetNormal
        if (consecutiveSplits == 1)
            z = Random.Range(Mathf.Cos(Mathf.PI / 12), 1);
        else
            z = Random.Range(Mathf.Cos(Mathf.PI / 6), Mathf.Cos(Mathf.PI / 3));
        float a = Random.Range(directionRange.x, directionRange.y);
        //https://math.stackexchange.com/questions/56784/generate-a-random-direction-within-a-cone/205589#205589
        Vector3 normalDir = new Vector3(Mathf.Sqrt(1 - z * z) * Mathf.Cos(a), Mathf.Sqrt(1 - z * z) * Mathf.Sin(a), z);
        return startPoint + Quaternion.FromToRotation(Vector3.forward, (planetNormal + parentNormal) / 2) * normalDir * branchLength;
    }
    public void HideBranch()
    {
        branch.SetActive(false);
    }
    public void ShowBranch()
    {
        branch.SetActive(true);
        if (!created)
            CreateBranch();
        created = true;
    }
    private void CreateBranch()
    {
        GameObject branchObj = Object.Instantiate(treeObj.BranchPrefab, branch.transform);
        branchObj.transform.localPosition = (startPoint + endPoint) / 2;
        branchObj.transform.GetChild(0).rotation = Quaternion.Euler(Quaternion.LookRotation(branchNormal).eulerAngles + new Vector3(90, 0, 0));
        branchObj.transform.GetChild(0).localScale = new Vector3(branchWidth, branchLength / 2, branchWidth);

        if (consecutiveSplits <= 2)
            branchObj.transform.GetChild(0).GetComponent<CapsuleCollider>().enabled = true;

        CreateLeaves();
    }
    public void CreateLeaves()
    {
        /*
        for (int i = 0; i < Random.Range(0, leafDensity * consecutiveSplits); i++)
        {
            GameObject leafObj = Object.Instantiate(gameObject.LeafPrefab, branch.transform);
            leafObj.transform.position = Vector3.Lerp(startPoint, endPoint, Random.Range(Random.Range(0.25f, 0.5f), 1));
            leafObj.transform.rotation = Random.rotation;
        }
        */
        //Add leaves only to branches that are higher up the tree
        if (consecutiveSplits > 2)
        {
            branch.transform.GetChild(0).GetChild(1).gameObject.SetActive(true);
            for (int i = 0; i < branch.transform.GetChild(0).GetChild(1).childCount; i++)
            {
                GameObject leafObj = branch.transform.GetChild(0).GetChild(1).GetChild(i).gameObject;
                leafObj.transform.position = branch.transform.position + Vector3.Lerp(startPoint, endPoint, Random.Range(Random.Range(0.25f, 0.5f), 1));
                leafObj.transform.rotation = Random.rotation;
            }
        }
    }
}