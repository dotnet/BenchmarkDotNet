namespace BenchmarkDotNet.Portability
{
    internal static class StreamWriter
    {
        internal static System.IO.StreamWriter FromPath(string path, bool append = false)
        {
#if !CORE
            return new System.IO.StreamWriter(path, append);
#else
            return new System.IO.StreamWriter(System.IO.File.OpenWrite(path));
#endif
        }
    }
}