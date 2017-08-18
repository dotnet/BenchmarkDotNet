namespace BenchmarkDotNet.Portability
{
    public class Antivirus
    {
        public Antivirus(string name, string path)
        {
            Name = name;
            Path = path;
        }

        public string Name { get; }
        public string Path { get; }

        public override string ToString() => $"{Name} ({Path})";
    }
}