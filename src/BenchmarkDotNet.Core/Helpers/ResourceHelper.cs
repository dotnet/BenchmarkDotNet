﻿using System;
using System.IO;
using System.Reflection;

namespace BenchmarkDotNet.Helpers
{
    internal static class ResourceHelper
    {
        internal static string LoadTemplate(string name)
        {
            var resourceName = "BenchmarkDotNet.Core.Templates." + name;
            using (var stream = GetResouceStream(resourceName))
            {
                if (stream == null)
                    throw new Exception($"Resource {resourceName} not found");
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }

        internal static byte[] LoadBinaryFile(string name)
        {
            var resourceName = "BenchmarkDotNet.Core.Templates." + name;
            using (var stream = GetResouceStream(resourceName))
            {
                if (stream == null)
                    throw new Exception($"Resource {resourceName} not found");
                using (var reader = new BinaryReader(stream))
                    return reader.ReadBytes((int)stream.Length);
            }
        }

        private static Stream GetResouceStream(string resourceName)
        {
            return typeof(ResourceHelper).GetTypeInfo().Assembly.GetManifestResourceStream(resourceName);
        }
    }
}