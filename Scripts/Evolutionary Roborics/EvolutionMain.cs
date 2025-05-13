using MyProject.Algorithms;
using MyProject.NeuralNetwork;
namespace MyProject;
using System;

    class Program
    {
        static void Main(string[] args)
        {   

            float[] sensorInput = [3.0f,4.0f,1.0f,3.0f];
            float[] motorVelocity = [0.2f,1.4f];

            //Initialize Neural Network 
            Model model = new Model(sensorInput.Length,motorVelocity.Length);
            
            // Network model = new Network;
            // model.run();

            int k = 5;
            int populationSize= 100; 
            Console.Write("weights" +model.countNeurons());
            //int populationSize = 10;
            int numOfWeights = model.countNeurons(); // num of weights of NN determines the size of our Genomes
            float parentSelectionPersentage = 0.20f;
            float mutationRate = 0.02f;
        //     GeneticAlgorithm ga = new GeneticAlgorithm(populationSize, numOfWeights, k, parentSelectionPersentage, mutationRate);

        //     //Get the population and print genomes

        //     // Run the Genetic Algorithm
        //     int count = 1;
        //     while (count != 4)
        //     {
        //         Console.WriteLine("--------------");
        //         Console.WriteLine("round: " + count);
        //         var population = ga.population.Genomes;
        //         for (int i = 0; i < population.Length; i++)
        //         {
        //             Console.WriteLine($"Genome {i + 1}:");
        //             float[] weights = population[i].weights;
        //             foreach (var weight in weights)
        //             {
        //                 Console.WriteLine(weight);
        //             }
        //         }

        //         ga.Run();
        //         Console.WriteLine("Populatioin: " + count + "best fit: " + ga.bestFitness);
        //         count++;
        //     }

         }
    }
