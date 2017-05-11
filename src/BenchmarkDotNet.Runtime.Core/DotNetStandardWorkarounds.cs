using System.Reflection;
using System.Threading;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet
{
    internal class DotNetStandardWorkarounds : IDotNetStandardWorkarounds
    {
        public Thread CurrentThread => Thread.CurrentThread;

        public int ThreadPriorityHighest => default(int);

        public string GetLocation(Assembly assembly) => assembly.Location;

        public AssemblyName[] GetReferencedAssemblies(Assembly assembly) => assembly.GetReferencedAssemblies();

        public int GetPriority(Thread thread) => default(int);

        public void SetPriority(Thread thread, int priority, ILogger logger) { }

        public void SetApartmentState(Thread thread, Benchmark benchmark) { }
    }
}