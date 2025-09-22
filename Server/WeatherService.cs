using Common;
using System;
using System.Globalization;
using System.IO;
using System.ServiceModel;

namespace Server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class WeatherService : IWeatherService, IDisposable
    {
        private FileWriter sessionWriter;
        private FileWriter rejectsWriter;
        private bool sessionActive = false;
        private bool disposed = false;

        public string StartSession(string meta)
        {
            if (sessionActive)
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "Sesija je već aktivna." });

            if (string.IsNullOrWhiteSpace(meta))
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault { Message = "Meta podaci su obavezni." });

            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            Directory.CreateDirectory(folder);

            string sessionFile = Path.Combine(folder, $"measurements_session_{DateTime.Now:yyyyMMddHHmmss}.csv");
            string rejectsFile = Path.Combine(folder, $"rejects_{DateTime.Now:yyyyMMddHHmmss}.csv");

            sessionWriter = new FileWriter(sessionFile);
            sessionWriter.WriteLine("T,Pressure,Tpot,Tdew,VPmax,VPdef,VPact,Date");

            rejectsWriter = new FileWriter(rejectsFile);
            rejectsWriter.WriteLine("Line,Reason,RawData");

            sessionActive = true;
            Console.WriteLine(">>> Nova sesija započeta. Prenos u toku...");
            return "ACK: Session started.";
        }

        public string PushSample(WeatherSample sample)
        {
            if (!sessionActive)
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "Nema aktivne sesije." });

            if (sample == null)
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault { Message = "Sample ne sme biti null." });

            if (sample.Pressure <= 0)
            {
                rejectsWriter.WriteLine($"Pressure<=0,{DateTime.Now},{sample.Pressure}");
                Console.WriteLine(">>> Odbačen sample (Pressure<=0)");
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "Pritisak mora biti pozitivan." });
            }

            if (sample.Date == default)
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault { Message = "Datum je obavezan." });

            try
            {
                string line = string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3},{4},{5},{6},{7:O}",
                    sample.T, sample.Pressure, sample.Tpot, sample.Tdew,
                    sample.VPmax, sample.VPdef, sample.VPact, sample.Date);

                sessionWriter.WriteLine(line);
                Console.WriteLine($">>> Sample primljen: Pressure={sample.Pressure}, Date={sample.Date}");

                return "ACK: Sample received.";
            }
            catch (Exception ex)
            {
                rejectsWriter.WriteLine($"Exception,{DateTime.Now},{ex.Message}");
                Console.WriteLine($">>> Greška: {ex.Message}");
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault { Message = $"Greška pri upisu: {ex.Message}" });
            }
        }

        public string EndSession()
        {
            if (!sessionActive)
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "Nema aktivne sesije za završavanje." });

            sessionActive = false;

            Dispose(); // oslobodi resurse

            Console.WriteLine(">>> Prenos završen.");
            return "ACK: Session ended.";
        }

        // IDisposable implementacija
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    sessionWriter?.Dispose();
                    rejectsWriter?.Dispose();
                }
                disposed = true;
            }
        }

        ~WeatherService()
        {
            Dispose(false);
        }
    }
}
