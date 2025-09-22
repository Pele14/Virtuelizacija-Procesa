using Common;
using System;
using Client;
using System.ServiceModel;
using System.IO;

namespace ClientApp
{
    class Program
    {
        static void Main(string[] args)
        {
            ChannelFactory<IWeatherService> factory =
                new ChannelFactory<IWeatherService>("WeatherServiceEndpoint");
            var proxy = factory.CreateChannel();

            Console.WriteLine(">>> Pokrećem sesiju...");
            string meta = "T,Pressure,Tpot,Tdew,VPmax,VPdef,VPact,Date";
            Console.WriteLine(proxy.StartSession(meta));

            string datasetPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\Data\cleaned_weather.csv"
            );
            datasetPath = Path.GetFullPath(datasetPath);

            Console.WriteLine($">>> Pokušavam da učitam CSV sa putanje: {datasetPath}");

            var samples = CsvReader.LoadSamples(datasetPath);
            Console.WriteLine($">>> Učitano {samples.Count} uzoraka iz CSV fajla.");

            foreach (var sample in samples)
            {
                string response = proxy.PushSample(sample);
                Console.WriteLine(response);
                System.Threading.Thread.Sleep(200); // simulacija realnog prenosa
            }

            Console.WriteLine(proxy.EndSession());
            Console.WriteLine(">>> Kraj programa.");
        }
    }
}
