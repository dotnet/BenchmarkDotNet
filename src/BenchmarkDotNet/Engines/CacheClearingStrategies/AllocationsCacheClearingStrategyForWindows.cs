using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using Microsoft.Win32.SafeHandles;

namespace BenchmarkDotNet.Engines.CacheClearingStrategies
{
    internal class AllocationsCacheClearingStrategyForWindows : ICacheClearingStrategy
    {
        private Process proces =  new Process() { StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "C:\\Work\\BenchmarkDotNet\\BenchmarkDotNet.MemoryAllocator\\bin\\Release\\netcoreapp2.0\\BenchmarkDotNet.MemoryAllocator.dll",
            UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = false
            }
        };

        private int[] affinities;

        public AllocationsCacheClearingStrategyForWindows(IntPtr? affinity)
        {
            if (affinity.HasValue)
            {
                this.affinities = GetAffinitiesForSelectedProcessors(affinity.Value).ToArray();
            }
            else
            {
                this.affinities = GetAffinitiesForAllProcessors();
            }
        }

        public void ClearCache()
        {
            const int howManyProcessOnProcessor = 2;

            for (int i = 0; i < howManyProcessOnProcessor; i++)
            {
                foreach (int affinity in affinities)
                {
                    AllocateMemory(affinity);
                }
            }
        }

        private void AllocateMemory(int affinity)
        {  
            proces.Start();
            proces.TrySetPriority(ProcessPriorityClass.High, ConsoleLogger.Default);
            proces.TrySetAffinity((IntPtr)affinity, ConsoleLogger.Default);
            proces.WaitForExit();
            var exitCode = proces.ExitCode;
            System.IO.File.AppendAllText("c:\\work\\All.txt", "ExitCode= " + exitCode.ToString());
        }


        private static IEnumerable<int> GetAffinitiesForSelectedProcessors(IntPtr affinityPtr)
        {
            int cpuCount = Environment.ProcessorCount;

            int affinity = (int)affinityPtr;

            for (int i = 0; i < cpuCount; i++)
            {
                int cpuMask = 1 << i;

                if ((affinity & cpuMask) > 0)
                    yield return cpuMask;
            }
        }

        private int[] GetAffinitiesForAllProcessors()
        {
            int cpuCount = Environment.ProcessorCount;

            var result = new int[cpuCount];
            for (int i = 0; i < cpuCount; ++i)
                result[i] = 1 << i;

            return result;
        }
    }
}