using System;
using Common;

namespace Server
{
    public class TransferEventArgs : EventArgs
    {
        public string SessionFile { get; }
        public string Meta { get; }
        public DateTime Timestamp { get; }

        public TransferEventArgs(string sessionFile, string meta, DateTime timestamp)
        {
            SessionFile = sessionFile;
            Meta = meta;
            Timestamp = timestamp;
        }
    }

    public class SampleEventArgs : EventArgs
    {
        public WeatherSample Sample { get; }
        public DateTime Timestamp { get; }

        public SampleEventArgs(WeatherSample sample, DateTime timestamp)
        {
            Sample = sample;
            Timestamp = timestamp;
        }
    }

    public class WarningEventArgs : EventArgs
    {
        public string Code { get; }
        public string Message { get; }
        public WeatherSample Sample { get; }
        public DateTime Timestamp { get; }

        public WarningEventArgs(string code, string message, WeatherSample sample, DateTime timestamp)
        {
            Code = code;
            Message = message;
            Sample = sample;
            Timestamp = timestamp;
        }
    }
}
