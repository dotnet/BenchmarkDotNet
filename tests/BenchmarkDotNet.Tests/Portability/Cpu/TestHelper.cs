using System.IO;
using System.Reflection;

namespace BenchmarkDotNet.Tests.Portability.Cpu
{
    public static class TestHelper
    {
        public static string ReadTestFile(string name)
        {
            var assembly = typeof(TestHelper).GetTypeInfo().Assembly;
            string resourceName = $"{typeof(TestHelper).Namespace}.TestFiles.{name}";

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}