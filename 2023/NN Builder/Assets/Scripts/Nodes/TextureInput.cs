using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureInput : InputNode
{
    public Texture2D image = new Texture2D(0, 0);
    private Batch[] rbgaValues;

    public override string name { get { return nameof(TextureInput); } }
    public override int outputNo { get { return 4; } }

    protected override void SetDataset() { }

    protected override Batch[] GetData()
    {
        float[] r = new float[image.width * image.height];
        float[] g = new float[image.width * image.height];
        float[] b = new float[image.width * image.height];
        float[] a = new float[image.width * image.height];
        for (int x = 0; x < image.width; x++)
        {
            for (int y = 0; y < image.height; y++)
            {
                Color color = image.GetPixel(x, y);
                r[x * image.height + y] = color.r;
                g[x * image.height + y] = color.g;
                b[x * image.height + y] = color.b;
                a[x * image.height + y] = color.a;
            }
        }

        Batch R = Batch.New();
        Batch G = Batch.New();
        Batch B = Batch.New();
        Batch A = Batch.New();

        R.samples[0] = new Matrix(r, new Vector2Int(image.width, image.height));
        G.samples[0] = new Matrix(g, new Vector2Int(image.width, image.height));
        B.samples[0] = new Matrix(b, new Vector2Int(image.width, image.height));
        A.samples[0] = new Matrix(a, new Vector2Int(image.width, image.height));

        rbgaValues = new Batch[4] { R, G, B, A };

        return rbgaValues;
    }
}
