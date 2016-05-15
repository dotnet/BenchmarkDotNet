using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Helpers
{
    public static class ResourceHelper
    {
        public static string LoadTemplate(string name)
        {
            var resourceName = "BenchmarkDotNet.Templates." + name;
            using (var stream = GetResouceStream(resourceName))
            {
                if (stream == null)
                    throw new Exception($"Resource {resourceName} not found");
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }

        public static Stream GetResouceStream(string resourceName)
        {
            return typeof(ResourceHelper).Assembly().GetManifestResourceStream(resourceName);
        }

        public static IEnumerable<string> GetAllResources(string prefix)
        {
            var assembly = typeof(ResourceHelper).Assembly();

            foreach (var resourceName in assembly.GetManifestResourceNames())
                if (resourceName.StartsWith(prefix, StringComparison.Ordinal))
                    yield return resourceName;
        }        
    }
}