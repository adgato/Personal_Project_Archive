using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class Network : MonoBehaviour
{
    public static bool isTraining;

    public bool toggleTrain;
    public int batchSize = 1;
    public float learningRate = 0.01f;

    public int trainSize = 60_000;
    public int testSize = 1000;

    private int total_iterations = 0;
    private int test_score = 0;
    public static float batchTime { get; private set; }

    public InputNode backPropToNode;
    public OutputNode forwardPropToNode;

    private QuietNetwork quietNetwork;

    public List<Node> nodes;
    public List<Arc> arcs;

    void Start()
    {
        nodes = new List<Node>();
        arcs = new List<Arc>();
    }
    private void OnValidate()
    {
        isTraining ^= toggleTrain;
        if (toggleTrain)
        {
            quietNetwork = new QuietNetwork(nodes, arcs);
            quietNetwork.Initialise(learningRate, batchSize);
        }
        toggleTrain = false;
    }
    private void Update()
    {
        if (!isTraining)
            return;

        if (total_iterations < trainSize)
        {
            quietNetwork.TrainBatch();
            total_iterations += Batch.size;
        }
        else if (total_iterations < trainSize + testSize)
        {
            quietNetwork.Test(out int score);
            test_score += score;
            total_iterations += Batch.size;
        }
        else if (total_iterations >= trainSize + testSize)
        {
            Debug.Log((float)test_score / testSize);
            isTraining = false;
        }
    }

    public void SaveNetwork(string saveName)
    {
        string path = Application.persistentDataPath + "/" + saveName;

        if (Directory.Exists(path))
            Directory.Delete(path, true);

        Directory.CreateDirectory(path);

        int i = 0;
        foreach (NodeInstance nodeInstance in GetComponentsInChildren<NodeInstance>())
        {
            Directory.CreateDirectory(path + "/" + i);
            nodeInstance.node.Save(path + "/" + i);
            i++;
        }
        Debug.Log("Saved!");
        NodeLoader.GetNodeTypes();
    }

    public void AddNode(string loadName)
    {
        nodes.Add(NodeLoader.NewNode(loadName));

        NodeInstance nodeInstance = Instantiate(Prefabs.Node, transform).GetComponent<NodeInstance>();
        nodeInstance.Init1(nodes[nodes.Count - 1], loadName);
    }

    public void OpenCustomNode(CustomNode customNode)
    {
        customNode.GetSubNet(out nodes, out arcs);

        if (nodes == null)
        {
            Debug.LogError("Error: no nodes to build");
            return;
        }
        for (int i = 0; i < transform.childCount; i++)
            Destroy(transform.GetChild(i).gameObject);
        transform.DetachChildren();
        for (int i = 0; i < nodes.Count; i++)
        {
            NodeInstance nodeInstance = Instantiate(Prefabs.Node, transform).GetComponent<NodeInstance>();
            nodeInstance.Init1(nodes[i], nodes[i].name);
        }
        for (int i = 0; i < arcs.Count; i++)
        {
            ArcInstance arcInstance = Instantiate(Prefabs.Arc, transform).GetComponent<ArcInstance>();
            arcInstance.Init2(arcs[i]);
        }
    }
}
