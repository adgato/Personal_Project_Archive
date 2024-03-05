using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class InputNode : Node
{
    public override Color colour { get { return new Color(0, 0.6f, 1); } }
    public override int inputNo { get { return 0; } }

    protected abstract Batch[] GetData();
    protected abstract void SetDataset();

    protected override Batch[] ForwardPropagate(Batch[] inputs)
    {
        return GetData();
    }
    protected override Batch[] BackPropagate(Batch[] costs)
    {
        return costs;
    }

    public override void InitialiseForPropagation()
    {
        base.InitialiseForPropagation();
        SetDataset();
    }
}