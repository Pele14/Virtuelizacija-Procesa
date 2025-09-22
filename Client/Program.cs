using Common;
using System;
using System.ServiceModel;
using System.IO;

namespace Client
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

            try
            {
                Console.WriteLine(proxy.StartSession(meta));
            }
            catch (FaultException<DataFormatFault> ex)
            {
                Console.WriteLine("DataFormatFault (StartSession): " + ex.Detail.Message);
                return;
            }
            catch (FaultException<ValidationFault> ex)
            {
                Console.WriteLine("ValidationFault (StartSession): " + ex.Detail.Message);
                return;
            }

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
                try
                {
                    string response = proxy.PushSample(sample);
                    Console.WriteLine(response);
                }
                catch (FaultException<DataFormatFault> ex)
                {
                    Console.WriteLine("DataFormatFault: " + ex.Detail.Message);
                }
                catch (FaultException<ValidationFault> ex)
                {
                    Console.WriteLine("ValidationFault: " + ex.Detail.Message);
                }

                System.Threading.Thread.Sleep(200); // simulacija realnog prenosa
            }

            try
            {
                Console.WriteLine(proxy.EndSession());
            }
            catch (FaultException<ValidationFault> ex)
            {
                Console.WriteLine("ValidationFault (EndSession): " + ex.Detail.Message);
            }

            Console.WriteLine(">>> Kraj programa.");
        }
    }
}
