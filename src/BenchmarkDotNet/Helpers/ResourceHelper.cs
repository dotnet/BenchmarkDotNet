using System;
using System.IO;
using System.Reflection;

namespace BenchmarkDotNet.Helpers
{
    internal static class ResourceHelper
    {
        internal static string LoadTemplate(string name) => LoadResource("BenchmarkDotNet.Templates." + name);

        internal static string LoadResource(string resourceName)
        {
            using (var stream = GetResourceStream(resourceName))
            {
                if (stream == null)
                    throw new Exception($"Resource {resourceName} not found");
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }

        private static Stream GetResourceStream(string resourceName)
        {
            return typeof(ResourceHelper).GetTypeInfo().Assembly.GetManifestResourceStream(resourceName);
        }
    }
}