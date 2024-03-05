using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class OutputNode : Node
{
    public override Color colour { get { return new Color(0, 0.6f, 1); } }
    public override int outputNo { get { return 0; } }

    public abstract bool[] Correct();
}