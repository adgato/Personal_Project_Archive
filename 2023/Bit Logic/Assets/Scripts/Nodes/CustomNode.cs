using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class CustomNode : Node
{
    public ExternalData[] saveSubNodes;
    public Stack<Node> subnodes { get; private set; } = new Stack<Node>();

    public List<Socket> out2subout { get; private set; } = new List<Socket>();

    public Dictionary<Socket, int> subin2in { get; private set; } = new Dictionary<Socket, int>();

    public Dictionary<Vector2Int, PortFwdNode> portFwdNodes { get; private set; } = new Dictionary<Vector2Int, PortFwdNode>();

    private Dictionary<Socket, int> ioRevIdxLookup = new Dictionary<Socket, int>();

    public override int InPortCount => subin2in.Count;
    public override int OutPortCount => out2subout.Count;
    public override bool AllowCycle => true;

    public int localTileHeight;
    public int localTileWidth;

    public void GenerateSaveData()
    {
        Socket[] leftOrdered = ioRevIdxLookup.Keys.Where(sock => (sock.inOut ^ sock.node.reverse) && !sock.node.vertical).OrderBy(sock => sock.GetTile().y).ToArray();
        Socket[] rightOrdered = ioRevIdxLookup.Keys.Where(sock => (sock.inOut ^ !sock.node.reverse) && !sock.node.vertical).OrderBy(sock => sock.GetTile().y).ToArray();
        Socket[] upOrdered = ioRevIdxLookup.Keys.Where(sock => (sock.inOut ^ !sock.node.reverse) && sock.node.vertical).OrderBy(sock => sock.GetTile().x).ToArray();
        Socket[] downOrdered = ioRevIdxLookup.Keys.Where(sock => (sock.inOut ^ sock.node.reverse) && sock.node.vertical).OrderBy(sock => sock.GetTile().x).ToArray();
        localTileHeight = Mathf.Max(leftOrdered.Length, rightOrdered.Length, 1) + 1;
        localTileWidth = Mathf.Max(upOrdered.Length, downOrdered.Length, 1) + 1;

        localInputTiles = new Vector2Int[InPortCount];
        localOutputTiles = new Vector2Int[OutPortCount]; 
        inPortSymbols = new string[InPortCount];
        outPortSymbols = new string[OutPortCount];
        inPortSymbolPositions = new Vector2Int[InPortCount];
        outPortSymbolPositions = new Vector2Int[OutPortCount];

        void setLocalTile(Socket sock, Vector2Int pos)
        {
            int index = ioRevIdxLookup[sock];
            if (sock.inOut)
            {
                localInputTiles[index] = pos;
                if (portSymbols.ContainsKey(sock.GetTile()))
                    inPortSymbols[index] = portSymbols[sock.GetTile()];
                inPortSymbolPositions[index] = sock.GetTile();
            }
            else
            {
                localOutputTiles[index] = pos;
                if (portSymbols.ContainsKey(sock.GetTile()))
                    outPortSymbols[index] = portSymbols[sock.GetTile()];
                outPortSymbolPositions[index] = sock.GetTile();
            }
        }

        for (int y = 0; y < leftOrdered.Length; y++)
            setLocalTile(leftOrdered[y], new Vector2Int(0, y + 1));
        for (int y = 0; y < rightOrdered.Length; y++)
            setLocalTile(rightOrdered[y], new Vector2Int(localTileWidth, y + 1));
        for (int x = 0; x < upOrdered.Length; x++)
            setLocalTile(upOrdered[x], new Vector2Int(x + 1, localTileHeight));
        for (int x = 0; x < downOrdered.Length; x++)
            setLocalTile(downOrdered[x], new Vector2Int(x + 1, 0));

        saveSubNodes = subnodes.Select(x => x.externalData).Reverse().ToArray();
    }

    public bool TryAddSubnode(Node node)
    {
        subnodes.Push(node);
        if (TryLinkCircuit())
            return true;
        subnodes.Pop();
        TryLinkCircuit();
        return false;
    }
    public bool TryPopSubnode(out Node node)
    {
        bool success = subnodes.TryPop(out node);
        if (success)
            TryLinkCircuit();
        return success;
    }
    public void BatchRemoveSubnodes(HashSet<Node> nodes)
    {
        List<Node> newSubnodes = subnodes.Where(x => !nodes.Contains(x)).Reverse().ToList();
        subnodes.Clear();
        foreach (Node node in newSubnodes)
            subnodes.Push(node);
        TryLinkCircuit();
        return;
    }

    public bool TryLinkCircuit()
    {
        Dictionary<Socket, int> subin2in = new Dictionary<Socket, int>();
        List<Socket> out2subout = new List<Socket>();
        Dictionary<Vector2Int, PortFwdNode> portFwdNodes = new Dictionary<Vector2Int, PortFwdNode>();
        Dictionary<Socket, int> ioRevIdxLookup = new Dictionary<Socket, int>();
        HashSet<Socket> linkedOutSockets = new HashSet<Socket>();

        foreach (Node inNode in subnodes)
        {
            for (int i = 0; i < inNode.InPortCount; i++)
            {
                Vector2Int inputTile = inNode.GetInputTile(i);
                bool foundNodeIn = false;
                foreach (Node outNode in subnodes)
                    for (int j = 0; j < outNode.OutPortCount; j++)
                        if (inputTile == outNode.GetOutputTile(j))
                        {
                            if (foundNodeIn)
                                return false;

                            Socket sockin = new Socket(outNode, j, false);
                            linkedOutSockets.Add(sockin);
                            inNode.NodesIn[i] = sockin;
                            foundNodeIn = true;
                            break;
                        }

                if (!foundNodeIn)
                {
                    Socket key = new Socket(inNode, i, true);
                    Vector2Int coord = key.GetTile();
                    if (portFwdNodes.ContainsKey(coord))
                        inNode.NodesIn[i] = new Socket(portFwdNodes[coord], -1, false);
                    else
                    {
                        PortFwdNode dummy = new PortFwdNode(this, key);
                        portFwdNodes.Add(key.GetTile(), dummy);

                        inNode.NodesIn[i] = new Socket(dummy, -1, false);

                        ioRevIdxLookup.Add(key, subin2in.Count);
                        subin2in.Add(key, subin2in.Count);
                    }
                }
            }
        }
        foreach (Node outNode in subnodes)
            for (int j = 0; j < outNode.OutPortCount; j++)
            {
                Socket key = new Socket(outNode, j, false);
                if (!linkedOutSockets.Contains(key))
                {
                    if (out2subout.Select(sock => sock.GetTile()).Contains(key.GetTile()))
                        return false;
                    ioRevIdxLookup.Add(key, out2subout.Count);
                    out2subout.Add(key);
                }
            }

        //This only finds cycles connected to outputs, which is fine, because only these cycles will cause errors.
        //Also, I'm starting to like helper functions within functions in OOP.
        HashSet<Node> safe = new HashSet<Node>();
        HashSet<Node> visited = new HashSet<Node>();
        bool FindCycle(Node node)
        {
            if (node.AllowCycle || safe.Contains(node))
                return false;
            if (visited.Contains(node))
                return true;
            visited.Add(node);
            for (int i = 0; i < node.InPortCount; i++)
            {
                if (FindCycle(node.NodesIn[i].node))
                    return true;
            }
            safe.Add(node);
            return false;
        }
        foreach (Socket socket in out2subout)
            if (FindCycle(socket.node))
                return false;

        this.subin2in = subin2in;
        this.out2subout = out2subout;
        this.portFwdNodes = portFwdNodes;
        this.ioRevIdxLookup = ioRevIdxLookup;
        NodesIn = new Socket[InPortCount];

        return true;
    }

    public override void PrePropagate()
    {
        base.PrePropagate();
        foreach (Node node in subnodes)
            node.PrePropagate();
        foreach (Node node in portFwdNodes.Values)
            node.PrePropagate();
    }

    protected override bool GenOutput(int i,  bool noCycle) => out2subout[i].GetOutput( noCycle);
}

public class PortFwdNode : Node
{
    CustomNode customNode;
    Socket key;

    public bool input;

    public PortFwdNode(CustomNode customNode, Socket key)
    {
        this.customNode = customNode;
        this.key = key;
        Move(key.node.GetInputTile(key.port));
    }

    protected override bool GenOutput(int _,  bool noCycle)
    {
        if (customNode.NodesIn[customNode.subin2in[key]].node == null)
            return input;
        return customNode.NodesIn[customNode.subin2in[key]].GetOutput( noCycle);
    }
}