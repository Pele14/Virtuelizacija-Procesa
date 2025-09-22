using System;

namespace Common
{
    public class VaporEventArgs : EventArgs
    {
        public string Type { get; }
        public double CurrentValue { get; }
        public double Delta { get; }
        public DateTime Timestamp { get; }

        public VaporEventArgs(string type, double currentValue, double delta, DateTime timestamp)
        {
            Type = type;
            CurrentValue = currentValue;
            Delta = delta;
            Timestamp = timestamp;
        }
    }
}
