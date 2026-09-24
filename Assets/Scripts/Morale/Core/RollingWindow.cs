using System.Collections.Generic;
namespace TheReckoning.Morale
{
    public sealed class RollingWindow
    {
        private readonly Queue<Sample> samples = new Queue<Sample>();
        private readonly struct Sample
        {
            public readonly double Time;
            public readonly float Value;
            public Sample(double time, float value)
            {
                Time = time;
                Value = value;
            }
        }
        public float Total
        {
            get; private set;
        }
        public void Add(double now, float value, double window)
        {
            Prune(now, window);
            samples.Enqueue(new Sample(now, value));
            Total += value;
        }
        public void Prune(double now, double window)
        {
            while (samples.Count > 0 && now - samples.Peek().Time > window)
                Total -= samples.Dequeue().Value;
            if (samples.Count == 0)
                Total = 0;
        }
        public void Clear()
        {
            samples.Clear();
            Total = 0;
        }
    }
}
