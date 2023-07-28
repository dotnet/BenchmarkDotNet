using System;
using System.Collections.Generic;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Helpers
{
    public static class ExternalToolsHelper
    {
        /// <summary>
        /// Output of the `system_profiler SPSoftwareDataType` command.
        /// MacOSX only.
        /// </summary>
        public static readonly Lazy<Dictionary<string, string>> MacSystemProfilerData =
            LazyParse(RuntimeInformation.IsMacOS, "system_profiler", "SPSoftwareDataType", s => SectionsHelper.ParseSection(s, ':'));

        private static Lazy<T> LazyParse<T>(Func<bool> isAvailable, string fileName, string arguments, Func<string, T> parseFunc)
        {
            return new Lazy<T>(() =>
            {
                string content = isAvailable() ? ProcessHelper.RunAndReadOutput(fileName, arguments) : "";
                return parseFunc(content);
            });
        }
    }
}