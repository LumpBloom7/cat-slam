using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Godot;


using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

public struct Particle
{
    // Z is the theta here
    public Vector3 Coordinate { get; init; }
    public double Weight { get; set; }
}

public partial class ParticleFilter : MultiMeshInstance3D
{
    private RobotCharacter? robotCharacter = null!;
    public int Height { get; private set; }
    public int Width { get; private set; }

    private Vector2 offset { get; set; }

    [Export]
    public int ParticleCount { get; set; } = 1000;

    [Export]
    public float Sigma { get; set; } = 0.01f;

    private List<Particle> particles { get; } = [];

    public override void _Ready()
    {
        base._Ready();

        robotCharacter = GetNode("/root").GetDescendants<RobotCharacter>(true).FirstOrDefault();

        var tileMap = GetNode("/root").GetDescendants<GridMapGenerator>(true).FirstOrDefault();
        Width = tileMap?.Width ?? 0;
        Height = tileMap?.Height ?? 0;

        offset = new Vector2(Width / 2f, Height / 2f);

        initializeParticles();
    }

    public void initializeParticles()
    {
        Random r = new Random();
        for (int i = 0; i < ParticleCount; i++)
        {
            float y = ((float)r.NextDouble() * Height) - offset.Y;
            float x = ((float)r.NextDouble() * Width) - offset.X;
            float theta = (float)r.NextDouble();
            Vector3 newVec = new Vector3(x, y, theta);
            // example how to do motion vector
            Particle newParticle = new Particle()
            {
                Coordinate = newVec,
                Weight = 1f / ParticleCount
            };
            particles.Add(newParticle);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (robotCharacter is null)
            return;

        List<Particle> NewParticlesCandidates = new List<Particle>();
        var beaconD = robotCharacter.BeaconDetector;
        var realObservations = beaconD.GetTrackedBeacons();
        double totalWeight = 0.0;

        // Applying motion and updating the weights
        foreach (var particle in particles)
        {
            var currentPos = particle.Coordinate;
            float theta = particle.Coordinate.Z;
            var motion_vector = robotCharacter.simulateMotion(theta) * (float)delta;
            Vector3 vecAfterMotion = new Vector3(currentPos.X + motion_vector.X, currentPos.Y + motion_vector.Y, theta);

            double newWeight = UpdateWeight(realObservations, vecAfterMotion);
            totalWeight += newWeight;
            Particle newParticle = new Particle()
            {
                Coordinate = vecAfterMotion,
                Weight = newWeight
            };
            NewParticlesCandidates.Add(newParticle);
        }
        // Normalizing
        if (totalWeight > 0)
        {
            for (int i = 0; i < NewParticlesCandidates.Count; i++)
            {
                double normalized = NewParticlesCandidates[i].Weight / totalWeight;
                Particle newParticle = new Particle()
                {
                    Coordinate = NewParticlesCandidates[i].Coordinate,
                    Weight = normalized
                };
                NewParticlesCandidates[i] = newParticle;
            }
        }
        // Resampling

        // Build cumulative sum of weights for efficient sampling

        double[] cumulativeWeights = new double[NewParticlesCandidates.Count];
        cumulativeWeights[0] = NewParticlesCandidates[0].Weight;

        for (int i = 1; i < NewParticlesCandidates.Count; i++)
        {
            cumulativeWeights[i] = cumulativeWeights[i - 1] + NewParticlesCandidates[i].Weight;
        }
        int index = 0;
        Random random = new Random();
        double u = random.NextDouble();

        particles.Clear();
        for (int i = 0; i < NewParticlesCandidates.Count; i++)
        {
            while (u > cumulativeWeights[index] && index < NewParticlesCandidates.Count - 1)
            {
                index++;
            }

            // Add a copy of the selected particle

            particles.Add(NewParticlesCandidates[index]);
            index = 0;
        }

        print();
    }

    private int count = 0;
    private void print()
    {
        if (++count == 60)
        {
            count = 0;
            GD.Print("----------Particles----------------");
            GD.Print(particles.MaxBy(x => x.Weight).Coordinate);
            GD.Print(particles.MaxBy(x => x.Weight).Weight);

            GD.Print("----------Current Position----------------");
            GD.Print(robotCharacter?.GlobalPosition);
        }
    }

    private double UpdateWeight(IEnumerable<(Vector2 Position, float Distance)> real, Vector3 particle)
    {

        double weight = 1.0;
        Vector2 particlePos = new Vector2(particle.X, particle.Y);
        foreach (var observation in real)
        {
            // Calculate expected observation from this particle's position
            double realDistance = observation.Distance;
            var realCords = observation.Position;

            float partDist = (realCords - particlePos).Length();

            // Calculate the probability of this observation

            double probRange = GaussianProbability(observation.Distance - partDist, Sigma);
            Console.WriteLine(probRange);

            // Combine probabilities

            weight *= probRange;
        }
        return weight;
    }
    private double GaussianProbability(double x, double sigma)
    {
        return Math.Exp(-0.5 * x * x / (sigma * sigma)) / (Math.Sqrt(2 * Math.PI) * sigma);
    }

}
