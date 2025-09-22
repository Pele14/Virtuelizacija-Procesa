using System;
using System.ServiceModel;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            // hostovanje servisa preko ServiceHost
            using (ServiceHost host = new ServiceHost(typeof(WeatherService)))
            {
                try
                {
                    host.Open();
                    Console.WriteLine(">>> WeatherService server pokrenut.");
                    Console.WriteLine(">>> Endpoint: net.tcp://localhost:4000/WeatherService");
                    Console.WriteLine(">>> Čekam klijente...");

                    Console.WriteLine("Pritisni ENTER za izlaz.");
                    Console.ReadLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(">>> Greška prilikom pokretanja servera: " + ex.Message);
                }
                finally
                {
                    if (host.State == CommunicationState.Faulted)
                        host.Abort();
                    else
                        host.Close();
                }
            }
        }
    }
}
