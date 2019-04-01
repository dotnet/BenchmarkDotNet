using System;
using System.Reflection;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using static BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation.RunnableConstants;
using static BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation.RunnableReflectionHelpers;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation
{
    public class RunnableProgram
    {
        public static int Run(
            BenchmarkId benchmarkId,
            Assembly partitionAssembly,
            BenchmarkCase benchmarkCase,
            IHost host)
        {
            // the first thing to do is to let diagnosers hook in before anything happens
            // so all jit-related diagnosers can catch first jit compilation!
            host.BeforeAnythingElse();

            try
            {
                // we are not using Runnable here in any direct way in order to avoid strong dependency Main<=>Runnable
                // which could cause the jitting/assembly loading to happen before we do anything
                // we have some jitting diagnosers and we want them to catch all the informations!!

                var runCallback = GetRunCallback(benchmarkId, partitionAssembly);

                runCallback.Invoke(null, new object[] { benchmarkCase, host });
                return 0;
            }
            catch (Exception oom) when (
                oom is OutOfMemoryException ||
                oom is TargetInvocationException reflection && reflection.InnerException is OutOfMemoryException)
            {
                DumpOutOfMemory(host, oom);
                return -1;
            }
            catch (Exception ex)
            {
                DumpError(host, ex);
                return -1;
            }
            finally
            {
                host.AfterAll();
            }
        }

        private static MethodInfo GetRunCallback(
            BenchmarkId benchmarkId, Assembly partitionAssembly)
        {
            var runnableType = partitionAssembly.GetType(GetRunnableTypeName(benchmarkId));

            var runnableMethod = runnableType.GetMethod(RunMethodName, BindingFlagsPublicStatic);

            return runnableMethod;
        }

        private static string GetRunnableTypeName(BenchmarkId benchmarkId)
        {
            return EmittedTypePrefix + benchmarkId;
        }

        private static void DumpOutOfMemory(IHost host, Exception oom)
        {
            host.WriteLine();
            host.WriteLine("OutOfMemoryException!");
            host.WriteLine(
                "BenchmarkDotNet continues to run additional iterations until desired accuracy level is achieved. It's possible only if the benchmark method doesn't have any side-effects.");
            host.WriteLine(
                "If your benchmark allocates memory and keeps it alive, you are creating a memory leak.");
            host.WriteLine(
                "You should redesign your benchmark and remove the side-effects. You can use `OperationsPerInvoke`, `IterationSetup` and `IterationCleanup` to do that.");
            host.WriteLine();
            host.WriteLine(oom.ToString());
        }

        private static void DumpError(IHost host, Exception ex)
        {
            host.WriteLine();
            host.WriteLine(ex.ToString());
        }
    }
}