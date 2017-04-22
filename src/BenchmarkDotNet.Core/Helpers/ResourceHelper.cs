using System;
using System.IO;
using System.Reflection;

namespace BenchmarkDotNet.Helpers
{
    internal class ResourceHelper
    {
        private readonly string resourcesNamespacePrefix;
        private readonly Assembly assembly;

        public static ResourceHelper CoreHelper = new ResourceHelper("BenchmarkDotNet.Templates.", typeof(ResourceHelper).GetTypeInfo().Assembly);

        internal ResourceHelper(string prefix1, Assembly assembly)
        {
            this.resourcesNamespacePrefix = prefix1;
            this.assembly = assembly;
        }

        internal string LoadTemplate(string name)
        {
            var resourceName = resourcesNamespacePrefix + name;
            using (var stream = GetResouceStream(resourceName))
            {
                if (stream == null)
                    throw new Exception($"Resource {resourceName} not found");
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }

        internal byte[] LoadBinaryFile(string name)
        {
            var resourceName = resourcesNamespacePrefix + name;
            using (var stream = GetResouceStream(resourceName))
            {
                if (stream == null)
                    throw new Exception($"Resource {resourceName} not found");
                using (var reader = new BinaryReader(stream))
                    return reader.ReadBytes((int)stream.Length);
            }
        }

        private Stream GetResouceStream(string resourceName)
        {
            return assembly.GetManifestResourceStream(resourceName);
        }
    }
}