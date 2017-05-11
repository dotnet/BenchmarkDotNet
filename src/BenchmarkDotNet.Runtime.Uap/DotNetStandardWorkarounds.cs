using System;
using System.Reflection;
using System.Threading;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet
{
    internal class DotNetStandardWorkarounds : IDotNetStandardWorkarounds
    {
        public Thread CurrentThread => default(Thread);

        public int ThreadPriorityHighest => default(int);

        public string GetLocation(Assembly assembly) => string.Empty;

        public AssemblyName[] GetReferencedAssemblies(Assembly assembly) => Array.Empty<AssemblyName>();

        public int GetPriority(Thread thread) => default(int);

        public void SetPriority(Thread thread, int priority, ILogger logger) { }

        public void SetApartmentState(Thread thread, Benchmark benchmark) { }
    }
}