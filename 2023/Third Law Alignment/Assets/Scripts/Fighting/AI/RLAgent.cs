using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RLAgent
{
    [System.Serializable]
    private struct Layer
    {
        public int size;
        public ActivationFunction activation;
        public Matrix weights;
        public Matrix bias;
    }
    [System.Serializable]
    public struct HyperParameters 
    {
        public int inputSize;
        public int prevStateCapacity;
        public float learningRate;

        public HyperParameters(int inputSize, int prevStateCapacity, int learningRate)
        {
            this.inputSize = inputSize;
            this.prevStateCapacity = prevStateCapacity;
            this.learningRate = learningRate;
        }
    }

    [SerializeField] private HyperParameters hyperParams;
    [SerializeField] private Layer[] layers;

    private Queue<Matrix[]> prevLayerStates;

    public void Initialise(RLAgent parent, float variation)
    {
        hyperParams = parent.hyperParams;
        layers = parent.layers;
        if (variation != 0)
            Mutate(variation);
    }
    public void Initialise(HyperParameters hyperParams, float variation)
    {
        this.hyperParams = hyperParams;
        for (int i = 0; i < layers.Length; i++)
        {
            layers[i].weights = Matrix.Random(new Vector2Int(layers[i].size, i == 0 ? hyperParams.inputSize : layers[i - 1].size), -variation, variation, new Rand());
            layers[i].bias = Matrix.Random(new Vector2Int(layers[i].size, 0), -variation, variation, new Rand());
        }
    }

    public void Mutate(float variation)
    {
        for (int i = 0; i < layers.Length; i++)
        {
            layers[i].weights += Matrix.Random(layers[i].weights.shape, -variation, variation, new Rand());
            layers[i].bias += Matrix.Random(layers[i].bias.shape, -variation, variation, new Rand());
        }
    }

    public float[] ForwardPropagate(float[] input, bool training = false)
    {
        if (input.Length != layers[0].size)
            Debug.LogError("Error, bad costPrime size: " + input.Length + " required: " + layers[0].size);

        Matrix current = new Matrix(input, new Vector2Int(hyperParams.inputSize, 1));

        int i = 0;
        Matrix[] layerStates = new Matrix[layers.Length + 1];
        layerStates[^1] = current;
        foreach (Layer layer in layers)
        {
            current = layer.activation.Apply(layer.weights * current + layer.bias);
            layerStates[i++] = current;
        }
        if (training)
        {
            prevLayerStates.Enqueue(layerStates);
            if (prevLayerStates.Count > hyperParams.inputSize)
                prevLayerStates.Dequeue();
        }

        return current.values;
    }
    public void BackPropagate(float[] costPrime)
    {
        if (costPrime.Length != layers[^1].size)
            Debug.LogError("Error, bad costPrime size: " + costPrime.Length + " required: " + layers[^1].size);

        Matrix[] weightCostsPrime = new Matrix[layers.Length];
        Matrix[] biasCostsPrime = new Matrix[layers.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            weightCostsPrime[i] = Matrix.Zeros(layers[i].weights.shape);
            biasCostsPrime[i] = Matrix.Zeros(layers[i].bias.shape);
        }

        float batchSize = prevLayerStates.Count;
        Matrix layerCost = new Matrix(costPrime, new Vector2Int(costPrime.Length, 1));
        while (prevLayerStates.TryDequeue(out Matrix[] layerState))
        {
            for (int i = layers.Length - 1; i >= 0; i--)
            {
                Matrix activationCostPrime = Matrix.Scale(layerCost, layers[i].activation.ApplyPrime(layerState[i]));

                layerCost = layers[i].weights.T * activationCostPrime;
                weightCostsPrime[i] += activationCostPrime * layerState[i == 0 ? ^1 : i - 1].T;
                biasCostsPrime[i] += activationCostPrime;
            }
        }
        float updateScale = hyperParams.learningRate / batchSize;
        for (int i = 0; i < layers.Length; i++)
        {
            layers[i].weights -= weightCostsPrime[i] * updateScale;
            layers[i].bias -= biasCostsPrime[i] * updateScale;
        }
    }
}
