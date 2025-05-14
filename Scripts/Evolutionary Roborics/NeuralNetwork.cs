
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch.nn;


public class Model2
{
    private Sequential seq;

    public Model2(int inputDimension, int outputDimension)
    {
        if (torch.cuda_is_available())
            torch.set_default_device(torch.CUDA);

        Linear lin1 = Linear(inputSize: inputDimension, outputSize: 12);
        Linear lin2 = Linear(inputSize: 12, outputSize: 8);
        Linear lin3 = Linear(inputSize: 8, outputSize: outputDimension);

        linearLayers = [lin1, lin2, lin3];

        ReLU relu = ReLU();
        var tanh = Tanh();
        seq = Sequential(lin1, relu, lin2, relu, lin3, tanh);

        cacheWeightInfo();

        evalBuffer = torch.zeros(inputDimension, torch.ScalarType.Float32);
    }

    private torch.Tensor evalBuffer;

    public torch.Tensor Forward(float[] X) => Forward(X.AsSpan());

    public torch.Tensor Forward(ReadOnlySpan<float> X)
    {
        using var ctx = torch.no_grad();
        seq.eval();
        var XBytes = MemoryMarshal.AsBytes(X);

        var evalBytes = evalBuffer.bytes;

        XBytes.CopyTo(evalBytes);
        return seq.forward(evalBuffer);
    }

    public torch.Tensor Forward(torch.Tensor X)
    {
        using var ctx = torch.no_grad();
        seq.eval();
        return seq.forward(X);
    }

    public int TotalWeights { get; private set; }

    private Linear[] linearLayers;
    private long[][] layerShapes = null!;

    public void cacheWeightInfo()
    {
        layerShapes = [.. linearLayers.Select(l => l.weight.shape)];

        TotalWeights = layerShapes.Select(s => (int)(s[0] * s[1])).Sum();
    }

    public void setWeights(float[] externalWeights)
    {

        using var d0 = torch.NewDisposeScope();
        ReadOnlySpan<float> wSpan = externalWeights;
        // Iterate through all layers in the Sequential container

        int index = 0;

        for (int i = 0; i < linearLayers.Length; ++i)
        {
            long[] shape = layerShapes[i];
            int numWeights = (int)(shape[0] * shape[1]);

            var layerWeights = MemoryMarshal.AsBytes(wSpan.Slice(index, numWeights));

            var weightT = linearLayers[i].weight.bytes;

            layerWeights.CopyTo(weightT);

            index += numWeights;
        }
    }
}


public class Model
{
    private List<float[,]> linearLayers;

    private float[][] buffers;

    public int TotalWeights { get; private set; }

    public Model(int inputDimension, int outputDimension)
    {
        MathNet.Numerics.Control.MaxDegreeOfParallelism = 8;
        linearLayers = [
            new float[inputDimension, 12],
            new float[12, 8],
            new float[8, 2] ];

        buffers = [
            new float[12],
            new float[8],
            new float[2]
        ];

        TotalWeights = 0;

        foreach (float[,] ll in linearLayers)
            TotalWeights += ll.Length;
    }

    public float[] Forward(float[] X)
    {
        MatMultInto(X, linearLayers[0], buffers[0]);
        ReluInplace(buffers[0]);
        MatMultInto(buffers[0], linearLayers[1], buffers[1]);
        ReluInplace(buffers[1]);
        MatMultInto(buffers[1], linearLayers[2], buffers[2]);
        TanHInpalace(buffers[2]);

        return buffers[2];
    }

    public static void MatMultInto(ReadOnlySpan<float> input, float[,] right, Span<float> output)
    {
        output.Clear();

        for (int r = 0; r < input.Length; ++r)
        {
            if (input[r] == 0)
                continue;

            for (int c = 0; c < output.Length; ++c)
                output[c] += input[r] * right[r, c];
        }
    }

    public static void ReluInplace(float[] vec)
    {
        for (int i = 0; i < vec.Length; ++i)
            vec[i] = MathF.Max(0, vec[i]);
    }

    public static void TanHInpalace(float[] vec)
    {
        for (int i = 0; i < vec.Length; ++i)
            vec[i] = MathF.Tanh(vec[i]);
    }

    public void setWeights(ReadOnlySpan<float> externalWeights)
    {
        int index = 0;

        for (int i = 0; i < linearLayers.Count; ++i)
        {
            float[,] layer = linearLayers[i];
            int numWeights = layer.Length;
            int columns = layer.GetLength(1);
            for (int j = 0; j < numWeights; ++j)
            {
                int row = j / columns;
                int column = j - row * columns;

                layer[row, column] = externalWeights[index + j];
            }
            index += numWeights;
        }
    }
}

