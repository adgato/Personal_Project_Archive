using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Layer : Node
{
    public override string name { get { return nameof(Layer); } }
    public override Color colour { get { return new Color(1, 0.6f, 0); } }
    public override int inputNo { get { return 1; } }
    public override int outputNo { get { return 1; } }

    private Matrix weights;
    private Matrix bias;


    private Batch currentInput;
    private Batch currentOutput;

    private ActivationFunction activationFunction;

    public bool useGPU = true;
    private ComputeShader forwardPropagationGPU;
    private ComputeShader backPropagationGPU;

    public Vector2Int inputShape = Vector2Int.one;
    public Vector2Int outputShape = Vector2Int.one;
    public ActivationFunction.Name activation;

    protected override Batch[] ForwardPropagate(Batch[] input)
    {
        if (input[0].samples[0].shape != inputShape)
            Debug.LogError("Error: bad input shape. Given: " + input[0].samples[0].shape + ". Expected: " + inputShape);

        if (useGPU)
            return ForwardPropagateGPU(input);

        currentInput = input[0];

        currentOutput = Batch.New();

        for (int b = 0; b < Batch.size; b++)
        {
            currentOutput.samples[b] = activationFunction.Apply(weights * currentInput.samples[b] + bias);
        }

        return new Batch[1] { currentOutput };
    }

    protected override Batch[] BackPropagate(Batch[] outputCostPrime)
    {
        if (outputCostPrime[0].samples[0].shape != outputShape)
            Debug.LogError("Error: bad input shape. Given: " + outputCostPrime[0].samples[0].shape + ". Expected: " + outputShape);

        if (useGPU)
            return BackPropagateGPU(outputCostPrime);

        Batch inputCostPrime = Batch.New();

        Matrix weightCostPrime = Matrix.Zeros(weights.shape);
        Matrix biasCostPrime = Matrix.Zeros(bias.shape);

        for (int b = 0; b < Batch.size; b++)
        {
            Matrix activationCostPrime = Matrix.Scale(outputCostPrime[0].samples[b], activationFunction.ApplyPrime(currentOutput.samples[b]));

            inputCostPrime.samples[b] = weights.T * activationCostPrime;

            weightCostPrime += activationCostPrime * currentInput.samples[b].T;
            biasCostPrime += activationCostPrime;
        }

        weights -= weightCostPrime * (QuietNetwork.learning_rate / Batch.size);
        bias -= biasCostPrime * (QuietNetwork.learning_rate / Batch.size);

        return new Batch[1] { inputCostPrime };
    }

    public override void New()
    {
        base.New();
        weights = new Matrix(new float[0], Vector2Int.zero);
        bias = new Matrix(new float[0], Vector2Int.zero);
    }

    public override void Load(string directory, ref List<Arc> arcs)
    {
        base.Load(directory, ref arcs);

        weights = Matrix.Load(directory + "/weights");
        bias = Matrix.Load(directory + "/bias");
        activationFunction = JsonSaver.LoadData<ActivationFunction>(directory + "/activation");

        inputShape = new Vector2Int(weights.shape.y, 1);
        outputShape = new Vector2Int(weights.shape.x, 1);
        activation = activationFunction.name;
    }
    public override void Save(string directory)
    {
        base.Save(directory);

        InitialiseForPropagation();

        weights.SaveAs(directory + "/weights");
        bias.SaveAs(directory + "/bias");

        JsonSaver.SaveData(directory + "/activation", new ActivationFunction(activation));
    }

    public override void InitialiseForPropagation()
    {
        base.InitialiseForPropagation();

        activationFunction = new ActivationFunction(activation);

        Vector2Int new_shape = new Vector2Int(outputShape.x, inputShape.x);
        if (weights.shape != new_shape)
        {
            weights = Matrix.Random(new_shape, -1, 1, Rand.stream);
            bias = Matrix.Random(outputShape, -1, 1, Rand.stream);
        }

        if (useGPU)
        {
            forwardPropagationGPU = Resources.Load<ComputeShader>("LayerForward");
            backPropagationGPU = Resources.Load<ComputeShader>("LayerBack");
        }
    }

    private Batch[] ForwardPropagateGPU(Batch[] input)
    {
        int handle = forwardPropagationGPU.FindKernel("ForwardPropagate");

        ComputeBuffer inputBuffer = new ComputeBuffer(inputShape.x * Batch.size, sizeof(float));
        ComputeBuffer outputBuffer = new ComputeBuffer(outputShape.x * Batch.size, sizeof(float));
        ComputeBuffer weightsBuffer = new ComputeBuffer(weights.values.Length, sizeof(float));
        ComputeBuffer biasBuffer = new ComputeBuffer(bias.values.Length, sizeof(float));

        inputBuffer.SetData(BatchToBuffer(input[0]));
        weightsBuffer.SetData(weights.values);
        biasBuffer.SetData(bias.values);

        forwardPropagationGPU.SetBuffer(handle, "input", inputBuffer);
        forwardPropagationGPU.SetBuffer(handle, "output", outputBuffer);
        forwardPropagationGPU.SetBuffer(handle, "weights", weightsBuffer);
        forwardPropagationGPU.SetBuffer(handle, "bias", biasBuffer);

        forwardPropagationGPU.SetInt("inputSize", inputShape.x);
        forwardPropagationGPU.SetInt("outputSize", outputShape.x);
        forwardPropagationGPU.SetInt("batchSize", Batch.size);
        forwardPropagationGPU.SetInt("activationFunction", (int)activationFunction.name);

        forwardPropagationGPU.Dispatch(handle, 1, 1, 1);

        float[] buffer = new float[outputShape.x * Batch.size];
        outputBuffer.GetData(buffer);

        currentInput = input[0];
        currentOutput = BufferToBatch(buffer, outputShape);

        inputBuffer.Dispose();
        outputBuffer.Dispose();
        weightsBuffer.Dispose();
        biasBuffer.Dispose();

        return new Batch[1] { currentOutput };
    }
    private Batch[] BackPropagateGPU(Batch[] outputCostPrime)
    {
        int handle = backPropagationGPU.FindKernel("BackPropagate");

        ComputeBuffer outputCostPrimeBuffer = new ComputeBuffer(outputShape.x * Batch.size, sizeof(float));
        ComputeBuffer inputCostPrimeBuffer = new ComputeBuffer(inputShape.x * Batch.size, sizeof(float));
        ComputeBuffer weightsBuffer = new ComputeBuffer(weights.values.Length, sizeof(float));
        ComputeBuffer biasBuffer = new ComputeBuffer(bias.values.Length, sizeof(float));
        ComputeBuffer currentOutputBuffer = new ComputeBuffer(outputShape.x * Batch.size, sizeof(float));
        ComputeBuffer currentInputBuffer = new ComputeBuffer(inputShape.x * Batch.size, sizeof(float));

        outputCostPrimeBuffer.SetData(BatchToBuffer(outputCostPrime[0]));
        weightsBuffer.SetData(weights.values);
        biasBuffer.SetData(bias.values);
        currentOutputBuffer.SetData(BatchToBuffer(currentOutput));
        currentInputBuffer.SetData(BatchToBuffer(currentInput));

        backPropagationGPU.SetBuffer(handle, "outputCostPrime", outputCostPrimeBuffer);
        backPropagationGPU.SetBuffer(handle, "inputCostPrime", inputCostPrimeBuffer);
        backPropagationGPU.SetBuffer(handle, "weights", weightsBuffer);
        backPropagationGPU.SetBuffer(handle, "bias", biasBuffer);
        backPropagationGPU.SetBuffer(handle, "currentOutput", currentOutputBuffer);
        backPropagationGPU.SetBuffer(handle, "currentInput", currentInputBuffer);

        backPropagationGPU.SetInt("inputSize", inputShape.x);
        backPropagationGPU.SetInt("outputSize", outputShape.x);
        backPropagationGPU.SetInt("batchSize", Batch.size);
        backPropagationGPU.SetInt("activationFunction", (int)activationFunction.name);
        backPropagationGPU.SetFloat("learningRate", QuietNetwork.learning_rate);

        backPropagationGPU.Dispatch(handle, 1, 1, 1);


        weightsBuffer.GetData(weights.values);
        biasBuffer.GetData(bias.values);

        float[] buffer = new float[inputShape.x * Batch.size];
        inputCostPrimeBuffer.GetData(buffer);

        outputCostPrimeBuffer.Dispose();
        inputCostPrimeBuffer.Dispose();
        weightsBuffer.Dispose();
        biasBuffer.Dispose();
        currentOutputBuffer.Dispose();
        currentInputBuffer.Dispose();

        return new Batch[1] { BufferToBatch(buffer, inputShape) };
    }

    //Courtesy of chatGPT :)
    public float[] BatchToBuffer(Batch batch)
    {
        int sampleLength = batch.samples[0].values.Length;
        int totalLength = batch.samples.Length * sampleLength;
        float[] concat = new float[totalLength];

        for (int i = 0; i < batch.samples.Length; i++)
            Buffer.BlockCopy(batch.samples[i].values, 0, concat, i * sampleLength * sizeof(float), sampleLength * sizeof(float));

        return concat;
    }
    //Lol it can even do it backwards :))
    public Batch BufferToBatch(float[] buffer, Vector2Int shape)
    {
        int sampleLength = shape.x * shape.y;
        int numSamples = buffer.Length / sampleLength;
        Matrix[] samples = new Matrix[numSamples];

        for (int i = 0; i < numSamples; i++)
        {
            float[] values = new float[sampleLength];
            Buffer.BlockCopy(buffer, i * sampleLength * sizeof(float), values, 0, sampleLength * sizeof(float));
            samples[i] = new Matrix(values, shape);
        }

        return new Batch(samples);
    }
}
