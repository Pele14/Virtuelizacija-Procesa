using Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Client
{
    public class CsvReader
    {
        public static List<WeatherSample> LoadSamples(string filePath, int maxRows = 100)
        {
            var samples = new List<WeatherSample>();
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "invalid_rows.log");

            if (!File.Exists(filePath))
                throw new FileNotFoundException("CSV fajl nije pronađen.", filePath);

            using (var sr = new StreamReader(filePath))
            using (var logWriter = new StreamWriter(logPath, append: true))
            {
                int rowCount = 0;
                int lineNumber = 0;

                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    lineNumber++;

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var parts = line.Split(',');
                    if (parts.Length < 8)
                    {
                        logWriter.WriteLine($"[Line {lineNumber}] Premalo kolona: {line}");
                        continue;
                    }

                    try
                    {
                        var sample = new WeatherSample
                        {
                            T = double.Parse(parts[0], CultureInfo.InvariantCulture),
                            Pressure = double.Parse(parts[1], CultureInfo.InvariantCulture),
                            Tpot = double.Parse(parts[2], CultureInfo.InvariantCulture),
                            Tdew = double.Parse(parts[3], CultureInfo.InvariantCulture),
                            VPmax = double.Parse(parts[4], CultureInfo.InvariantCulture),
                            VPdef = double.Parse(parts[5], CultureInfo.InvariantCulture),
                            VPact = double.Parse(parts[6], CultureInfo.InvariantCulture),
                            Date = DateTime.Parse(parts[7], CultureInfo.InvariantCulture)
                        };

                        samples.Add(sample);
                        rowCount++;
                    }
                    catch (Exception ex)
                    {
                        logWriter.WriteLine($"[Line {lineNumber}] Parse error: {ex.Message} | Raw: {line}");
                    }

                    if (rowCount >= maxRows)
                        break;
                }
            }

            return samples;
        }
    }
}
