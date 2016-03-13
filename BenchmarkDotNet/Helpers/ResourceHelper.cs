using System;
using System.IO;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Helpers
{
    public static class ResourceHelper
    {
        public static string LoadTemplate(string name)
        {
            var assembly = typeof(ResourceHelper).Assembly();
            var resourceName = "BenchmarkDotNet.Templates." + name;
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new Exception($"Resource {resourceName} not found");
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }
    }
}