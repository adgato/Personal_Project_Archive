using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct CostFunction
{
    public enum Name { MeanSquaredError, CrossEntropy }

    public Name name;

    public CostFunction(Name name)
    {
        this.name = name;
    }

    public float Apply(Matrix output_batch, Matrix label_batch)
    {
        return name switch
        {
            Name.MeanSquaredError => Matrix.Apply(MeanSquaredError, output_batch, label_batch).Mean(),

            Name.CrossEntropy => Matrix.Apply(CrossEntropy, output_batch, label_batch).Mean(),

            _ => default
        };
    }

    public Matrix ApplyPrime(Matrix output_batch, Matrix label_batch)
    {
        return name switch
        {
            Name.MeanSquaredError => Matrix.Apply(MeanSquaredErrorPrime, output_batch, label_batch),

            Name.CrossEntropy => Matrix.Apply(CrossEntropyPrime, output_batch, label_batch),

            _ => default
        };
    }

    private float MeanSquaredError(float x, float l)
    {
        return 0.5f * (x - l) * (x - l);
    }
    private float MeanSquaredErrorPrime(float x, float l)
    {
        return x - l;
    }

    private float CrossEntropy(float x, float l)
    {
        return l > 0.5f ? x * (Mathf.Log(x) - 1) + 1 : x - (x - 1) * Mathf.Log(1 - x); //Only valid for l = {0, 1}
    }
    private float CrossEntropyPrime(float x, float l)
    {
        return Mathf.Clamp(l > 0.5f ? Mathf.Log(x) : -Mathf.Log(1 - x), -10, 10); //Only valid for l = {0, 1}
    }
}
