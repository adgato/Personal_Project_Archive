using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Batch
{
    public static int size;
    public Matrix[] samples;

    public Batch(Matrix[] samples)
    {
        if (samples.Length != size)
            Debug.LogError("Error: samples.Length does not match Batch.size");

        this.samples = samples;
    }
    public static Batch New()
    {
        return new Batch(new Matrix[size]);
    }
}

public struct Matrix
{
    public float[] values { get; private set; }
    public float Get(int x, int y) => values[x * shape.y + y];
    public Vector2Int shape { get; private set; }
    public Matrix T { get { return Transpose(); } }


    [System.Serializable]
    private struct MatrixSaveFormat 
    {
        public float[] data;
        public Vector2Int shape;

        public MatrixSaveFormat(float[] values, Vector2Int shape)
        {
            this.shape = shape;
            data = values;
        }
    }

    //public Matrix(float[,] values)
    //{
    //    this.values = values;
    //    shape = new Vector2Int(values.GetLength(0), values.GetLength(1));
    //}
    public Matrix(float[] values, Vector2Int shape)
    {
        this.values = values;
        this.shape = shape;
    }

    public Matrix Reflect(bool horizontally, bool vertically)
    {
        float[] new_values = new float[values.Length];
        for (int i = 0; i < shape.x; i++)
        {
            int x = horizontally ? shape.x - 1 - i : i;
            for (int j = 0; j < shape.y; j++)
            {
                int y = vertically ? shape.y - 1 - j : j;
                new_values[x * shape.y + y] = Get(i, j);
            }
        }
        return new Matrix(new_values, shape);
    }

    public Matrix Transpose()
    {
        float[] new_values = new float[values.Length];
        for (int x = 0; x < shape.x; x++)
        {
            for (int y = 0; y < shape.y; y++)
                new_values[y * shape.x + x] = Get(x, y);
        }
        return new Matrix(new_values, new Vector2Int(shape.y, shape.x));
    }

    public float Sum()
    {
        float total = 0;
        for (int i = 0; i < values.Length; i++)
            total += values[i];
        return total;
    }
    public float Mean()
    {
        return Sum() / values.Length;
    }

    public Matrix Clone()
    {
        return new Matrix(values, shape);
    }
    public void Log()
    {
        string log = "\nMatrix:\n";
        for (int x = 0; x < shape.x; x++)
        {
            log += "[ ";
            for (int y = 0; y < shape.y; y++)
                log += Get(x, y) + ", ";
            log = log.Remove(log.Length - 2);
            log += " ]\n";
        }
        Debug.Log(log);
    }

    public void SaveAs(string filename)
    {
        JsonSaver.SaveData(filename, new MatrixSaveFormat(values, shape));
    }
    public static Matrix Load(string filename)
    {
        MatrixSaveFormat matrixSave = JsonSaver.LoadData<MatrixSaveFormat>(filename);
        return new Matrix(matrixSave.data, matrixSave.shape);
    }

    public static Matrix Random(Vector2Int shape, float min, float max, Rand rand)
    {
        float[] values = new float[shape.x * shape.y];
        for (int x = 0; x < shape.x; x++)
        {
            for (int y = 0; y < shape.y; y++)
                values[x * shape.y + y] = rand.Range(min, max);
        }
        return new Matrix(values, shape);
    }
    public static Matrix Number(Vector2Int shape, float number)
    {
        float[] values = new float[shape.x * shape.y];
        for (int x = 0; x < shape.x; x++)
        {
            for (int y = 0; y < shape.y; y++)
                values[x * shape.y + y] = number;
        }
        return new Matrix(values, shape);
    }
    public static Matrix Zeros(Vector2Int shape)
    {
        return Number(shape, 0);
    }
    public static Matrix Ones(Vector2Int shape)
    {
        return Number(shape, 1);
    }

    public static Matrix Apply(System.Func<float, float> Function, Matrix a)
    {
        float[] values = new float[a.values.Length];

        for (int x = 0; x < a.shape.x; x++)
        {
            for (int y = 0; y < a.shape.y; y++)
                values[x * a.shape.y + y] = Function(a.Get(x, y));
        }

        return new Matrix(values, a.shape);
    }
    public static Matrix Apply(System.Func<float, float, float> Function, Matrix a, Matrix b)
    {
        if (a.shape != b.shape)
        {
            Debug.LogError("Error: Matrix 1 must have the same shape as Matrix 2. Currently: " + a.shape + " and " + b.shape);
            return default;
        }

        float[] values = new float[a.values.Length];

        for (int x = 0; x < a.shape.x; x++)
        {
            for (int y = 0; y < a.shape.y; y++)
                values[x * a.shape.y + y] = Function(a.Get(x, y), b.Get(x, y));
        }

        return new Matrix(values, a.shape);
    }
    public static Matrix Apply(System.Func<float, float, float> Function, Matrix a, float b)
    {
        float[] values = new float[a.values.Length];

        for (int x = 0; x < a.shape.x; x++)
        {
            for (int y = 0; y < a.shape.y; y++)
                values[x * a.shape.y + y] = Function(a.Get(x, y), b);
        }

        return new Matrix(values, a.shape);
    }
    public static Matrix Apply(System.Func<float, float, float> Function, float a, Matrix b)
    {
        float[] values = new float[b.values.Length];

        for (int x = 0; x < b.shape.x; x++)
        {       
            for (int y = 0; y < b.shape.y; y++)
                values[x * b.shape.y + y] = Function(a, b.Get(x, y));
        }

        return new Matrix(values, b.shape);
    }

    public static Matrix Dot(Matrix a, Matrix b)
    {
        if (a.shape.y != b.shape.x)
        {
            Debug.LogError("Error: Matrix 1 must have the same Y-dimension size as Matrix 2's X-dimension size. Currently: " + a.shape + " dot " + b.shape);
            return default;
        }

        float[] values = new float[a.shape.x * b.shape.y];
        for (int x = 0; x < a.shape.x; x++)
        {
            for (int y = 0; y < b.shape.y; y++)
            {
                float sum = 0;
                for (int i = 0; i < a.shape.y; i++)
                    sum += a.Get(x, i) * b.Get(i, y);

                values[x * b.shape.y + y] = sum;
            }
        }
        return new Matrix(values, new Vector2Int(a.shape.x, b.shape.y));
    }

    private static float Add(float a, float b) => a + b;
    private static float Subtract(float a, float b) => a - b;
    private static float Multiply(float a, float b) => a * b;
    private static float Divide(float a, float b) => a / b;

    public static Matrix operator *(Matrix a, Matrix b) => Dot(a, b);
    public static Matrix Scale(Matrix a, Matrix b) => Apply(Multiply, a, b);
    public static Matrix operator *(Matrix a, float b) => Apply(Multiply, a, b);
    public static Matrix operator *(float a, Matrix b) => Apply(Multiply, a, b);

    public static Matrix operator +(Matrix a) => a;
    public static Matrix operator -(Matrix a) => -1 * a;

    public static Matrix operator /(Matrix a, Matrix b) => Apply(Divide, a, b);
    public static Matrix operator /(Matrix a, float b) => Apply(Divide, a, b);
    public static Matrix operator /(float a, Matrix b) => Apply(Divide, a, b);

    public static Matrix operator +(Matrix a, Matrix b) => Apply(Add, a, b);
    public static Matrix operator +(Matrix a, float b) => Apply(Add, a, b);
    public static Matrix operator +(float a, Matrix b) => Apply(Add, a, b);

    public static Matrix operator -(Matrix a, Matrix b) => Apply(Subtract, a, b);
    public static Matrix operator -(Matrix a, float b) => Apply(Subtract, a, b);
    public static Matrix operator -(float a, Matrix b) => Apply(Subtract, a, b);
}
