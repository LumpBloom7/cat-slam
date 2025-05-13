
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch.nn;
public class Model
{
    public Sequential seq;
    public Model(int inputDimension, int outputDimension)
    {
        Linear lin1 = Linear(inputSize: inputDimension, outputSize: 12);
        Linear lin2 = Linear(inputSize: 12, outputSize: 8);
        Linear lin3 = Linear(inputSize: 8, outputSize: outputDimension);
        ReLU relu = ReLU();
        var tanh = Tanh();
        seq = Sequential(lin1, relu, lin2, relu, lin3, tanh);
    }
    public int countNeurons()
    {
        int totalWeights = 0;
        foreach (var parameter in seq.named_parameters())
        {
            if (parameter.name.Contains("weight"))
            {
                //Console.WriteLine("--------");
                //Console.Write("parameter name: "+parameter.name + "amount "+ parameter.parameter.numel());
                totalWeights += (int)parameter.parameter.numel();
            }
        }
        return totalWeights;
    }

    public void setWeights(float[] externalWeights)
    {
        int index = 0;

        // Iterate through all layers in the Sequential container
        foreach (var layer in seq.modules())
        {
            if (layer is Linear linearLayer)
            {
                var weight = linearLayer.weight;
                //Console.Write("---------------");
                //Console.Write("inner weight count: "+weight.shape[0]*weight.shape[1]);
                int numWeights = (int)(weight.shape[0] * weight.shape[1]);
                int outputSize = (int)weight.shape[0];
                int inputSize = (int)weight.shape[1];
                //Create a tensor from the external weights and reshape it to the correct shape
                var weightTensor = torch.tensor(externalWeights[index..(index + numWeights)]).reshape([outputSize, inputSize]);
                //Use copy_() to assign the weights to the layer's weight tensor
                linearLayer.weight.copy_(weightTensor);
                index += numWeights;
            }
        }
    }

}
