namespace BenchmarkDotNet.Portability
{
    internal static class StreamWriter
    {
        internal static System.IO.StreamWriter FromPath(string path)
        {
            return new System.IO.StreamWriter(System.IO.File.OpenWrite(path));
        }
    }
}