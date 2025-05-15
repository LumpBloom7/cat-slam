using System;
using System.Buffers;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Numerics;
using Godot;
using MathNet.Numerics.Distributions;
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
    public int ParticleCount { get; set; } = 1000;

    [Export]
    public float Sigma { get; set; } = 2f;

    public float SigmaTheta { get; set; } = 1f;


    private List<Particle> particles { get; } = [];
    private List<Particle> candidates { get; } = [];

    // Added parameters for Augmented MCL
    [Export]
    public float AlphaSlow { get; set; } = 0.4f; // Decay rate for long-term average

    [Export]
    public float AlphaFast { get; set; } = 0.5f; // Decay rate for short-term average

    [Export]
    private float noiseXalpha { get; set; } = 0;
    [Export]
    private float noiseYalpha { get; set; } = 0f;
    [Export]
    private float noiseThetaAlpha { get; set; } = 0f;
    private double wSlow = 0; // Long-term average weight
    private double wFast = 0; // Short-term average weight

    private float a1 = 0.5f;

    private float a2 = 0.5f;

    private float a3 = 0.5f;

    private float a4 = 0.5f;

    private float a5 = 0.01f;

    private float a6 = 0.01f;
    private bool hadObservationsLastFrame = false;

    [Signal]
    public delegate void GhostStateChangedEventHandler(Godot.Vector3 position, Godot.Vector3 rotation);

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
            float y = ((float)r.NextDouble() * Height) - offset.Y; // random number centered at 0
            float x = ((float)r.NextDouble() * Width) - offset.X;
            float theta = ((float)r.NextDouble() * (2 * MathF.PI)) - MathF.PI; // angle between - pi and pi
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
        var realObservations = beaconD.GetTrackedBeacons().Select(p => (p.Position * new Vector2(1, -1), p.Distance)).ToList();
        double totalWeight = 0.0;
        double avgWeight = 0.0;

        var gp = robotCharacter.GlobalPosition;



        // Applying motion and updating the weights
        foreach (var particle in particles)
        {
            var currentPos = particle.Coordinate;
            float theta = particle.Coordinate.Z;
            var motion_vector = robotCharacter.simulateMotion(theta, delta);
            var num_mot_vec = new Vector2() { X = motion_vector.X, Y = motion_vector.Y }; // Velocity and Angular Velocity
            // Vector3 vecAfterMotion = new Vector3(currentPos.X + motion_vector.X, currentPos.Y + motion_vector.Y, theta + motion_vector.Z);
            Vector3 vecAfterMotion = sampleNewPosition(currentPos, num_mot_vec, (float)delta);
            double newWeight = UpdateWeight(realObservations, vecAfterMotion, gp);
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
                // double normalized = candidates[i].Weight / totalWeight;
                // Particle newParticle = new Particle()
                // {
                //     Coordinate = candidates[i].Coordinate,
                //     Weight = normalized
                // };
                // candidates[i] = newParticle;
                //avgWeight += normalized;
                avgWeight += candidates[i].Weight;
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
            double randomSampleProb = 0.0;
            if (wSlow > 1e-10)
            { // Prevent division by zero
                randomSampleProb = Math.Max(0.0, 1.0 - wFast / wSlow);
            }
            // Optionally add a minimum probability
            randomSampleProb = Math.Max(0, randomSampleProb);

            if (random.NextDouble() < randomSampleProb)
            {
                // Add random sample
                float y = ((float)random.NextDouble() * Height) - offset.Y;
                float x = ((float)random.NextDouble() * Width) - offset.X;
                float theta = ((float)random.NextDouble() * (2 * MathF.PI)) - MathF.PI;
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
                // double u = random.NextDouble();
                double u = random.NextDouble() * cumulativeWeights[candidates.Count() - 1];

                int index = 0;

                while (index < candidates.Count - 1 && u > cumulativeWeights[index])
                {
                    index++;
                }

                // Get the selected particle
                Particle selectedParticle = candidates[index];


                float noiseX = (float)(NextGaussian() * noiseXalpha);
                float noiseY = (float)(NextGaussian() * noiseYalpha);
                float noiseTheta = (float)(NextGaussian() * noiseThetaAlpha);


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

        //print();
        updateVisuals();
        var highestWeightParticle = particles.MaxBy(p => p.Weight);
        // Make sure you map coordinates correctly here
        var tmp = CalculateWeightedAverage(particles);

        if (!tmp.HasValue)
            return;

        var best_position = tmp.Value;

        // Keep the negative Y coordinate since your system is apparently calibrated for that
        var coord = new Godot.Vector3() { X = best_position.X, Y = 0f, Z = -best_position.Y };

        // Then convert to Godot's rotation system
        var rotation = new Godot.Vector3(0, (best_position.Theta).FromMathematicalAngle(), 0);

        EmitSignal(SignalName.GhostStateChanged, coord, rotation);

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
            // GD.Print(robotCharacter?.simulateMotion(particles.MaxBy(x => x.Weight).Coordinate.Z));
            GD.Print(particles.Count);

            GD.Print("----------Current Position----------------");
            GD.Print(robotCharacter?.GlobalPosition);
            // GD.Print(robotCharacter?.simulateMotion(robotCharacter.Rotation.Y));
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
    private double UpdateWeight(IEnumerable<(Vector2 Position, float Distance)> real, Vector3 particle, Godot.Vector3 robotPos)
    {
        // Early return if no observations
        if (!real.Any())
        {
            return 0.5 / ParticleCount;
        }

        double logWeight = 0.0;
        Vector2 particlePos = new Vector2(particle.X, particle.Y);

        foreach (var observation in real)
        {
            float partDist = (observation.Position - particlePos).Length();
            double probRange = GaussianProbability(observation.Distance - partDist, Sigma);

            // Revised bearing calculation - note the sign adjustments
            float expectedBearing = MathF.Atan2(observation.Position.Y - particlePos.Y,
                                            observation.Position.X - particlePos.X);

            // This version matches your coordinate transformation on output
            float real_bearing = MathF.Atan2((observation.Position.Y + robotPos.Z),
                                        observation.Position.X - robotPos.X);

            // Normalize the difference properly
            float bearingDiff = AngleDifference(real_bearing, expectedBearing);
            double probBearing = GaussianProbability(bearingDiff, SigmaTheta);

            if (probRange > 0 && probBearing > 0)
                logWeight += Math.Log(probRange * probBearing);
            else
                logWeight += -5.0;
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

            Godot.Vector3 Scale = isBest ? new Godot.Vector3(1, 5, 1) : Godot.Vector3.One;

            meshTransform = meshTransform.Scaled(Scale).TranslatedLocal(new Godot.Vector3(particles[i].Coordinate.X, 0, -particles[i].Coordinate.Y));

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
        double u1 = 1.0 - r.NextDouble(); // Uniform(0,1) random doubles
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

    public static (float X, float Y, float Theta)? CalculateWeightedAverage(
        List<Particle> partics)
    {
        if (partics == null || partics.Count == 0)
            return null;

        double sumWeights = partics.Sum(v => v.Weight);
        if (Math.Abs(sumWeights) < 1e-10)
            return null;

        double sumX = 0;
        double sumY = 0;
        double sumCosTheta = 0;
        double sumSinTheta = 0;

        foreach (var vector in partics)
        {
            sumX += vector.Coordinate.X * vector.Weight;
            sumY += vector.Coordinate.Y * vector.Weight;
            // For angles, we weight the unit vectors
            sumCosTheta += Math.Cos(vector.Coordinate.Z) * vector.Weight;
            sumSinTheta += Math.Sin(vector.Coordinate.Z) * vector.Weight;
        }

        double avgX = sumX / sumWeights;
        double avgY = sumY / sumWeights;
        // Convert weighted average unit vector back to angle
        double avgTheta = Math.Atan2(sumSinTheta, sumCosTheta);

        return ((float)avgX, (float)avgY, (float)avgTheta);
    }

    private static float sampleGaussian(float mean, float stdDev)
    {
        Random rand = Random.Shared; //reuse this if you are generating many
        double u1 = 1.0 - rand.NextDouble(); //uniform(0,1] random doubles
        double u2 = 1.0 - rand.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                    Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
        double randNormal = mean + stdDev * randStdNormal; //random normal(mean,stdDev^2)

        return (float)randNormal;
    }

    private Vector3 sampleNewPosition(Vector3 position, Vector2 u, float delta)
    {
        float v_hat = u.X + sampleGaussian(0, (a1 * MathF.Pow(u.X, 2) + a2 * MathF.Pow(u.Y, 2)));
        float w_hat = u.Y + sampleGaussian(0, (a3 * MathF.Pow(u.X, 2) + a4 * MathF.Pow(u.Y, 2)));
        float gamma_hat = sampleGaussian(0, (a5 * MathF.Pow(u.X, 2) + a6 * MathF.Pow(u.Y, 2)));
        float v_hat_div_w_hat = 0;
        if (w_hat != 0)
        {
            v_hat_div_w_hat = v_hat / w_hat;
        }

        float x_prime = position.X - (v_hat_div_w_hat * MathF.Sin(position.Z)) + (v_hat_div_w_hat * MathF.Sin(position.Z + (delta * w_hat)));
        float y_prime = position.Y + (v_hat_div_w_hat * MathF.Cos(position.Z)) - (v_hat_div_w_hat * MathF.Cos(position.Z + (delta * w_hat)));
        float theta_prime = position.Z + (delta * w_hat) + (delta * gamma_hat);
        // Normalize the angle
        //theta_prime = theta_prime);
        return new Vector3() { X = x_prime, Y = y_prime, Z = theta_prime };
    }

    private float NormalizeAngle(float angle)
    {
        // Using modulo to get principal value, then adjusting if needed
        angle = angle % (2 * MathF.PI);

        if (angle > MathF.PI)
            angle -= 2 * MathF.PI;
        else if (angle <= -MathF.PI)
            angle += 2 * MathF.PI;

        return angle;
    }
}
