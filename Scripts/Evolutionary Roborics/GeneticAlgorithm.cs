using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TorchSharp;

public class GeneticAlgorithm
{
    private readonly int k;
    public Population population;
    private readonly float parentSelectionPercentage;
    private readonly int genomeSize;
    private readonly float mutationRate;
    private Model model;
    public Genome? bestGenome = null;

    public GeneticAlgorithm(int populationSize, int numberOfInputs, int tournamentSelectionSize, float parentSelectionPercentage, float mutationRate)
    {
        this.mutationRate = mutationRate;

        model = new Model(numberOfInputs, 2);
        genomeSize = model.countNeurons();
        k = tournamentSelectionSize;
        this.parentSelectionPercentage = parentSelectionPercentage;

        population = new Population(populationSize, genomeSize);
    }

    private float evaluate(Genome individual, SimulationProvider.SimulationContext ctx)
    {
        // Reset the environment
        ctx.Reset();

        float[] neuralNetworkInput = ArrayPool<float>.Shared.Rent(ctx.SensorValues.Length + 2);

        //Here we get sensor inputs and motor velocities for input
        ctx.SensorValues.CopyTo(neuralNetworkInput, 0);
        neuralNetworkInput[neuralNetworkInput.Length] = ctx.LeftVel;
        neuralNetworkInput[neuralNetworkInput.Length + 1] = ctx.RightVel;

        //Define the NN
        model.setWeights(individual.weights); // assign the weights to out NN

        for (int i = 0; i < 1000; ++i)
        {
            //here we get our observations but i am unsure how to retrieve them from godot dynamically so i am using place holder
            var seq = model.seq;
            using var eval = torch.tensor(neuralNetworkInput);
            //perform step
            using var action = seq.forward(eval);

            var data = eval.data<float>();

            ctx.Update((int)Math.Round(data[0]), (int)Math.Round(data[1])); //update enviroment

            //update neural network inputs for next step
            ctx.SensorValues.CopyTo(neuralNetworkInput, 0);
            neuralNetworkInput[neuralNetworkInput.Length] = ctx.LeftVel;
            neuralNetworkInput[neuralNetworkInput.Length + 1] = ctx.RightVel;
        }

        ArrayPool<float>.Shared.Return(neuralNetworkInput);

        return ctx.ComputeReward(); // placeholder
    }
    public (int, int) evaluateToGetAction(Genome individual, SimulationProvider.SimulationContext ctx)
    {
        ctx.Reset();

        float[] neuralNetworkInput = ArrayPool<float>.Shared.Rent(ctx.SensorValues.Length + 2);

        //Here we get sensor inputs and motor velocities for input
        ctx.SensorValues.CopyTo(neuralNetworkInput, 0);
        neuralNetworkInput[neuralNetworkInput.Length] = ctx.LeftVel;
        neuralNetworkInput[neuralNetworkInput.Length + 1] = ctx.RightVel;

        model.setWeights(individual.weights); // assign the weights to out NN
        var seq = model.seq;
        using var x = torch.tensor(neuralNetworkInput); // forgot we need to include sensor Input+ motor inputs
        using var action = seq.forward(x);

        int leftMotorReading = (int)Math.Round(x.data<float>()[0]);
        int rightMotorReading = (int)Math.Round(x.data<float>()[1]);
        return (leftMotorReading, rightMotorReading); // placeholder
    }

    private Genome tournamentSelection()
    {
        ///Console.WriteLine("--------------------------");
        //Console.WriteLine("Tournament Selection");
        Debug.Assert(population.Genomes.Length > 0);

        Random.Shared.Shuffle(population.Genomes);

        return population.Genomes.Take(k).MinBy(g => g.FitnessScore)!;
    }

    private Genome[] selectParents()
    {
        // Calculate 10% of the population size
        int numberOfParents = (int)(population.PopulationSize * parentSelectionPercentage);
        if (numberOfParents < 2)
            return [];

        HashSet<Genome> selectedParents = [];

        while (selectedParents.Count < numberOfParents)
            selectedParents.Add(tournamentSelection());

        return [.. selectedParents];
    }

    public float[] singlePointCrossover(float[] parent1, float[] parent2)
    {
        //Console.WriteLine("single crossover");
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
            Console.WriteLine("Mutation performed");
            int index = random.Next(childWeights.Length);
            childWeights[index] += (float)(random.NextDouble() * 2 - 1); //Random between -1 and 1
        }
        return childWeights;
    }


    public void Run(SimulationProvider.SimulationContext ctx)
    {
        Console.WriteLine("Running Genetic Algorithm..."); //GA process: selection, crossover, mutation, etc.
                                                           //Console.WriteLine("");

        //select parents
        var parents = selectParents();
        //Console.WriteLine("Parent length: " + parents.Length);

        // Create new population, include parents implicitly
        List<Genome> newPopulation = [.. parents];

        Random rand = Random.Shared;

        while (newPopulation.Count < population.PopulationSize)
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
            newPopulation.Add(childGenome);
        }

        bestGenome = newPopulation.MaxBy(g => g.FitnessScore);

        Population newGeneration = new Population(genomeSize, [.. newPopulation]);
        population = newGeneration;
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

public class Population
{
    public Genome[] Genomes { get; set; } = [];
    public int PopulationSize => Genomes.Length;
    private int genomeSize;

    public Population(int populationSize, int genomeSize)
    {
        Console.WriteLine("Random Population initialization");
        this.genomeSize = genomeSize;
        InitializePopulation(populationSize);
    }
    public Population(int genomeSize, Genome[] population)
    {
        Console.WriteLine("new Generation initialization");
        this.genomeSize = genomeSize;
        Genomes = population;
    }

    private void InitializePopulation(int populationSize)
    {
        Genome[] genomes = new Genome[populationSize];

        for (int i = 0; i < populationSize; i++)
            genomes[i] = new Genome(genomeSize); // Initialize each genome with random weights

        Genomes = genomes;
    }
}

