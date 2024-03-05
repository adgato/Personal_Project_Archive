using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CustomNode : Node
{
    public override string name { get { return customName; } }
    public override Color colour { get { return new Color(0.6f, 0, 1); } }
    public override int inputNo { get { return customInputNo; } }
    public override int outputNo { get { return customOutputNo; } }

    private int customInputNo;
    private int customOutputNo;
    private string customName;

    private List<Node> nodes;
    private List<Arc> arcs;

    private List<Arc> forwardArcsIn;
    private List<Arc> forwardArcsOut;
    private Forwarder forwarderIn;
    private Forwarder forwarderOut;

    protected override Batch[] ForwardPropagate(Batch[] inputs)
    {
        forwarderIn.SetData(inputs);
        return forwarderOut.GetInput();
    }
    protected override Batch[] BackPropagate(Batch[] costs)
    {
        forwarderOut.SetData(costs);
        return forwarderIn.GetCost();
    }

    public CustomNode(string type)
    {
        customName = type;
        LoadAsParent();
    }

    public void GetSubNet(out List<Node> nodes, out List<Arc> arcs)
    {
        nodes = this.nodes;
        arcs = this.arcs;
    }

    private void LoadAsParent()
    {
        customInputNo = 0;
        customOutputNo = 0;

        nodes = new List<Node>();
        arcs = new List<Arc>();

        foreach (string nodeDir in Directory.GetDirectories(Application.persistentDataPath + "/" + customName))
            nodes.Add(NodeLoader.LoadNode(nodeDir, ref arcs));

        forwarderIn = new Forwarder();
        forwarderOut = new Forwarder();
        forwardArcsIn = new List<Arc>();
        forwardArcsOut = new List<Arc>();

        foreach (Node subNode in nodes)
        {
            for (int i = 0; i < subNode.arcsIn.Length; i++)
            {
                if (subNode.arcsIn[i] != null)
                    continue;
                Arc forwardArc = new Arc(forwarderIn, customInputNo, subNode, i, -2);
                subNode.arcsIn[i] = forwardArc;
                forwardArcsIn.Add(forwardArc);
                customInputNo++;
            }
            for (int i = 0; i < subNode.arcsOut.Length; i++)
            {
                if (subNode.arcsOut[i] != null)
                    continue;
                Arc forwardArc = new Arc(subNode, i, forwarderOut, customOutputNo, -2);
                subNode.arcsOut[i] = forwardArc;
                forwardArcsOut.Add(forwardArc);
                customOutputNo++;
            }
        }

        forwarderIn.arcsIn = new Arc[0];
        forwarderIn.arcsOut = forwardArcsIn.ToArray();
        forwarderOut.arcsIn = forwardArcsOut.ToArray();
        forwarderOut.arcsOut = new Arc[0];
    }
}
