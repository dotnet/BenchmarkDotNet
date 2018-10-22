using JetBrains.Annotations;

namespace BenchmarkDotNet.Portability
{
    public class Antivirus
    {
        public Antivirus(string name, string path)
        {
            Name = name;
            Path = path;
        }

        [PublicAPI] public string Name { get; }
        [PublicAPI] public string Path { get; }

        public override string ToString() => $"{Name} ({Path})";
    }
}