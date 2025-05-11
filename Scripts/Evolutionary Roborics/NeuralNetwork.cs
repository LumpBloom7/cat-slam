using System;
using Tensorflow;
using Tensorflow.Keras;
using Tensorflow.Keras.Layers;
using Tensorflow.Keras.Models;
using static Tensorflow.Binding;

public class Network
{
    public static void Main(string[] args)
    {
        var activations = new Activations();
        var model = new Tensorflow.Keras.Engine.Sequential(
            new Tensorflow.Keras.ArgsDefinition.SequentialArgs
            {
                Layers = [
                    new Dense(new Tensorflow.Keras.ArgsDefinition.DenseArgs{
                        Units = 12,
                        Activation =  activations.Relu,
                        InputShape = new Shape(5,5)
                    }),
                    new Dense(new Tensorflow.Keras.ArgsDefinition.DenseArgs{
                        Units = 8,
                        Activation =  activations.Relu
                    }),
                    new Dense(new Tensorflow.Keras.ArgsDefinition.DenseArgs{
                        Units = 3,
                        Activation =  activations.Softmax,
                    })
                ]
            }
        );

        model.compile(optimizer: "adam", loss: "mean_squared_error", metrics: ["accuracy"]);

        Console.WriteLine("Model Built.");
    }
}
