using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TorchSharp;

public class GeneticAlgorithm
{
    private readonly int k;
    private readonly float parentSelectionPercentage;
    private readonly int genomeSize;
    private readonly float mutationRate;
    private Model model;
    public Genome? bestGenome = null;

    private int populationSize;

    private float[] neuralNetworkInputArray;
    public GeneticAlgorithm(int populationSize, int numberOfInputs, int tournamentSelectionSize, float parentSelectionPercentage, float mutationRate)
    {
        this.mutationRate = mutationRate;
        this.populationSize = populationSize;

        model = new Model(numberOfInputs, 2);
        genomeSize = model.TotalWeights;
        k = tournamentSelectionSize;
        this.parentSelectionPercentage = parentSelectionPercentage;
        neuralNetworkInputArray = new float[numberOfInputs];
        InitializePopulationRandomly(populationSize);
    }

    private Genome[] population;

    private void InitializePopulationRandomly(int populationSize)
    {
        population = new Genome[populationSize];
        for (int i = 0; i < populationSize; i++)
            population[i] = new Genome(genomeSize); // Initialize each genome with random weights
    }

    private float evaluate(Genome individual, SimulationProvider.SimulationContext ctx)
    {
        // Reset the environment
        ctx.Reset();

        ctx.SensorValues.CopyTo(neuralNetworkInputArray, 0);
        neuralNetworkInputArray[^2] = ctx.LeftVel;
        neuralNetworkInputArray[^1] = ctx.RightVel;

        //Define the NN
        model.setWeights(individual.weights); // assign the weights to out NN

        for (int i = 0; i < 10; ++i)
        {
            //perform step
            float[] data = model.Forward(neuralNetworkInputArray);

            ctx.Update((int)Math.Round(data[0]), (int)Math.Round(data[1])); //update enviroment

            //update neural network inputs for next step
            ctx.SensorValues.CopyTo(neuralNetworkInputArray, 0);
            neuralNetworkInputArray[^2] = ctx.LeftVel;
            neuralNetworkInputArray[^1] = ctx.RightVel;
        }


        return ctx.ComputeReward(); // placeholder
    }
    public (int, int) evaluateToGetAction(Genome individual, SimulationProvider.SimulationContext ctx)
    {
        ctx.Reset();

        //Here we get sensor inputs and motor velocities for input

        ctx.SensorValues.CopyTo(neuralNetworkInputArray, 0);
        neuralNetworkInputArray[^2] = ctx.LeftVel;
        neuralNetworkInputArray[^1] = ctx.RightVel;

        model.setWeights(individual.weights); // assign the weights to out NN
        float[] y = model.Forward(neuralNetworkInputArray);
        int leftMotorReading = (int)Math.Round(y[0]);
        int rightMotorReading = (int)Math.Round(y[1]);
        return (leftMotorReading, rightMotorReading); // placeholder
    }

    private Genome tournamentSelection()
    {
        Debug.Assert(population.Length > 0);

        Random.Shared.Shuffle(population);

        return population.Take(k).MaxBy(g => g.FitnessScore)!;
    }

    private HashSet<Genome> selectedParents = [];
    private HashSet<Genome> selectParents()
    {
        selectedParents.Clear();

        // Calculate 10% of the population size
        int numberOfParents = (int)(populationSize * parentSelectionPercentage);
        if (numberOfParents < 2)
            return selectedParents;

        while (selectedParents.Count < numberOfParents)
            selectedParents.Add(tournamentSelection());

        return selectedParents;
    }

    public float[] singlePointCrossover(float[] parent1, float[] parent2)
    {
        int length = parent1.Length;
        Random rand = Random.Shared;
        int crossoverPoint = rand.Next(1, length); //not 0 or length

        //Randomly choose which parent contributes the first half
        bool firstHalfFromParent1 = rand.NextDouble() < 0.5;
        float[] child = new float[length];

        for (int i = 0; i < length; i++)
        {
            if (i < crossoverPoint)
                child[i] = firstHalfFromParent1 ? parent1[i] : parent2[i];
            else
                child[i] = firstHalfFromParent1 ? parent2[i] : parent1[i];
        }

        return child;
    }

    public float[] mutate(float mutationRate, float[] childWeights)
    {
        Random random = Random.Shared;

        //Console.Write("in mutation");
        if (random.NextDouble() < mutationRate)
        {
            int index = random.Next(childWeights.Length);
            childWeights[index] += (float)(random.NextDouble() * 2 - 1); //Random between -1 and 1
        }
        return childWeights;
    }

    public void Run(SimulationProvider.SimulationContext ctx)
    {
        //select parents
        var parentsEnumerable = population.OrderByDescending(g => g.FitnessScore).Take(k);

        var parents = parentsEnumerable.ToArray();
        //Console.WriteLine("Parent length: " + parents.Length);

        // Create new population, include parents implicitly
        int i = 0;
        for (; i < parents.Length; ++i)
        {
            var parent = population[i] = parents[i];
            parent.FitnessScore = evaluate(parent, ctx);
        }

        Random rand = Random.Shared;

        for (; i < populationSize / 2; ++i)
        {
            var newcomer = population[i] = new Genome(genomeSize); // Initialize each genome with random weights
            newcomer.FitnessScore = evaluate(newcomer, ctx);
        }

        for (; i < populationSize; ++i)
        {
            //Console.WriteLine("population creation count: "+count);
            //Select two random parents
            Genome parent1 = parents[rand.Next(parents.Length)];
            Genome parent2 = parents[rand.Next(parents.Length)];
            //Avoid selecting same parent
            while (parent1 == parent2)
                parent2 = parents[rand.Next(parents.Length)];

            //Perform crossover
            float[] childWeights = singlePointCrossover(parent1.weights, parent2.weights);

            //Perform mutation
            childWeights = mutate(mutationRate: mutationRate, childWeights);

            //Create new genome
            Genome childGenome = new Genome(childWeights);
            childGenome.FitnessScore = evaluate(childGenome, ctx);

            //childGenome.evaluateFitness();
            population[i] = childGenome;
        }

        bestGenome = population.MaxBy(g => g.FitnessScore);
        Console.WriteLine(bestGenome?.FitnessScore);
    }
}

public class Genome
{
    public float FitnessScore { get; set; }
    public float[] weights { get; set; } // At start initialize weights (they are equal to the amount of neurons in NN)
    public Genome(int numWeights) //Initialize random weights
    {
        //Initialize weights with random values between -1 and 1
        Random random = new Random();
        weights = new float[numWeights];
        for (int i = 0; i < numWeights; i++)
        {
            weights[i] = (float)(random.NextDouble() * 2 - 1); // Random between -1 and 1
        }
    }

    public Genome(float[] weights) //Initialize with predefined weights
    {
        this.weights = weights;
    }

}
