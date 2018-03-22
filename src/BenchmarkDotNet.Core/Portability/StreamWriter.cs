namespace BenchmarkDotNet.Portability
{
    internal static class StreamWriter
    {
        internal static System.IO.StreamWriter FromPath(string path, bool append = false) => new System.IO.StreamWriter(path, append);
    }
}