using BenchmarkDotNet.Running;
using System.Reflection;
using System.Threading;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Portability
{
    /// <summary>
    /// .NET Standard 1.3 does not expose few things that are available for the frameworks that we target
    /// </summary>
    internal interface IDotNetStandardWorkarounds
    {
        Thread CurrentThread { get; }

        int ThreadPriorityHighest { get; }

        string GetLocation(Assembly assembly);

        AssemblyName[] GetReferencedAssemblies(Assembly assembly);

        int GetPriority(Thread thread); // we are not using ThreadPriority because it's available in .NET Standard 2.0+

        void SetPriority(Thread thread, int priority, ILogger logger); // we are not using ThreadPriority because it's available in .NET Standard 2.0+

        void SetApartmentState(Thread thread, Benchmark benchmark);
    }
}