using MyProject.Algorithms;
namespace MyProject
{
    class Program
    {
        static void Main(string[] args)
        {  
            int k = 5;
            int populationSize = 150;
            int numOfWeights = 3;
            float parentSelectionPersentage = 0.10f;
            float mutationRate = 0.01f;
            GeneticAlgorithm ga = new GeneticAlgorithm(populationSize, numOfWeights,k, parentSelectionPersentage,mutationRate);

            //Get the population and print genomes
            var population = ga.population.GetGenomes();
            for (int i = 0; i < population.Length; i++)
            {
                Console.WriteLine($"Genome {i + 1}:");
                float[] weights = population[i].getGenome();
                foreach (var weight in weights)
                {
                    Console.WriteLine(weight);
                }
            }

            // Run the Genetic Algorithm
            int count=1;
            while(count!=30){
                ga.Run();
                Console.WriteLine("Populatioin: " + count + "best fit: " + ga.bestFitness);
                count++;
            }
    
        }
    }
}
