
using System;
using System.Data;

public class EvolutionMain
{
    public Model model;
    private int k; //5
    private int populationSize; //10
    private float parentSelectionPersentage; //0.20f;
    private float mutationRate; //0.02f
    private Genome bestGenome;
    SimulationProvider.SimulationContext simulation;

    public EvolutionMain(int populationSize, int k, int parentSelectionPersentage, int mutationRate){
        this.k = k;
        this.populationSize = populationSize;
        this.parentSelectionPersentage = parentSelectionPersentage;
        this.mutationRate = mutationRate;
    }
    public (int,int) update(SimulationProvider.SimulationContext simulation){
        //Initialize Neural Network
        int sensorLength = simulation.SensorValues.Length;
        int motorLength = 2;
        model = new Model(sensorLength + motorLength, motorLength);
        int numOfWeights = model.countNeurons(); // num of weights of NN determines the size of our Genomes

        GeneticAlgorithm ga = new GeneticAlgorithm(populationSize, numOfWeights, k, parentSelectionPersentage, mutationRate, model, simulation);
        //Run the Genetic Algorithm
        int count = 1;
        float generationBestFintess = 1000000000;
        int hasGenerationImproved = 0;

        while (count != 4 | hasGenerationImproved < 10)
        {
            //Console.WriteLine("--------------");
            //Console.WriteLine("round: " + count);
            var population = ga.population.Genomes;
            ga.Run();
            if (generationBestFintess == 1000000000 || ga.bestGenome.FitnessScore < generationBestFintess)
            {
                generationBestFintess = ga.bestGenome.FitnessScore;
                hasGenerationImproved = 0;
                bestGenome = ga.bestGenome;
            }
            else
            {
                hasGenerationImproved += 1;
            }
            //re evaluate shild with best fitness and get motor left right etc.
        }
        (int,int) action = ga.evaluateToGetAction(bestGenome);
        return action;
    }
}
