using System.Text;

namespace BenchmarkDotNet.Portability
{
    internal static class StreamWriter
    {
        internal static System.IO.StreamWriter FromPath(string path, bool append = false, Encoding encoding = null) 
            => encoding != null
                ? new System.IO.StreamWriter(path, append, encoding) 
                : new System.IO.StreamWriter(path, append);
    }
}