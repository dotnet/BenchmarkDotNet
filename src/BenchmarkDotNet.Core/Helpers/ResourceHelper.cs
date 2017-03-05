using System;
using System.IO;
using System.Reflection;

namespace BenchmarkDotNet.Helpers
{
    internal class ResourceHelper
    {
        private readonly string prefix1;
        private readonly string prefix2;
        private readonly Assembly assembly;

        public static ResourceHelper CoreHelper = new ResourceHelper("BenchmarkDotNet.Templates.", "BenchmarkDotNet.Core.Templates.", typeof(ResourceHelper).GetTypeInfo().Assembly);

        internal ResourceHelper(string prefix1, string prefix2, Assembly assembly)
        {
            this.prefix1 = prefix1;
            this.prefix2 = prefix2;
            this.assembly = assembly;
        }

        internal string LoadTemplate(string name)
        {
            var resourceName = prefix1 + name;
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
            var resourceName = prefix2 + name;
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