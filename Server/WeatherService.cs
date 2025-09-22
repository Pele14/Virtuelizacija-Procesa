using Common;
using System;
using System.Globalization;
using System.IO;
using System.ServiceModel;
using System.Xml; // za XmlDocument

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

        // ====== EVENT-ovi (tačka 9) ======
        public event EventHandler<PressureEventArgs> PressureSpike;
        public event EventHandler<PressureEventArgs> OutOfBandWarning;

        // ====== EVENT-ovi (tačka 10) ======
        public event EventHandler<VaporEventArgs> VPActSpike;
        public event EventHandler<VaporEventArgs> VPDefSpike;

        private FileWriter sessionWriter;
        private FileWriter rejectsWriter;
        private bool sessionActive = false;
        private bool disposed = false;

        private string sessionFilePath;
        private string rejectsFilePath;
        private string lastMeta;

        // ====== Polja za analitiku pritiska (tačka 9) ======
        private double? lastPressure = null;
        private double runningSum = 0;
        private int sampleCount = 0;
        private readonly double pressureThreshold;

        // ====== Polja za analitiku VPact i VPdef (tačka 10) ======
        private double? lastVPact = null;
        private double? lastVPdef = null;
        private readonly double vpactThreshold;
        private readonly double vpdefThreshold;

        public WeatherService()
        {
            // Ručno parsiranje appSettings iz App.config
            pressureThreshold = ReadThresholdFromConfig("P_threshold", 5.0);
            vpactThreshold = ReadThresholdFromConfig("VPact_threshold", 0.5);
            vpdefThreshold = ReadThresholdFromConfig("VPdef_threshold", 0.5);
        }

        private double ReadThresholdFromConfig(string key, double defaultVal)
        {
            try
            {
                var configPath = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
                var doc = new XmlDocument();
                doc.Load(configPath);

                var node = doc.SelectSingleNode($"//appSettings/add[@key='{key}']");
                if (node?.Attributes?["value"] != null &&
                    double.TryParse(node.Attributes["value"].Value, out double val))
                {
                    return val;
                }
            }
            catch { }
            return defaultVal;
        }

        // --- Helpers za evente
        protected virtual void OnTransferStarted(string sessionFile, string meta)
            => TransferStarted?.Invoke(this, new TransferEventArgs(sessionFile, meta, DateTime.UtcNow));

        protected virtual void OnSampleReceived(WeatherSample sample)
            => SampleReceived?.Invoke(this, new SampleEventArgs(sample, DateTime.UtcNow));

        protected virtual void OnTransferCompleted(string sessionFile)
            => TransferCompleted?.Invoke(this, new TransferEventArgs(sessionFile, lastMeta, DateTime.UtcNow));

        protected virtual void OnWarningRaised(string code, string message, WeatherSample sample = null)
            => WarningRaised?.Invoke(this, new WarningEventArgs(code, message, sample, DateTime.UtcNow));

        protected virtual void OnPressureSpike(double currentPressure, double deltaP, double mean, string direction)
            => PressureSpike?.Invoke(this, new PressureEventArgs(currentPressure, deltaP, mean, direction));

        protected virtual void OnOutOfBandWarning(double currentPressure, double deltaP, double mean, string direction)
            => OutOfBandWarning?.Invoke(this, new PressureEventArgs(currentPressure, deltaP, mean, direction));

        protected virtual void OnVPActSpike(double current, double delta)
            => VPActSpike?.Invoke(this, new VaporEventArgs("VPact", current, delta, DateTime.UtcNow));

        protected virtual void OnVPDefSpike(double current, double delta)
            => VPDefSpike?.Invoke(this, new VaporEventArgs("VPdef", current, delta, DateTime.UtcNow));

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

            // Reset analitike
            lastPressure = null;
            runningSum = 0;
            sampleCount = 0;
            lastVPact = null;
            lastVPdef = null;

            OnTransferStarted(sessionFilePath, meta);

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
                OnSampleReceived(sample);

                // ====== Tačka 9 – ΔP analitika ======
                sampleCount++;
                runningSum += sample.Pressure;
                double mean = runningSum / sampleCount;

                if (lastPressure.HasValue)
                {
                    double deltaP = sample.Pressure - lastPressure.Value;

                    if (Math.Abs(deltaP) > pressureThreshold)
                    {
                        string direction = deltaP > 0 ? "iznad očekivanog" : "ispod očekivanog";
                        OnPressureSpike(sample.Pressure, deltaP, mean, direction);
                    }

                    if (sample.Pressure < 0.75 * mean || sample.Pressure > 1.25 * mean)
                    {
                        string direction = sample.Pressure > mean ? "iznad očekivane vrednosti" : "ispod očekivane vrednosti";
                        OnOutOfBandWarning(sample.Pressure, deltaP, mean, direction);
                    }
                }
                lastPressure = sample.Pressure;

                // ====== Tačka 10 – ΔVPact i ΔVPdef ======
                if (lastVPact.HasValue)
                {
                    double deltaVPact = sample.VPact - lastVPact.Value;
                    if (Math.Abs(deltaVPact) > vpactThreshold)
                        OnVPActSpike(sample.VPact, deltaVPact);
                }
                lastVPact = sample.VPact;

                if (lastVPdef.HasValue)
                {
                    double deltaVPdef = sample.VPdef - lastVPdef.Value;
                    if (Math.Abs(deltaVPdef) > vpdefThreshold)
                        OnVPDefSpike(sample.VPdef, deltaVPdef);
                }
                lastVPdef = sample.VPdef;

                return "ACK: Sample received.";
            }
            catch (Exception ex)
            {
                rejectsWriter.WriteLine($"Exception,{DateTime.Now},{ex.Message}");
                OnWarningRaised("WRITE_ERROR", $"Greška pri upisu: {ex.Message}", sample);
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
            Dispose();
            OnTransferCompleted(sessionFilePath);

            Console.WriteLine(">>> Prenos završen.");
            return "ACK: Session ended.";
        }

        // IDisposable
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
