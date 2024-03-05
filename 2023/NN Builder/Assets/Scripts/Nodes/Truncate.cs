using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Truncate : Node
{
    public override string name { get { return nameof(Truncate); } }
    public override Color colour { get { return new Color(1, 0, 0.6f); } }
    public override int inputNo { get { return 1; } }
    public override int outputNo { get { return 1; } }

    private Vector2Int inputShape;

    protected override Batch[] BackPropagate(Batch[] costs)
    {
        Batch original = Batch.New();

        for (int b = 0; b < Batch.size; b++)
            original.samples[b] = new Matrix(costs[0].samples[b].values, inputShape);

        return new Batch[1] { original };
    }

    protected override Batch[] ForwardPropagate(Batch[] inputs)
    {
        inputShape = inputs[0].samples[0].shape;

        Batch truncated = Batch.New();

        for (int b = 0; b < Batch.size; b++)
            truncated.samples[b] = new Matrix(inputs[0].samples[b].values, new Vector2Int(inputShape.x * inputShape.y, 1));

        return new Batch[1] { truncated };
    }
}
