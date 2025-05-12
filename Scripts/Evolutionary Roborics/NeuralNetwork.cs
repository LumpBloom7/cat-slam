using System;

using TorchSharp;
using static TorchSharp.torch.nn;

public class Network
{
    public static void Main(string[] args)
    {

        var lin1 = Linear(1000, 100);
        var lin2 = Linear(100, 10);
        var seq = Sequential(("lin1", lin1), ("relu1", ReLU()), ("drop1", Dropout(0.1)), ("lin2", lin2));

        using var x = torch.randn(64, 1000);
        using var y = torch.randn(64, 10);

        var optimizer = torch.optim.Adam(seq.parameters());

        for (int i = 0; i < 10; i++)
        {
            using var eval = seq.forward(x);
            using var output = functional.mse_loss(eval, y, Reduction.Sum);

            optimizer.zero_grad();

            output.backward();

            optimizer.step();
        }


        Console.WriteLine("Model Built.");
    }
}
