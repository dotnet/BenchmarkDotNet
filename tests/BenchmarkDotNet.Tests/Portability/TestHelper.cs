using System.IO;
using System.Reflection;

namespace BenchmarkDotNet.Tests.Portability
{
    public static class TestHelper
    {
        public static string ReadTestFile(string name, string deviceType)
        {
            var assembly = typeof(TestHelper).GetTypeInfo().Assembly;
            string resourceName = $"{typeof(TestHelper).Namespace}.{deviceType}.TestFiles.{name}";

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }        
    }
}