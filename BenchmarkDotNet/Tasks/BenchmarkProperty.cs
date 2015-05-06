namespace BenchmarkDotNet.Tasks
{
    public class BenchmarkProperty
    {
        public string Name { get; }
        public string Value { get; }

        public BenchmarkProperty(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public override string ToString()
        {
            return $"{Name}={Value}";
        }
    }
}