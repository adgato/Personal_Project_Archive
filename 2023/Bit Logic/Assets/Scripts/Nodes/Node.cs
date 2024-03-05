using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class Node
{
    public struct Socket
    {
        public Node node;
        public int port;
        public bool inOut;

        public Socket(Node node, int port, bool inOut)
        {
            this.node = node;
            this.port = port;
            this.inOut = inOut;
        }

        public bool GetOutput(bool noCycle)
        {
            if (inOut)
            {
                Debug.LogError("Cannot get output from an input port");
                return default;
            }
            return node.GetOutput(port, noCycle);
        }
        public Vector2Int GetTile() => inOut ? node.GetInputTile(port) : node.GetOutputTile(port);
    }
    public Socket[] NodesIn { get; protected set; } = new Socket[0];

    [System.Serializable]
    public struct ExternalData 
    {
        public string Name;
        public byte orientation;
        public Vector2Int offsetTileOrigin;

        public ExternalData(System.Type callerType)
        {
            Name = callerType.Name;
            orientation = 0;
            offsetTileOrigin = Vector2Int.zero;
        }
    }

    public Vector2Int[] localInputTiles = new Vector2Int[0];
    public Vector2Int[] localOutputTiles = new Vector2Int[0];
    public string[] inPortSymbols = new string[0];
    public string[] outPortSymbols = new string[0];
    public Vector2Int[] inPortSymbolPositions = new Vector2Int[0];
    public Vector2Int[] outPortSymbolPositions = new Vector2Int[0];
    public ExternalData externalData;

    public Dictionary<Vector2Int, string> portSymbols { get; private set; } = new Dictionary<Vector2Int, string>();

    public bool reverse
    {
        get => (externalData.orientation & 4) > 0;
        set => externalData.orientation = (byte)(externalData.orientation & ~4 | (value ? 4 : 0));
    }
    public bool vertical
    {
        get => (externalData.orientation & 2) > 0;
        set => externalData.orientation = (byte)(externalData.orientation & ~2 | (value ? 2 : 0));
    }
    public bool flip
    {
        get => (externalData.orientation & 1) > 0;
        set => externalData.orientation = (byte)(externalData.orientation & ~1 | (value ? 1 : 0));
    }

    public Node()
    {
        externalData = new ExternalData(GetType());
    }


    public void Move(Vector2Int destination) => externalData.offsetTileOrigin = destination;
    public Vector2Int GetCentre() => externalData.offsetTileOrigin;
    public Vector2Int GetInputTile(int i) => Transform(localInputTiles[i]) + externalData.offsetTileOrigin;
    public Vector2Int GetOutputTile(int i) => Transform(localOutputTiles[i]) + externalData.offsetTileOrigin;

    public void PointRight()
    {
        reverse = false;
        vertical = false;
    }
    public void PointLeft()
    {
        reverse = true;
        vertical = false;
    }
    public void PointUp()
    {
        reverse = false;
        vertical = true;
    }
    public void PointDown()
    {
        reverse = true;
        vertical = true;
    }
    public void Flip()
    {
        flip ^= true;
    }

    private Vector2Int Transform(Vector2Int coord)
    {
        if (reverse)
            coord.x *= -1;
        if (flip)
            coord.y *= -1;
        if (vertical)
            coord = new Vector2Int(coord.y, coord.x);
        return coord;
    }

    private Dictionary<int, bool> cachedOutputs;
    private Dictionary<int, int> cycles;

    public virtual bool CycleSafe => false; //this should equal nodesIn.Count
    public virtual bool AllowCycle => CycleSafe; //this should equal nodesIn.Count
    public virtual int InPortCount => localInputTiles.Length; //this should equal nodesIn.Count
    public virtual int OutPortCount => localOutputTiles.Length;

    public bool inCycle;

    protected bool RemoveCached(int i) => cachedOutputs.Remove(i);

    public virtual void PrePropagate()
    {
        cachedOutputs = new Dictionary<int, bool>(OutPortCount);
        inCycle = false;
        cycles = new Dictionary<int, int>(OutPortCount);
    }

    public bool GetCachedOutput(int i, out bool output)
    {
        output = false;
        return cachedOutputs != null && cachedOutputs.TryGetValue(i, out output);
    }

    public bool GetOutput(int i, bool noCycle)
    {
        if (GetCachedOutput(i, out bool output))
            return output;
        if (cycles.ContainsKey(i) && cycles[i] > 0 && !noCycle)
        {
            inCycle = true;
            //Allow each member of the cycle to detect it
            if (cycles[i] > 1)
                return false;
        }
        if (!CycleSafe)
            cycles[i] = cycles.ContainsKey(i) ? cycles[i] + 1 : 1;
        output = GenOutput(i, noCycle);
        cycles[i] = 0;
        cachedOutputs[i] = output;
        return output;
    }
    /// <summary>
    /// MUST get the output of every node in when called, so that all nodes connected to an output are updated.
    /// </summary>
    protected abstract bool GenOutput(int i, bool noCycle);
}
