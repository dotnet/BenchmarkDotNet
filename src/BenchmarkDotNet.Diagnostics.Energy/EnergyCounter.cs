namespace BenchmarkDotNet.Diagnosers
{
    internal abstract class EnergyCounter
    {
        public EnergyCounter(string name, string id)
        {
            Name = !string.IsNullOrEmpty(name) ? name : throw new ArgumentException(nameof(name));
            Id = !string.IsNullOrEmpty(id) ? id : throw new ArgumentException(nameof(id));
        }

        public abstract (bool, string) TestRead();

        public abstract void FixStart();

        public abstract void FixFinish();

        public abstract long GetValue();

        public string Name { get; protected set; }

        public string Id { get; protected set; }
    }
}
