using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Godot;


using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

public record struct Particle
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
    public int ParticleCount { get; set; } = 100;

    [Export]
    public float Sigma { get; set; } = 2f;

    public float SigmaTheta{ get; set; } = 0.01f;

    private List<Particle> particles { get; } = [];
    private List<Particle> candidates { get; } = [];

    // Added parameters for Augmented MCL
    [Export]
    public float AlphaSlow { get; set; } = 0.01f; // Decay rate for long-term average

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
        CastShadow = ShadowCastingSetting.Off;

        Multimesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            Mesh = new BoxMesh
            {
                Size = new Godot.Vector3(0.1f, 0.1f, 0.1f),
                Material = new StandardMaterial3D
                {
                    AlbedoColor = Color.Color8(255, 255, 255, 255),
                    VertexColorUseAsAlbedo = true,
                }
            },
            InstanceCount = ParticleCount,
            VisibleInstanceCount = ParticleCount
        };
    }

    public void initializeParticles()
    {
        Random r = Random.Shared;
        for (int i = 0; i < ParticleCount; i++)
        {
            float y = ((float)r.NextDouble() * Height) - offset.Y;
            float x = ((float)r.NextDouble() * Width) - offset.X;
            float theta = (float)r.NextDouble() * (2 * MathF.PI);
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

        candidates.Clear();
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
            Vector3 vecAfterMotion = new Vector3(currentPos.X + motion_vector.X, currentPos.Y + motion_vector.Y, theta + motion_vector.Z);

            double newWeight = UpdateWeight(realObservations, vecAfterMotion);
            totalWeight += newWeight;
            Particle newParticle = new Particle()
            {
                Coordinate = vecAfterMotion,
                Weight = newWeight
            };
            candidates.Add(newParticle);
        }
        // Normalizing
        // GD.Print("----------Weights----------------");
        // GD.Print(totalWeight);
        if (totalWeight > 0)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                candidates[i] = candidates[i] with { Weight = candidates[i].Weight / totalWeight };
                avgWeight += candidates[i].Weight;
                // avgWeight += candidates[i].Weight;
            }
            avgWeight /= candidates.Count;
        }
        // Resampling
        wSlow += AlphaSlow * (avgWeight - wSlow);
        wFast += AlphaFast * (avgWeight - wFast);
        // Build cumulative sum of weights for efficient sampling

        double[] cumulativeWeights = ArrayPool<double>.Shared.Rent(candidates.Count);
        cumulativeWeights[0] = candidates[0].Weight;

        for (int i = 1; i < candidates.Count; i++)
        {
            cumulativeWeights[i] = cumulativeWeights[i - 1] + candidates[i].Weight;
        }
        Random random = Random.Shared;

        particles.Clear();
        for (int i = 0; i < candidates.Count; i++)
        {
            // Calculate probability of adding random samples
            double randomSampleProb = Math.Max(0.0, 1.0 - wFast / wSlow);

            if (random.NextDouble() < randomSampleProb)
            {
                // Add random sample
                float y = ((float)random.NextDouble() * Height) - offset.Y;
                float x = ((float)random.NextDouble() * Width) - offset.X;
                float theta = (float)random.NextDouble()*(2 * MathF.PI);
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
                // double u = random.NextDouble() * cumulativeWeights.Max();
                int index = 0;

                while (index < candidates.Count - 1 && u > cumulativeWeights[index])
                {
                    index++;
                }

                // Get the selected particle
                Particle selectedParticle = candidates[index];


                float noiseX = (float)(NextGaussian() * 0.05);
                float noiseY = (float)(NextGaussian() * 0.05);
                float noiseTheta = (float)(NextGaussian() * 0.02);


                Particle newParticle = new Particle()
                {
                    Coordinate = new Vector3(
                        selectedParticle.Coordinate.X + noiseX,
                        selectedParticle.Coordinate.Y + noiseY,
                        selectedParticle.Coordinate.Z + noiseTheta
                    ),
                    Weight = selectedParticle.Weight
                };

                particles.Add(newParticle);
            }
        }

        ArrayPool<double>.Shared.Return(cumulativeWeights);

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
            GD.Print(robotCharacter?.simulateMotion(particles.MaxBy(x => x.Weight).Coordinate.Z));
            GD.Print(particles.Count);

            GD.Print("----------Current Position----------------");
            GD.Print(robotCharacter?.GlobalPosition);
            GD.Print(robotCharacter?.simulateMotion(robotCharacter.Rotation.Y));
        }
    }

    // private double UpdateWeight(IEnumerable<(Vector2 Position, float Distance)> real, Vector3 particle)
    // {
    //     double logWeight = 0.0;
    //     Vector2 particlePos = new Vector2(particle.X, particle.Y);
    //     foreach (var observation in real)
    //     {
    //         // Calculate expected observation from this particle's position
    //         double realDistance = observation.Distance;
    //         var realCords = observation.Position;

    //         float partDist = (realCords - particlePos).Length();

    //         // Calculate the probability of this observation

    //         double probRange = GaussianProbability(observation.Distance - partDist, Sigma);
    //         //Console.WriteLine(probRange);

    //         // Combine probabilities

    //         if (probRange > 0) // Avoid taking log of zero
    //             logWeight += Math.Log(probRange);
    //         else
    //             logWeight += -20.0;
    //     }
    //     return Math.Exp(logWeight);
    // }

    private double UpdateWeight(IEnumerable<(Vector2 Position, float Distance)> real, Vector3 particle)
    {
        double logWeight = 0.0;
        Vector2 particlePos = new Vector2(particle.X, particle.Y);
        float particleTheta = particle.Z;
        foreach (var observation in real)
        {
            // Calculate expected observation from this particle's position
            double realDistance = observation.Distance;
            var realCords = observation.Position;

            float partDist = (realCords - particlePos).Length();
            double probRange = GaussianProbability(observation.Distance - partDist, Sigma);

            // Bearing calculation 
            float expectedBearing = MathF.Atan2(observation.Position.Y - particlePos.Y, observation.Position.X - particlePos.X);
            // Compare with particle orientation
            float bearingDiff = AngleDifference(expectedBearing, particleTheta);
            double probBearing = GaussianProbability(bearingDiff, SigmaTheta);
            // Combine probabilities

            if (probRange > 0) // Avoid taking log of zero
                logWeight += Math.Log(probRange) + Math.Log(probBearing);
            else
                logWeight += -20.0;
        }
        return Math.Exp(logWeight);
    }
    private void updateVisuals()
    {
        Multimesh.VisibleInstanceCount = particles.Count;

        if (particles.Count == 0)
            return;

        var bestCandidateParticle = particles.MaxBy(p => p.Weight);
        for (int i = 0; i < particles.Count; ++i)
        {
            var meshTransform = Transform3D.Identity;
            bool isBest = (particles[i] == bestCandidateParticle);

            Godot.Vector3 Scale = isBest ? Godot.Vector3.One : Godot.Vector3.One;


            meshTransform = meshTransform.Scaled(Scale).TranslatedLocal(new Godot.Vector3(particles[i].Coordinate.X, 0, particles[i].Coordinate.Y));

            Multimesh.SetInstanceTransform(i, meshTransform);
            Multimesh.SetInstanceColor(i, isBest ? Color.Color8(0, 255, 0, 255) : Color.Color8(0, 255, 255, 255));
        }
    }

    private static double GaussianProbability(double x, double sigma)
    {
        return Math.Exp(-0.5 * x * x / (sigma * sigma)) / (Math.Sqrt(2 * Math.PI) * sigma);
    }

    private static double NextGaussian()
    {
        Random r = Random.Shared;
        double u1 = 1.0 - r.NextDouble();
        double u2 = 1.0 - r.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }

    private float AngleDifference(float angle1, float angle2)
    {
        // Returns smallest angle difference in range [-π, π]
        float diff = (angle1 - angle2) % (2 * MathF.PI);
        if (diff > MathF.PI) diff -= 2 * MathF.PI;
        if (diff < -MathF.PI) diff += 2 * MathF.PI;
        return diff;
    }
    private float NormalizeAngle(float angle)
    {
        // Normalize to [0, 2π)
        angle = angle % (2 * MathF.PI);
        if (angle < 0) angle += 2 * MathF.PI;
        return angle;
    }
}
