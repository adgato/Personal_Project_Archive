using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CostOutput : OutputNode
{
    public override string name { get { return nameof(CostOutput); } }
    public override int inputNo { get { return 2; } }

    public CostFunction.Name cost;

    private CostFunction costFunction;

    private Batch currentOutput;
    private Batch currentLabel;


    private List<float> allCosts;
    private float maxCost = float.MinValue;

    protected override Batch[] ForwardPropagate(Batch[] inputs)
    {
        currentOutput = inputs[0];
        currentLabel = inputs[1];

        return inputs;
    }
    protected override Batch[] BackPropagate(Batch[] costs)
    {
        float batchCost = 0;

        Batch primeBatch = Batch.New();
        for (int b = 0; b < Batch.size; b++)
        {
            primeBatch.samples[b] = costFunction.ApplyPrime(currentOutput.samples[b], currentLabel.samples[b]);

            batchCost += costFunction.Apply(currentOutput.samples[b], currentLabel.samples[b]);
        }

        allCosts.Add(batchCost / Batch.size);
        maxCost = Mathf.Max(batchCost / Batch.size, maxCost);

        if (instance.showData)
            ShowData();

        return new Batch[2] { primeBatch, Batch.New() };
    }
    public void ShowData()
    {
        float[] costGraph = new float[10_000];

        for (int i = 0; i < 100; i++)
        {
            float[] point = new float[100];

            point[Mathf.FloorToInt(allCosts[Mathf.FloorToInt(i / 100f * allCosts.Count)] / maxCost * 99)] = 1;

            point.CopyTo(costGraph, 100 * i);
        }

        Batch image = Batch.New();
        image.samples[0] = new Matrix(costGraph, new Vector2Int(100, 100)).T;

        output = new Batch[1] { image };
    }

    public override bool[] Correct()
    {
        bool[] scores = new bool[Batch.size];

        for (int b = 0; b < Batch.size; b++)
        {
            Vector2 maxOutput = new Vector2(-1, -1);
            for (int i = 0; i < currentOutput.samples[b].values.Length; i++)
            {
                if (currentOutput.samples[b].Get(i, 0) > maxOutput.y)
                    maxOutput = new Vector2(i, currentOutput.samples[b].Get(i, 0));
            }
            scores[b] = currentLabel.samples[b].Get((int)maxOutput.x, 0) == 1;
        }

        return scores;
    }

    public override void Load(string directory, ref List<Arc> arcs)
    {
        base.Load(directory, ref arcs);

        costFunction = JsonSaver.LoadData<CostFunction>(directory + "/cost");
        cost = costFunction.name;
    }
    public override void Save(string directory)
    {
        base.Save(directory);

        JsonSaver.SaveData(directory + "/cost", new CostFunction(cost));
    }

    public override void InitialiseForPropagation()
    {
        base.InitialiseForPropagation();

        allCosts = new List<float>();
        costFunction = new CostFunction(cost);
    }
}
