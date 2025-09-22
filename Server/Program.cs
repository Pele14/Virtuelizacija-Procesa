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

            // 2) Pretplati se na evente (tačka 8 – osnovni događaji)
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

            // 3) Pretplate na tačku 9 (ΔP analitika)
            service.PressureSpike += (s, e) =>
            {
                Console.WriteLine($">>> [EVT][ΔP SPIKE] ΔP={e.DeltaP}, P={e.CurrentPressure}, Mean={e.MeanPressure} ({e.Direction})");
            };

            service.OutOfBandWarning += (s, e) =>
            {
                Console.WriteLine($">>> [EVT][OUT OF BAND] P={e.CurrentPressure}, Mean={e.MeanPressure} ({e.Direction})");
            };

            // 4) Pretplate na tačku 10 (ΔVPact i ΔVPdef analitika)
            service.VPActSpike += (s, e) =>
            {
                Console.WriteLine($">>> [EVT][ΔVPact SPIKE] Δ={e.Delta}, VPact={e.CurrentValue} @ {e.Timestamp:O}");
            };

            service.VPDefSpike += (s, e) =>
            {
                Console.WriteLine($">>> [EVT][ΔVPdef SPIKE] Δ={e.Delta}, VPdef={e.CurrentValue} @ {e.Timestamp:O}");
            };

            // 5) Hostuj baš tu instancu
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
