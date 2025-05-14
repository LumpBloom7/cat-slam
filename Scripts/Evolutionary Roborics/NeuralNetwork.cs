
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch.nn;
public class Model
{
    private Sequential seq;

    public Model(int inputDimension, int outputDimension)
    {
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
        seq.eval();
        var XBytes = MemoryMarshal.AsBytes(X);

        var evalBytes = evalBuffer.bytes;

        XBytes.CopyTo(evalBytes);
        return seq.forward(evalBuffer);
    }

    public torch.Tensor Forward(torch.Tensor X)
    {
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
