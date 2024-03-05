using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ActivationFunction
{
    public enum Name { Sigmoid, Relu, Tanh }

    public Name name;

    public ActivationFunction(Name name)
    {
        this.name = name;
    }

    public Matrix Apply(Matrix input)
    {
        return name switch
        {
            Name.Sigmoid => Matrix.Apply(Sigmoid, input),

            Name.Relu => Matrix.Apply(Relu, input),

            Name.Tanh => Matrix.Apply(Tanh, input),

            _ => default
        };
    }

    public Matrix ApplyPrime(Matrix output)
    {
        return name switch
        {
            Name.Sigmoid => Matrix.Apply(SigmoidPrime, output),

            Name.Relu => Matrix.Apply(ReluPrime, output),

            Name.Tanh => Matrix.Apply(TanhPrime, output),

            _ => default
        };
    }

    private float Sigmoid(float x)
    {
        return Mathf.Pow(1 + Mathf.Exp(-x), -1);
    }
    private float SigmoidPrime(float sigmoid_x)
    {
        return sigmoid_x * (1 - sigmoid_x);
    }

    private float Relu(float x)
    {
        return Mathf.Max(0, x);
    }
    private float ReluPrime(float relu_x)
    {
        return relu_x > 0 ? 1 : 0;
    }

    private float Tanh(float x)
    {
        return (Mathf.Exp(x) - Mathf.Exp(-x)) / (Mathf.Exp(x) + Mathf.Exp(-x));
    }
    private float TanhPrime(float tanh_x)
    {
        return 1 - tanh_x * tanh_x;
    }
}
