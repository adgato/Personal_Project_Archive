using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnInput : Node
{
    public OnInput() : base()
    {
        localOutputTiles = new Vector2Int[1] { Vector2Int.zero };
    }
    protected override bool GenOutput(int _,  bool __) => true;
}
public class OffInput : Node
{
    public OffInput() : base()
    {
        localOutputTiles = new Vector2Int[1] { Vector2Int.zero };
    }
    protected override bool GenOutput(int _,  bool __) => false;
}
public class HideOutput : Node
{
    public override int OutPortCount => 0;
    public HideOutput() : base()
    {
        NodesIn = new Socket[1];
        localInputTiles = new Vector2Int[1] { Vector2Int.zero };
        localOutputTiles = new Vector2Int[1] { Vector2Int.zero };
    }
    protected override bool GenOutput(int _,  bool __) => false;
}
public class Nand : Node
{
    public Nand() : base()
    {
        NodesIn = new Socket[2];
        localInputTiles = new Vector2Int[2] { Vector2Int.up, Vector2Int.down };
        localOutputTiles = new Vector2Int[1] { Vector2Int.right };
    }
    protected override bool GenOutput(int _,  bool noCycle) => !(NodesIn[0].GetOutput( noCycle) & NodesIn[1].GetOutput( noCycle)); //single & here, as we want to update nodes on both sides
}
public class Wire : Node
{
    public Wire() : base()
    {
        NodesIn = new Socket[1];
        localInputTiles = new Vector2Int[1] { Vector2Int.zero };
        localOutputTiles = new Vector2Int[1] { Vector2Int.right };
    }
    protected override bool GenOutput(int _,  bool noCycle) => NodesIn[0].GetOutput( noCycle);
}
public class JumpWire : Node
{
    public JumpWire() : base()
    {
        NodesIn = new Socket[1];
        localInputTiles = new Vector2Int[1] { Vector2Int.left };
        localOutputTiles = new Vector2Int[1] { Vector2Int.right };
    }
    protected override bool GenOutput(int _,  bool noCycle) => NodesIn[0].GetOutput( noCycle);
}
public class Repeater : Node
{
    bool state;
    bool cycle = false;
    public override bool CycleSafe => true;
    public Repeater() : base()
    {
        NodesIn = new Socket[1];
        localInputTiles = new Vector2Int[1] { Vector2Int.zero };
        localOutputTiles = new Vector2Int[1] { Vector2Int.right };
    }
    protected override bool GenOutput(int _,  bool noCycle)
    {
        bool output = state;
        if (!cycle)
        {
            cycle = true;
            noCycle = false;
            bool nextCycle = true;
            state = NodesIn[0].GetOutput( nextCycle);
            cycle = false;
        }
        return output;
    }
}