﻿using System;
using System.IO;
using System.Reflection;

namespace BenchmarkDotNet.Helpers
{
    internal static class ResourceHelper
    {
        internal static string LoadTemplate(string name)
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

        private static Stream GetResouceStream(string resourceName)
        {
            return typeof(ResourceHelper).GetTypeInfo().Assembly.GetManifestResourceStream(resourceName);
        }
    }
}