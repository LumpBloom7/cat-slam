
using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch.nn;


using Matrix = MathNet.Numerics.LinearAlgebra.Matrix<float>;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<float>;
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
    private Matrix[] linearLayers;

    private Vector inputBuffer;
    private float[] outputBuffer;

    public int TotalWeights { get; private set; }

    public Model(int inputDimension, int outputDimension)
    {
        linearLayers = [
            Matrix.Build.Dense(inputDimension, 12),
            Matrix.Build.Dense(12, 8),
            Matrix.Build.Dense(8, outputDimension)];

        inputBuffer = Vector.Build.Dense(inputDimension);
        outputBuffer = new float[outputDimension];

        TotalWeights = 0;

        foreach (var ll in linearLayers)
            TotalWeights += ll.ColumnCount * ll.RowCount;
    }

    public float[] Forward(float[] X) => Forward(X.AsSpan());

    public float[] Forward(ReadOnlySpan<float> X)
    {
        for (int i = 0; i < inputBuffer.Count; ++i)
            inputBuffer[i] = X[i];

        var ll = linearLayers;

        var output = TanHInpalace(ReluInplace(ReluInplace(inputBuffer * ll[0]) * ll[1]) * ll[2]);

        for (int i = 0; i < outputBuffer.Length; ++i)
            outputBuffer[i] = output[i];

        return outputBuffer;
    }

    public static Vector ReluInplace(Vector vec)
    {
        for (int i = 0; i < vec.Count; ++i)
            vec[i] = MathF.Max(0, vec[i]);

        return vec;
    }

    public static Vector TanHInpalace(Vector vec)
    {
        for (int i = 0; i < vec.Count; ++i)
            vec[i] = MathF.Tanh(vec[i]);

        return vec;
    }

    public void setWeights(ReadOnlySpan<float> externalWeights)
    {
        int index = 0;

        for (int i = 0; i < linearLayers.Length; ++i)
        {
            var layer = linearLayers[i];
            int numWeights = layer.ColumnCount * layer.RowCount;

            for (int j = 0; j < numWeights; ++j)
            {
                int row = j / layer.ColumnCount;
                int column = j - row * layer.ColumnCount;
                layer[row, column] = externalWeights[index + j];
            }

            index += numWeights;
        }
    }

}
