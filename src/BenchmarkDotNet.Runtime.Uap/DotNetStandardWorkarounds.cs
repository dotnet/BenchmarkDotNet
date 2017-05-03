using System;
using System.Reflection;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet
{
    internal class DotNetStandardWorkarounds : IDotNetStandardWorkarounds
    {
        public string GetLocation(Assembly assembly) => string.Empty;

        public AssemblyName[] GetReferencedAssemblies(Assembly assembly) => Array.Empty<AssemblyName>();
    }
}