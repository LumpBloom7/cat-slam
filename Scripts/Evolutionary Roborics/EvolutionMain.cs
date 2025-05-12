using MyProject.Algorithms;
namespace MyProject
{
    class Program
    {
        static void Main(string[] args)
        {  
            int k = 5;
            int populationSize = 10;
            int numOfWeights = 3;
            float parentSelectionPersentage = 0.20f;
            float mutationRate = 0.02f;
            GeneticAlgorithm ga = new GeneticAlgorithm(populationSize, numOfWeights,k, parentSelectionPersentage,mutationRate);

            //Get the population and print genomes
            var population = ga.population.GetGenomes();
           
            // Run the Genetic Algorithm
            int count=1;
            while(count!=3){
                Console.WriteLine("--------------");
                Console.WriteLine("round: "+count);
                 for (int i = 0; i < population.Length; i++)
                {
                    Console.WriteLine($"Genome {i + 1}:");
                    float[] weights = population[i].getGenome();
                    foreach (var weight in weights)
                    {
                        Console.WriteLine(weight);
                    }
                }

                ga.Run();
                Console.WriteLine("Populatioin: " + count + "best fit: " + ga.bestFitness);
                count++;
            }
    
        }
    }
}
