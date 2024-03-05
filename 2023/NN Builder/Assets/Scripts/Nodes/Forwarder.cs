using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Forwarder : Node
{
    public override string name { get { return nameof(Forwarder); } }
    public override Color colour { get { return new Color(0, 0, 0); } }
    public override int inputNo { get { return arcsIn.Length; } }
    public override int outputNo { get { return arcsOut.Length; } }

    private Batch[] forwardData;

    public void SetData(Batch[] forwardData)
    {
        this.forwardData = forwardData;
    }

    protected override Batch[] BackPropagate(Batch[] inputs)
    {
        return forwardData;
    }

    protected override Batch[] ForwardPropagate(Batch[] inputs)
    {
        return forwardData;
    }
}
