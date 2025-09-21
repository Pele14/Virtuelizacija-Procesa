using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string datasetPath = @"C:\Datasets\weather.csv"; // ovde stavi tačnu putanju

            try
            {
                var samples = CsvReader.LoadSamples(datasetPath);

                Console.WriteLine($"Učitano {samples.Count} validnih redova.");
                Console.WriteLine("Prvi uzorak:");
                if (samples.Count > 0)
                {
                    var s = samples[0];
                    Console.WriteLine($"T={s.T}, Pressure={s.Pressure}, Date={s.Date}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška: {ex.Message}");
            }
        }
    }
}
