using Common;
using System;
using System.IO;
using System.ServiceModel;
using System.Globalization;

namespace Server
{
    public class WeatherService : IWeatherService
    {
        private string sessionFile;
        private string rejectsFile;
        private bool sessionActive = false;

        public string StartSession(string meta)
        {
            if (sessionActive)
                return "ERROR: Session already active.";

            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            Directory.CreateDirectory(folder);

            // kreiraj fajlove za sesiju
            sessionFile = Path.Combine(folder, $"measurements_session_{DateTime.Now:yyyyMMddHHmmss}.csv");
            rejectsFile = Path.Combine(folder, $"rejects_{DateTime.Now:yyyyMMddHHmmss}.csv");

            File.WriteAllText(sessionFile, "T,Pressure,Tpot,Tdew,VPmax,VPdef,VPact,Date\n");
            File.WriteAllText(rejectsFile, "Line,Reason,RawData\n");

            sessionActive = true;
            return "ACK: Session started.";
        }

        public string PushSample(WeatherSample sample)
        {
            if (!sessionActive)
                return "NACK: No active session.";

            try
            {
                // validacija pritiska
                if (sample.Pressure <= 0)
                {
                    File.AppendAllText(rejectsFile, $"Pressure<=0,{DateTime.Now},{sample.Pressure}\n");
                    return "NACK: Invalid pressure.";
                }

                string line = string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3},{4},{5},{6},{7:O}",
                    sample.T, sample.Pressure, sample.Tpot, sample.Tdew,
                    sample.VPmax, sample.VPdef, sample.VPact, sample.Date);

                File.AppendAllText(sessionFile, line + Environment.NewLine);

                return "ACK: Sample received.";
            }
            catch (Exception ex)
            {
                File.AppendAllText(rejectsFile, $"Exception,{DateTime.Now},{ex.Message}\n");
                return $"NACK: {ex.Message}";
            }
        }

        public string EndSession()
        {
            if (!sessionActive)
                return "NACK: No active session.";

            sessionActive = false;
            return "ACK: Session ended.";
        }
    }
}
