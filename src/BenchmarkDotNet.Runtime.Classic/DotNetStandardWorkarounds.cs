using System;
using System.Linq;
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

        public int ThreadPriorityHighest => (int)ThreadPriority.Highest;

        public string GetLocation(Assembly assembly) => assembly.Location;

        public AssemblyName[] GetReferencedAssemblies(Assembly assembly) => assembly.GetReferencedAssemblies();

        public int GetPriority(Thread thread) => (int)thread.Priority;

        public void SetPriority(Thread thread, int priorityValue, ILogger logger)
        {
            ThreadPriority priority = (ThreadPriority)priorityValue;
            if (thread == null)
                throw new ArgumentNullException(nameof(thread));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            try
            {
                thread.Priority = priority;
            }
            catch (Exception ex)
            {
                logger.WriteLineError(
                    $"// ! Failed to set up priority {priority} for thread {thread}. Make sure you have the right permissions. Message: {ex.Message}");
            }
        }

        public void SetApartmentState(Thread thread, Benchmark benchmark)
        {
            if (benchmark.Target.Method.GetCustomAttributes<STAThreadAttribute>(false).Any())
            {
                thread.SetApartmentState(ApartmentState.STA);
            }
        }
    }
}