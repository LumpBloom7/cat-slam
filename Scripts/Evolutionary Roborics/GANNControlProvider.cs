using System.Linq;
using Godot;

public partial class GANNControlProvider : Node
{
    [Export]
    public int PopulationSize { get; set; } = 10;

    [Export]
    public float ParentSelectionPercentage { get; set; } = 0.2f;

    [Export]
    public float MutationRate { get; set; } = 0.02f;

    [Export]
    public int TournamentSize_k { get; set; } = 5;


    private GeneticAlgorithm? geneticAlgorithm;

    private SimulationProvider? simulationProvider = null!;

    public override void _Ready()
    {
        base._Ready();

        var sensorArray = GetNode("/root").GetDescendants<SensorArray>(true).FirstOrDefault();

        if (sensorArray is null)
            return;

        int inputLength = 2 + sensorArray.NumberOfSensors;

        geneticAlgorithm = new GeneticAlgorithm(PopulationSize, inputLength, TournamentSize_k, ParentSelectionPercentage, MutationRate);
        simulationProvider = GetNode("/root").GetDescendants<SimulationProvider>(true).FirstOrDefault();
    }

    private bool gannEnabled = false;

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event.IsActionPressed("ToggleGann", false))
            gannEnabled = !gannEnabled;
    }

    public (int, int)? ComputeGannInput(double delta)
    {
        if (geneticAlgorithm is null || simulationProvider is null || !gannEnabled)
            return null;

        var simulation = simulationProvider.createSimulationContext();

        //Run the Genetic Algorithm up to (50) times
        float generationBestFitness = float.MinValue;
        Genome? bestGenome = null;
        int generationsSinceLastImprovement = 0;

        for (int i = 0; i < 1; ++i)
        {
            geneticAlgorithm.Run(simulation, delta);

            if (geneticAlgorithm.bestGenome?.FitnessScore >= generationBestFitness)
            {
                bestGenome = geneticAlgorithm.bestGenome;
                generationsSinceLastImprovement = -1;
            }

            if (++generationsSinceLastImprovement >= 10)
                break;
        }

        if (bestGenome is null)
            return null;

        return geneticAlgorithm.evaluateToGetAction(bestGenome, simulation);
    }

}
