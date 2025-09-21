using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Common;


    namespace Server
    {
        public class WeatherService : IWeatherService
        {
            private FileWriter sessionWriter;
            private bool sessionActive = false;

            public string StartSession(string meta)
            {
                if (sessionActive)
                    return "ERROR: Session already active.";

                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                Directory.CreateDirectory(folder);

                string sessionFile = Path.Combine(folder, $"measurements_session_{DateTime.Now:yyyyMMddHHmmss}.csv");
                sessionWriter = new FileWriter(sessionFile);
                sessionWriter.WriteLine(meta);

                sessionActive = true;
                return "ACK: Session started.";
            }

            public string PushSample(WeatherSample sample)
            {
                if (!sessionActive)
                    return "NACK: No active session.";

                try
                {
                    sessionWriter.WriteLine($"{sample.T},{sample.Pressure},{sample.Tpot},{sample.Tdew},{sample.VPmax},{sample.VPdef},{sample.VPact},{sample.Date:O}");
                    return "ACK: Sample received.";
                }
                catch (Exception ex)
                {
                    return $"NACK: {ex.Message}";
                }
            }

            public string EndSession()
            {
                if (!sessionActive)
                    return "NACK: No active session.";

                sessionWriter?.Dispose();
                sessionWriter = null;

                sessionActive = false;
                return "ACK: Session ended.";
            }
        }
    }
