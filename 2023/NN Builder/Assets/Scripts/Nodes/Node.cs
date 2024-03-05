using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class Node
{
    [System.Serializable]
    public struct NodeHeader
    {
        public string nodeType;

        public NodeHeader(string nodeType)
        {
            this.nodeType = nodeType;
        }
    }
    [System.Serializable]
    public struct NodeSaveFormat
    {
        public int[] arcIDsIn;
        public int[] arcIDsOut;
        public float[] arcInsplitRatio01s;
        public Vector2 anchoredPosition;

        public NodeSaveFormat(Node node)
        {
            arcIDsIn = new int[node.inputNo];
            arcIDsOut = new int[node.outputNo];
            arcInsplitRatio01s = new float[node.inputNo];

            for (int i = 0; i < node.inputNo; i++)
            {
                if (node.arcsIn[i] == null)
                {
                    arcIDsIn[i] = -1;
                    continue;
                }
                arcIDsIn[i] = node.arcsIn[i].ID;
                arcInsplitRatio01s[i] = node.arcsIn[i].splitRatio01;
            }
            for (int i = 0; i < node.outputNo; i++)
            {
                if (node.arcsOut[i] == null)
                {
                    arcIDsOut[i] = -1;
                    continue;
                }
                arcIDsOut[i] = node.arcsOut[i].ID;
            }

            anchoredPosition = node.instance == null ? Vector2.zero : node.instance.rectTransform.anchoredPosition;
        }
    }

    public abstract string name { get; }
    public abstract Color colour { get; }
    public abstract int inputNo { get; }
    public abstract int outputNo { get; }

    [HideInInspector] public Arc[] arcsIn;
    [HideInInspector] public Arc[] arcsOut;

    [HideInInspector] public NodeInstance instance;
    public Vector2 anchoredStartPos;

    public Batch[] output { get; protected set; }
    public Batch[] inputCost { get; private set; }
    public int iterations { get; private set; }

    private bool forwardPropagated;
    private bool backPropagated;
    public float forwardTime { get; private set; }
    public float backTime { get; private set; }

    public void Clear()
    {
        forwardPropagated = false;
        backPropagated = false;
    }
    public virtual void InitialiseForPropagation()
    {
        iterations = 0;
    }

    public Batch[] GetInput()
    {
        if (forwardPropagated)
            return output;

        Batch[] input = new Batch[inputNo];
        int i = 0;
        foreach (Arc arc in arcsIn)
        {
            if (arc != null)
                input[i] = arc.GetInput();
            i++;
        }

        float start = Time.realtimeSinceStartup;
        output = ForwardPropagate(input);
        forwardTime = Time.realtimeSinceStartup - start;

        forwardPropagated = true;
        iterations++;
        return output;
    }

    public Batch[] GetCost()
    {
        if (backPropagated)
            return inputCost;


        Batch[] cost = new Batch[outputNo];
        int i = 0;
        foreach (Arc arc in arcsOut)
            cost[i++] = arc.GetCost();

        float start = Time.realtimeSinceStartup;
        inputCost = BackPropagate(cost);
        backTime = Time.realtimeSinceStartup - start;

        backPropagated = true;
        return inputCost;
    }

    public virtual void New()
    {
        anchoredStartPos = Input.mousePosition;
        arcsIn = new Arc[inputNo];
        arcsOut = new Arc[outputNo];
    }

    protected abstract Batch[] ForwardPropagate(Batch[] inputs);
    protected abstract Batch[] BackPropagate(Batch[] costs);
    public virtual void Load(string directory, ref List<Arc> arcs)
    {
        NodeSaveFormat nodeSave = JsonSaver.LoadData<NodeSaveFormat>(directory + "/instance");

        New();
        anchoredStartPos = nodeSave.anchoredPosition;

        for (int i = 0; i < Mathf.Max(inputNo, outputNo); i++)
        {
            bool inputSet = i >= inputNo || nodeSave.arcIDsIn[i] == -1;
            bool outputSet = i >= outputNo || nodeSave.arcIDsOut[i] == -1;
            if (inputSet && outputSet)
                continue;

            foreach (Arc arc in arcs)
            {
                if (!inputSet && nodeSave.arcIDsIn[i] == arc.ID)
                {
                    arcsIn[i] = arc;
                    arcsIn[i].splitRatio01 = nodeSave.arcInsplitRatio01s[i];
                    arc.outputNode = this;
                    arc.outputNodePort = i;
                    inputSet = true;
                }
                else if (!outputSet && nodeSave.arcIDsOut[i] == arc.ID)
                {
                    arcsOut[i] = arc;
                    arc.inputNode = this;
                    arc.inputNodePort = i;
                    outputSet = true;
                }
                if (inputSet && outputSet)
                    break;
            }
            
            if (!inputSet)
            {
                Arc arc = new Arc(null, -1, this, i, nodeSave.arcIDsIn[i]);
                arcsIn[i] = arc;
                arcsIn[i].splitRatio01 = nodeSave.arcInsplitRatio01s[i];
                arc.splitRatio01 = nodeSave.arcInsplitRatio01s[i];
                arcs.Add(arc);
            }
            if (!outputSet)
            {
                Arc arc = new Arc(this, i, null, -1, nodeSave.arcIDsOut[i]);
                arcsOut[i] = arc;
                arcs.Add(arc);
            }
        }

    }
    public virtual void Save(string directory)
    {
        JsonSaver.SaveData(directory + "/header", new NodeHeader(name));
        JsonSaver.SaveData(directory + "/instance", new NodeSaveFormat(this));
    }
}
