using System;
using static Tensorflow.Binding;

using Tensorflow;
using Tensorflow.Keras;
using Tensorflow.Keras.Models;
using Tensorflow.Keras.Layers;

public class Network
{
    public static void Main(string[] args)
    {
        var model = new tf.model.Sequential();
        model.Add(new Dense(12, activation: "relu", input_dim: 5));
        model.Add(new Dense(8, activation: "relu"));
        model.Add(new Dense(3, activation: "softmax"));

        model.compile(optimizer: "adam", loss: "mean_squared_error", metrics: new[] { "accuracy" });

        Console.WriteLine("Model Built.");
    }
}
