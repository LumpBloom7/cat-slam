using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public struct Particle
{
    // Z is the theta here
    public Vector3 Coordinate { get; init; }
    public double Weight { get; set; }
}

public class ParticleFilter
{
    public RobotCharacter RobotCharacter = null!;
    public int Height { get; }
    public int Width { get; }

    public double sigma { get; }

    public int Num_particles { get; }

    public List<Particle> Particles { get; set; }

    public ParticleFilter(Tile[][] map, RobotCharacter rb, int num_particles = 1000, double standard_diviation = 0.01)
    {
        RobotCharacter = rb;
        Height = map.Length;
        Width = map[0].Length;
        Num_particles = num_particles;
        Particles = new List<Particle>();
        sigma = standard_diviation;
    }
    public ParticleFilter(RobotCharacter rb, int height = 3, int width = 3, int num_particles = 1000, double standard_diviation = 0.01)
    {
        RobotCharacter = rb;
        Height = height;
        Width = width;
        Num_particles = num_particles;
        Particles = new List<Particle>();
        sigma = standard_diviation;
    }

    public void initializeParticles()
    {
        Random r = new Random();
        for (int i = 0; i < Num_particles; i++)
        {
            float y = (float)r.NextDouble() * Height;
            float x = (float)r.NextDouble() * Width;
            float theta = (float)r.NextDouble();
            Vector3 newVec = new Vector3(x, y, theta);
            // example how to do motion vector 
            Particle newParticle = new Particle()
            {
                Coordinate = newVec,
                Weight = 1f / Num_particles
            };
            Particles.Add(newParticle);
        }
    }

    public void Update()
    {
        List<Particle> NewParticles = new List<Particle>();
        List<Particle> NewParticlesCandidates = new List<Particle>();
        var beaconD = RobotCharacter.BeaconDetector;
        var realObservations = beaconD.GetTrackedBeacons();
        double totalWeight = 0.0;

        // Applying motion and updating the weights
        foreach (var particle in Particles)
        {
            var currentPos = particle.Coordinate;
            float theta = particle.Coordinate.Z;
            var motion_vector = RobotCharacter.simulateMotion(theta);

            Vector3 vecAfterMotion = new Vector3(currentPos.X + motion_vector.X, currentPos.Y + motion_vector.Y, theta);


            var partivleObservations = beaconD.GetBeaconsInfoFrom(vecAfterMotion.X, vecAfterMotion.Y);
            double newWeight = UpdateWeight(partivleObservations, realObservations);
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
        for (int i = 0; i < NewParticlesCandidates.Count; i++)
        {
            while (u > cumulativeWeights[index] && index < NewParticlesCandidates.Count - 1)
            {
                index++;
            }

            // Add a copy of the selected particle

            NewParticles.Add(NewParticlesCandidates[index]);
            index = 0;
        }
        Particles = NewParticles;
    }

    private double UpdateWeight(IEnumerable<(Vector2 Position, float Distance)> observations, IEnumerable<(Vector2 Position, float Distance)> real)
    {

        List<(Vector2 Position, float Distance)> inRange = [];

        foreach (var observation in observations)
        {
            if (observation.Distance < 500)
            {
                inRange.Add(observation);
            }
        }

        if (inRange.Count != real.Count())
        {
            return 0.0;
        }

        double weight = 1.0;

        foreach (var observation in inRange)
        {
            // Find the landmark corresponding to this observation
            var landmark = real.FirstOrDefault(r => r.Position == observation.Position);
            if (landmark == (null, null)) continue;

            // Calculate expected observation from this particle's position

            double observationDistance = observation.Distance;



            // Calculate the probability of this observation

            double probRange = GaussianProbability(landmark.Distance - observationDistance, sigma);
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