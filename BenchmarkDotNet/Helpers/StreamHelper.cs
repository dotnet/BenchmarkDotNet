using System.IO;

namespace BenchmarkDotNet.Helpers
{
    public static class StreamHelper
    {
        public static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
                output.Write(buffer, 0, bytesRead);
        }
    }
}