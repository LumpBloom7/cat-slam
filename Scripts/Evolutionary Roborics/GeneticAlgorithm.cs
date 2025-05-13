using System.Diagnostics;
using System.Runtime.CompilerServices;
using MyProject.NeuralNetwork;
using TorchSharp;

namespace MyProject.Algorithms
{
    public class GeneticAlgorithm
    {
        private int k;
        public Population population;
        private float parentSelectionPersentage;
        private int genomeSize;
        float mutationRate;
        public float bestFitness;

        public GeneticAlgorithm(int populationSize, int genomeSize, int torunamentSeelectionSize, float parentSelectionPersentage, float mutationRate)
        {
            this.mutationRate = mutationRate;
            this.genomeSize = genomeSize;
            this.k = torunamentSeelectionSize;
            this.parentSelectionPersentage = parentSelectionPersentage;
            this.population = new Population(populationSize, genomeSize);
            var populationEvaluate = this.population.Genomes;
            for (int i = 0; i < populationEvaluate.Length; i++)
            {
                //populationEvaluate[i].evaluateFitness();
                populationEvaluate[i].FitnessScore = evaluate(populationEvaluate[i]);
                float fitness = populationEvaluate[i].FitnessScore;
                //Console.WriteLine(fitness);
            }
        }

        private float evaluate(Genome individual){ // accepts an individual (We will simulate his game)
            //Here we get sensor inputs and motor velocities for input + restart the enviroment
            float[] sensorInput = [3.0f,4.0f,1.0f,3.0f];
            float[] motorVelocity = [0.2f,1.4f];
            //Define the NN 
            Model model = new Model(sensorInput.Length,motorVelocity.Length);
            model.setWeights(individual.weights); // assign the weights to out NN
            int step = 0;
            int award=0;

            while(step<1000){
            // here we get our observations but i am unsure how to retrieve them from godot dynamically so i am using place holder
                var seq = model.seq;
                var x = torch.tensor(sensorInput); // forgot we need to include sensor Input+ motor inputs
                var y = torch.tensor(new float[]{2f,3f});
                //perform step
                using var action  = seq.forward(x);
                //Now that we have an action we perform a move in out enviroment from which we get award (like hitting wall -200 or smth, collecting a cat +1000)
                // reward, observation = envitoment.step()
                //award += reward
                step = step+1;
                //sensorInput = obs2 set to new sensor readings
            }
            return 1f; // placeholder 
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
            int numberOfParents = (int)(population.PopulationSize * parentSelectionPersentage);
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


        public void Run()
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
                childGenome.FitnessScore=evaluate(childGenome);
                //childGenome.evaluateFitness();
                newPopulation.Add(childGenome);
            }

            //Store best fit 
            float bestFitness = 10000000;
            for (int i = 0; i < newPopulation.Count; i++)
            {
                Console.WriteLine("parent " + i + "fitness: " + newPopulation[i].FitnessScore);
                float fitness = newPopulation[i].FitnessScore;
                if (bestFitness == 10000000 || fitness < bestFitness)
                {
                    bestFitness = fitness;
                }
            }
            this.bestFitness = bestFitness;

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

        // public void evaluateFitness()
        // {
        //     float errorSum = 0;
        //     int numSamples = 100; // Number of samples to test the polynomial on
        //     float xMin = -10f; // Minimum x value for testing
        //     float xMax = 10f;  // Maximum x value for testing
        //     float step = (xMax - xMin) / numSamples; // Step size to sample values of x

        //     // Iterate over x values to calculate the error (MSE)
        //     for (float x = xMin; x <= xMax; x += step)
        //     {
        //         // Genome represents the polynomial coefficients: a, b, c (for a*x^2 + b*x + c)
        //         float polynomialValue = this.weights[0] * x * x + this.weights[1] * x + this.weights[2]; // ax^2 + bx + c
        //         float targetValue = TargetPolynomial(x); // Target polynomial value

        //         // Compute the squared error between the genome's polynomial and the target polynomial
        //         errorSum += (polynomialValue - targetValue) * (polynomialValue - targetValue);
        //     }

        //     // Return the mean squared error (MSE)
        //     this.FitnessScore = errorSum / numSamples;
        // }

        public static float TargetPolynomial(float x) => x * x + 3 * x + 2;

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
            this.Genomes = population;
        }

        private void InitializePopulation(int populationSize)
        {
            Genome[] genomes = new Genome[populationSize];

            for (int i = 0; i < populationSize; i++)
                genomes[i] = new Genome(genomeSize); // Initialize each genome with random weights

            this.Genomes = genomes;
        }
    }
}
