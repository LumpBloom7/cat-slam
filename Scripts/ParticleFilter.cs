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
    public float Sigma { get; set; } = 2f;

    private List<Particle> particles { get; } = [];

    // Added parameters for Augmented MCL
    [Export]
    public float AlphaSlow { get; set; } = 0.001f; // Decay rate for long-term average

    [Export]
    public float AlphaFast { get; set; } = 0.1f; // Decay rate for short-term average

    private double wSlow = 0.0; // Long-term average weight
    private double wFast = 0.0; // Short-term average weight

    public override void _Ready()
    {
        base._Ready();

        robotCharacter = GetNode("/root").GetDescendants<RobotCharacter>(true).FirstOrDefault();

        var tileMap = GetNode("/root").GetDescendants<GridMapGenerator>(true).FirstOrDefault();
        Width = tileMap?.Width ?? 0;
        Height = tileMap?.Height ?? 0;

        offset = new Vector2(Width / 2f, Height / 2f);

        wSlow = 0.0;
        wFast = 0.0;

        initializeParticles();

        Multimesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = new SphereMesh
            {
                Radius = 0.2f,
                Material = new StandardMaterial3D
                {
                    AlbedoColor = Color.Color8(0, 255, 255, 255),
                }
            },
            InstanceCount = ParticleCount,
            VisibleInstanceCount = ParticleCount
        };
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
        double avgWeight = 0.0;

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
                avgWeight += normalized;
            }
            avgWeight /= NewParticlesCandidates.Count;
        }
        // Resampling
        wSlow = wSlow + AlphaSlow * (avgWeight - wSlow);
        wFast = wFast + AlphaFast * (avgWeight - wFast);
        // Build cumulative sum of weights for efficient sampling

        double[] cumulativeWeights = new double[NewParticlesCandidates.Count];
        cumulativeWeights[0] = NewParticlesCandidates[0].Weight;

        for (int i = 1; i < NewParticlesCandidates.Count; i++)
        {
            cumulativeWeights[i] = cumulativeWeights[i - 1] + NewParticlesCandidates[i].Weight;
        }
        Random random = new Random();

        particles.Clear();
        for (int i = 0; i < NewParticlesCandidates.Count; i++)
        {
            // Calculate probability of adding random samples
            double randomSampleProb = Math.Max(0.0, 1.0 - wFast / wSlow);

            if (random.NextDouble() < randomSampleProb)
            {
                // Add random sample
                float y = ((float)random.NextDouble() * Height) - offset.Y;
                float x = ((float)random.NextDouble() * Width) - offset.X;
                float theta = (float)random.NextDouble();
                Vector3 newVec = new Vector3(x, y, theta);
                // example how to do motion vector
                Particle newParticle = new Particle()
                {
                    Coordinate = newVec,
                    Weight = 1f / ParticleCount
                };
                particles.Add(newParticle);
            }
            else
            {
                // Regular resampling
                double u = random.NextDouble();
                int index = 0;

                while (index < NewParticlesCandidates.Count - 1 && u > cumulativeWeights[index])
                {
                    index++;
                }

                particles.Add(NewParticlesCandidates[index]);
            }
        }

        print();
        updateVisuals();
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

    private void updateVisuals()
    {
        Multimesh.VisibleInstanceCount = particles.Count;
        for (int i = 0; i < particles.Count; ++i)
        {
            var meshTransform = Transform3D.Identity;

            meshTransform = meshTransform.TranslatedLocal(new Godot.Vector3(particles[i].Coordinate.X, 0.25f, particles[i].Coordinate.Y));

            Multimesh.SetInstanceTransform(i, meshTransform);
        }
    }

    private double GaussianProbability(double x, double sigma)
    {
        return Math.Exp(-0.5 * x * x / (sigma * sigma)) / (Math.Sqrt(2 * Math.PI) * sigma);
    }

}
