namespace BenchmarkDotNet.Helpers
{
    public sealed class EnvironmentInfoItem
    {
        public EnvironmentInfoItem(string name, string value, int group)
        {
            Name = name;
            Value = value;
            Group = group;
        }

        public int Group { get; }

        public string Name { get; }

        public string Value { get; }

        public override string ToString()
        {
            return $"{Name}={Value}";
        }
    }
}