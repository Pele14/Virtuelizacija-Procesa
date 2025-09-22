using System;
using System.ServiceModel;
using System.IO;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            // 1) Napravi JEDNU instancu servisa (bitno zbog InstanceContextMode.Single)
            var service = new WeatherService();

            // 2) Pretplati se na evente (tačka 8)
            service.TransferStarted += (s, e) =>
            {
                Console.WriteLine($">>> [EVT] Start: {Path.GetFileName(e.SessionFile)} @ {e.Timestamp:O}");
            };

            service.SampleReceived += (s, e) =>
            {
                Console.WriteLine($">>> [EVT] Sample: P={e.Sample.Pressure} @ {e.Sample.Date:O}");
            };

            service.TransferCompleted += (s, e) =>
            {
                Console.WriteLine($">>> [EVT] Completed: {Path.GetFileName(e.SessionFile)} @ {e.Timestamp:O}");
            };

            service.WarningRaised += (s, e) =>
            {
                Console.WriteLine($">>> [EVT][WARN:{e.Code}] {e.Message}");
            };

            // 3) Hostuj baš tu instancu
            using (ServiceHost host = new ServiceHost(service))
            {
                try
                {
                    host.Open();
                    Console.WriteLine(">>> WeatherService server pokrenut.");
                    Console.WriteLine(">>> Endpoint: net.tcp://localhost:4000/WeatherService");
                    Console.WriteLine(">>> Cekam klijente...");
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
