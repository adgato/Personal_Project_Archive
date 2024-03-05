using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class QuietNetwork
{
    public static float learning_rate { get; private set; }
    public static float batchTime { get; private set; }

    public InputNode backPropToNode;
    public OutputNode forwardPropToNode;

    public List<Node> nodes;
    public List<Arc> arcs;

    public void Initialise(float learningRate, int batchSize)
    {
        learning_rate = learningRate;

        Batch.size = batchSize;
        foreach (Node node in nodes)
        {
            if (node.GetType().IsSubclassOf(typeof(InputNode)))
                backPropToNode = (InputNode)node;
            else if (node.GetType().IsSubclassOf(typeof(OutputNode)))
                forwardPropToNode = (OutputNode)node;

            node.InitialiseForPropagation();
        }
    }

    /// <summary>
    /// Train the next batch.
    /// </summary>
    public void TrainBatch()
    {
        foreach (Node node in nodes)
            node.Clear();

        float start = Time.realtimeSinceStartup;

        forwardPropToNode.GetInput();
        backPropToNode.GetCost();

        batchTime = Time.realtimeSinceStartup - start;
    }

    /// <summary>
    /// Test the next batch.
    /// </summary>
    /// <param name="raw_score">The number of correctly evaluated outputs from the entire batch.</param>
    public void Test(out int raw_score)
    {
        foreach (Node node in nodes)
            node.Clear();

        raw_score = 0;
        forwardPropToNode.GetInput();
        foreach (bool mark in forwardPropToNode.Correct())
        {
            if (mark)
                raw_score++;
        }
    }

    public void SaveNetwork(string saveName)
    {
        string path = Application.persistentDataPath + "/" + saveName;

        if (Directory.Exists(path))
            Directory.Delete(path, true);

        Directory.CreateDirectory(path);

        int i = 0;
        foreach (Node node in nodes)
        {
            Directory.CreateDirectory(path + "/" + i);
            node.Save(path + "/" + i);
            i++;
        }
        Debug.Log("Saved!");
    }

    /// <param name="customNodeName">The name of the custom node to load as a network, this CustomNode must have 1 InputNode and OutputNode</param>
    public QuietNetwork(string customNodeName)
    {
        new CustomNode(customNodeName).GetSubNet(out nodes, out arcs);
    }

    public QuietNetwork(List<Node> nodes, List<Arc> arcs)
    {
        this.nodes = nodes;
        this.arcs = arcs;
    }
}
