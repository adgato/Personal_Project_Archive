using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MnistInput : InputNode
{
    public override string name { get { return nameof(MnistInput); } }
    public override int outputNo { get { return 2; } }

    private List<string> digits;

    protected override void SetDataset()
    {
        TextAsset dataset = Resources.Load<TextAsset>("mnist_784");

        digits = dataset.text.Split('\n').ToList();
        digits.RemoveAt(0);

        Resources.UnloadAsset(dataset);
    }

    protected override Batch[] GetData()
    {
        if (iterations * Batch.size >= digits.Count - 1)
            Network.isTraining = false;

        Batch input = Batch.New();
        Batch label = Batch.New();

        for (int b = 0; b < Batch.size; b++)
        {
            string labelStr = digits[iterations * Batch.size + b].Split(',').Last();
            input.samples[b] = new Matrix(digits[iterations * Batch.size + b].Split(',').Where(value => value != labelStr).Select(value => float.Parse(value) / 255).ToArray(), new Vector2Int(28, 28)).Reflect(true, false);
            label.samples[b] = Base1Label(int.Parse(labelStr));
        }

        return new Batch[2] { input, label };
    }

    private Matrix Base1Label(int label)
    {
        return new Matrix(
            new float[10]
            {
                label == 0 ? 1 : 0,
                label == 1 ? 1 : 0,
                label == 2 ? 1 : 0,
                label == 3 ? 1 : 0,
                label == 4 ? 1 : 0,
                label == 5 ? 1 : 0,
                label == 6 ? 1 : 0,
                label == 7 ? 1 : 0,
                label == 8 ? 1 : 0,
                label == 9 ? 1 : 0
            },
            new Vector2Int(10, 1)
        );
    }
}
