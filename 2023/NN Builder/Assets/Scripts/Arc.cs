using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arc
{
    public Node inputNode;
    public Node outputNode;
    public int inputNodePort;
    public int outputNodePort;
    public int ID = -1;
    private static int countID = 0;

    public float splitRatio01 = 0.5f;

    public Batch GetInput()
    {
        if (ID == -1)
        {
            Debug.LogError("Error: arc has no ID");
            return default;
        }
        return inputNode.GetInput()[inputNodePort];
    }

    public Batch GetCost()
    {
        if (ID == -1)
        {
            Debug.LogError("Error: arc has no ID");
            return default;
        }
        return outputNode.GetCost()[outputNodePort];
    }

    public Arc(Node inputNode, int inputNodePort, Node outputNode, int outputNodePort, int ID = -1)
    {
        this.inputNode = inputNode;
        this.outputNode = outputNode;
        this.inputNodePort = inputNodePort;
        this.outputNodePort = outputNodePort;
        if (ID == -1)
        {
            this.ID = countID;
            countID++;
        }
        else
        {
            this.ID = ID;
            countID = Mathf.Max(countID, ID + 1);
        }
    }
}
