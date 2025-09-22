using System;

namespace Common
{
    public class PressureEventArgs : EventArgs
    {
        public double CurrentPressure { get; }
        public double DeltaP { get; }
        public double MeanPressure { get; }
        public string Direction { get; }

        public PressureEventArgs(double currentPressure, double deltaP, double meanPressure, string direction)
        {
            CurrentPressure = currentPressure;
            DeltaP = deltaP;
            MeanPressure = meanPressure;
            Direction = direction;
        }
    }
}
