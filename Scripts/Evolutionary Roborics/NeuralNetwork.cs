
using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Random;

public class Model
{
    private List<float[,]> linearLayers;

    private float[][] biases;

    private float[][] buffers;

    public int TotalWeights { get; private set; }

    public Model(int inputDimension, int outputDimension)
    {
        MathNet.Numerics.Control.MaxDegreeOfParallelism = 8;
        linearLayers = [
            new float[inputDimension, 12],
            new float[12, 8],
            new float[8, outputDimension] ];

        buffers = [
            new float[12],
            new float[8],
            new float[outputDimension]
        ];

        biases = [
            [..Random.Shared.NextDoubles(12).Select(x => (float)(((x * 2) - 1) * float.MaxValue))],
            [..Random.Shared.NextDoubles(8).Select(x => (float)(((x * 2) - 1) * float.MaxValue))],
            [..Random.Shared.NextDoubles(outputDimension).Select(x => (float)(((x * 2) - 1) * float.MaxValue))],
        ];

        TotalWeights = 0;

        foreach (float[,] ll in linearLayers)
            TotalWeights += ll.Length;
    }

    public float[] Forward(float[] X)
    {
        MatMultInto(X, linearLayers[0], buffers[0]);
        AddBiasInto(biases[0], buffers[0]);
        ReluInplace(buffers[0]);
        MatMultInto(buffers[0], linearLayers[1], buffers[1]);
        AddBiasInto(biases[1], buffers[1]);
        ReluInplace(buffers[1]);
        MatMultInto(buffers[1], linearLayers[2], buffers[2]);
        AddBiasInto(biases[2], buffers[2]);
        TanHInpalace(buffers[2]);

        return buffers[2];
    }

    public static void AddBiasInto(ReadOnlySpan<float> bias, Span<float> output)
    {

        for (int i = 0; i < output.Length; ++i)
            output[i] += bias[i];
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

