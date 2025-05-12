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
            this.mutationRate=mutationRate;
            this.genomeSize=genomeSize;
            this.k=torunamentSeelectionSize;
            this.parentSelectionPersentage = parentSelectionPersentage;
            population = new Population(populationSize, genomeSize);
        }
        
        private Genome tournamentSelection()
        {   
            //Console.WriteLine("--------------------------");
            //Console.WriteLine("Tournament Selection");
            Random.Shared.Shuffle(population.GetGenomes());
            var uniqueGenomes = population.GetGenomes().Take(3).ToArray();

            Genome bestGenome = null;
            foreach (var genome in uniqueGenomes)
            {
                if (bestGenome == null || genome.fitnessScore < bestGenome.fitnessScore) // the smaller the better!
                {
                    bestGenome = genome;
                }
            }
            float[] bestGenomeWeight = bestGenome.getGenome();
            //Console.WriteLine("best genome: "+bestGenomeWeight);
            return bestGenome;
        }
        private Genome[] selectParents()
        {
            // Calculate 10% of the population size
            int numberOfParents = (int)(this.population.GetGenomes().Length *  this.parentSelectionPersentage);
            Genome[] selectedParents = new Genome[numberOfParents];
            //Console.WriteLine("---------------");
            //Console.WriteLine("Selecting Patents");
            for (int i = 0; i < numberOfParents; i++)
            {
                selectedParents[i] = tournamentSelection();
                 //Console.WriteLine("Selected Parent: "+ selectedParents[i]);
            }
            return selectedParents;
        }

        public float[] singlePointCrossover(float[] parent1, float[] parent2)
        {
            int length = parent1.Length;
            Random rand = new Random();
            int crossoverPoint = rand.Next(1, length); //not 0 or length
            float[] child = new float[length];

            //Randomly choose which parent contributes the first half
            bool firstHalfFromParent1 = rand.NextDouble() < 0.5;
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
            Random random = new Random();
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
            var population =  this.population.GetGenomes();
            for (int i = 0; i < population.Length; i++)
            {
                population[i].evaluateFitness();
                var fitness = population[i].getFitness();
                //Console.WriteLine(fitness);
            }

            //select parents 
            Genome[] parents = selectParents();
            //Console.WriteLine("Parent length: " + parents.Length);

            //perform crossover + build new generation
            Genome[] newPopulation = new Genome[this.population.populationSize];
            Random rand = new Random();
            int count=parents.Length;
            int parentCount=0;
            //Console.WriteLine("count: "+count);
            for (int i = 0; i < this.population.populationSize; i++)
            {   
                if(i<this.population.populationSize-count){
                //Select two random parents
                Genome parent1 = parents[rand.Next(parents.Length)];
                Genome parent2 = parents[rand.Next(parents.Length)];
                //Avoid selecting same parent
                while (parent1 == parent2)
                    parent2 = parents[rand.Next(parents.Length)];

                //Perform crossover
                float[] childWeights = singlePointCrossover(parent1.getGenome(), parent2.getGenome());

                //Perform mutation
                childWeights = mutate(mutationRate: mutationRate,childWeights);

                //Create new genome
                newPopulation[i] = new Genome(childWeights);
                }else{
                    newPopulation[i] = parents[parentCount];
                    parentCount++;
                }
            }
            count++;
    
            //Console.WriteLine("---------------------");
            Console.WriteLine("new population size: "+newPopulation.Length);

            //Store best fit 
            float bestFitness = -1000;
            for (int i = 0; i < newPopulation.Length; i++)
            {
                float fitness = newPopulation[i].getFitness();
                if (bestFitness == -1000 || fitness > bestFitness)
                {
                    bestFitness=fitness;
                }
            }
            this.bestFitness=bestFitness;
        }
    }

    public class Genome
    {

        public float fitnessScore;
        public float[] weights; // At start initialize weights (they are equal to the amount of neurons in NN)
        public Genome(int numWeights) //Initialize random weights
        {
            //Initialize weights with random values between -1 and 1
            Random random = new Random();
            weights = new float[numWeights];
            for (int i = 0; i < numWeights; i++)
            {
                weights[i] = (float)(random.NextDouble() * 2 - 1); // Random between -1 and 1
            }
            setGenome(weights);
        }
        public Genome(float[] weights) //Initialize with predefined weights 
        {
            this.weights = weights;
        }
        public float[] getGenome()
        {
            return this.weights;
        }
        public void setGenome(float[] weights)
        {
            this.weights = weights;
        }
        public void evaluateFitness()
        {
            float errorSum = 0;
            int numSamples = 100; // Number of samples to test the polynomial on
            float xMin = -10f; // Minimum x value for testing
            float xMax = 10f;  // Maximum x value for testing
            float step = (xMax - xMin) / numSamples; // Step size to sample values of x

            // Iterate over x values to calculate the error (MSE)
            for (float x = xMin; x <= xMax; x += step)
            {
                // Genome represents the polynomial coefficients: a, b, c (for a*x^2 + b*x + c)
                float polynomialValue = this.weights[0] * x * x + this.weights[1] * x + this.weights[2]; // ax^2 + bx + c
                float targetValue = TargetPolynomial(x); // Target polynomial value

                // Compute the squared error between the genome's polynomial and the target polynomial
                errorSum += (polynomialValue - targetValue) * (polynomialValue - targetValue);
            }

            // Return the mean squared error (MSE)
            this.fitnessScore = errorSum/numSamples;
        }
        public float getFitness(){
            return this.fitnessScore;
        }
        public static float TargetPolynomial(float x)
        {
            return x * x + 3 * x + 2;
        }
    }

    public class Population
    {
        private Genome[] genomes;
        public int populationSize;
        private int genomeSize;

        public Population(int populationSize, int genomeSize)
        {
            this.populationSize = populationSize;
            this.genomeSize = genomeSize;
            InitializePopulation();
        }

        private void InitializePopulation()
        {
            Genome[] genomes = new Genome[populationSize];

            for (int i = 0; i < populationSize; i++)
            {
                genomes[i] = new Genome(genomeSize); // Initialize each genome with random weights
            }
            this.genomes = genomes;
        }

        public Genome[] GetGenomes()
        {
            return this.genomes;
        }
    }
}
