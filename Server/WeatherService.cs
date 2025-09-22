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
        // ====== EVENT-ovi (tačka 8) ======
        public event EventHandler<TransferEventArgs> TransferStarted;
        public event EventHandler<SampleEventArgs> SampleReceived;
        public event EventHandler<TransferEventArgs> TransferCompleted;
        public event EventHandler<WarningEventArgs> WarningRaised;

        private FileWriter sessionWriter;
        private FileWriter rejectsWriter;
        private bool sessionActive = false;
        private bool disposed = false;

        private string sessionFilePath;
        private string rejectsFilePath;
        private string lastMeta;

        // --- Helpers za sigurno podizanje event-ova
        protected virtual void OnTransferStarted(string sessionFile, string meta)
            => TransferStarted?.Invoke(this, new TransferEventArgs(sessionFile, meta, DateTime.UtcNow));

        protected virtual void OnSampleReceived(WeatherSample sample)
            => SampleReceived?.Invoke(this, new SampleEventArgs(sample, DateTime.UtcNow));

        protected virtual void OnTransferCompleted(string sessionFile)
            => TransferCompleted?.Invoke(this, new TransferEventArgs(sessionFile, lastMeta, DateTime.UtcNow));

        protected virtual void OnWarningRaised(string code, string message, WeatherSample sample = null)
            => WarningRaised?.Invoke(this, new WarningEventArgs(code, message, sample, DateTime.UtcNow));

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

            sessionFilePath = Path.Combine(folder, $"measurements_session_{DateTime.Now:yyyyMMddHHmmss}.csv");
            rejectsFilePath = Path.Combine(folder, $"rejects_{DateTime.Now:yyyyMMddHHmmss}.csv");

            sessionWriter = new FileWriter(sessionFilePath);
            sessionWriter.WriteLine("T,Pressure,Tpot,Tdew,VPmax,VPdef,VPact,Date");

            rejectsWriter = new FileWriter(rejectsFilePath);
            rejectsWriter.WriteLine("Line,Reason,RawData");

            lastMeta = meta;
            sessionActive = true;

            // Event
            OnTransferStarted(sessionFilePath, meta);

            // (po želji) i dalje možemo ispisati u konzolu direktno
            Console.WriteLine(">>> Nova sesija započeta. Prenos u toku...");
            return "ACK: Session started.";
        }

        public string PushSample(WeatherSample sample)
        {
            if (!sessionActive)
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "Nema aktivne sesije." });

            if (sample == null)
            {
                OnWarningRaised("SAMPLE_NULL", "Sample ne sme biti null.");
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault { Message = "Sample ne sme biti null." });
            }

            if (sample.Pressure <= 0)
            {
                rejectsWriter.WriteLine($"Pressure<=0,{DateTime.Now},{sample?.Pressure}");
                OnWarningRaised("PRESSURE_INVALID", "Pritisak mora biti pozitivan.", sample);
                Console.WriteLine(">>> Odbačen sample (Pressure<=0)");
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "Pritisak mora biti pozitivan." });
            }

            if (sample.Date == default)
            {
                OnWarningRaised("DATE_MISSING", "Datum je obavezan.", sample);
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault { Message = "Datum je obavezan." });
            }

            try
            {
                string line = string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3},{4},{5},{6},{7:O}",
                    sample.T, sample.Pressure, sample.Tpot, sample.Tdew,
                    sample.VPmax, sample.VPdef, sample.VPact, sample.Date);

                sessionWriter.WriteLine(line);

                // Event
                OnSampleReceived(sample);

                Console.WriteLine($">>> Sample primljen: Pressure={sample.Pressure}, Date={sample.Date}");
                return "ACK: Sample received.";
            }
            catch (Exception ex)
            {
                rejectsWriter.WriteLine($"Exception,{DateTime.Now},{ex.Message}");
                OnWarningRaised("WRITE_ERROR", $"Greška pri upisu: {ex.Message}", sample);
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

            // Zatvori resurse
            Dispose();

            // Event
            OnTransferCompleted(sessionFilePath);

            Console.WriteLine(">>> Prenos završen.");
            return "ACK: Session ended.";
        }

        // ====== IDisposable implementacija (tačka 4) ======
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
                    sessionWriter = null;
                    rejectsWriter = null;
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
