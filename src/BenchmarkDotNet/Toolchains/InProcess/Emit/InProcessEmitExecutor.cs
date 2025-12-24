using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation;
using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit
{
    internal class InProcessEmitExecutor(bool executeOnSeparateThread) : IExecutor
    {
        public ExecuteResult Execute(ExecuteParameters executeParameters)
        {
            var host = new InProcessHost(executeParameters.BenchmarkCase, executeParameters.Logger, executeParameters.Diagnoser);

            int exitCode = -1;
            if (executeOnSeparateThread)
            {
                var runThread = new Thread(() => exitCode = ExecuteCore(host, executeParameters));

                if (executeParameters.BenchmarkCase.Descriptor.WorkloadMethod.GetCustomAttributes<STAThreadAttribute>(false).Any()
                    && OsDetector.IsWindows())
                {
                    runThread.SetApartmentState(ApartmentState.STA);
                }

                runThread.IsBackground = true;

                runThread.Start();
                runThread.Join();
            }
            else
            {
                exitCode = ExecuteCore(host, executeParameters);
            }
            host.HandleInProcessDiagnoserResults(executeParameters.BenchmarkCase, executeParameters.CompositeInProcessDiagnoser);

            return ExecuteResult.FromRunResults(host.RunResults, exitCode);
        }

        private int ExecuteCore(IHost host, ExecuteParameters parameters)
        {
            int exitCode = -1;
            var process = Process.GetCurrentProcess();
            var oldPriority = process.PriorityClass;
            var oldAffinity = process.TryGetAffinity();
            var thread = Thread.CurrentThread;
            var oldThreadPriority = thread.Priority;

            var affinity = parameters.BenchmarkCase.Job.ResolveValueAsNullable(EnvironmentMode.AffinityCharacteristic);
            try
            {
                process.TrySetPriority(ProcessPriorityClass.High, parameters.Logger);
                thread.TrySetPriority(ThreadPriority.Highest, parameters.Logger);

                if (affinity != null)
                {
                    process.TrySetAffinity(affinity.Value, parameters.Logger);
                }

                var generatedAssembly = ((InProcessEmitArtifactsPath)parameters.BuildResult.ArtifactsPaths)
                    .GeneratedAssembly;

                exitCode = RunnableProgram.Run(generatedAssembly, host, parameters);
            }
            catch (Exception ex)
            {
                parameters.Logger.WriteLineError($"// ! {GetType().Name}, exception: {ex}");
            }
            finally
            {
                process.TrySetPriority(oldPriority, parameters.Logger);
                thread.TrySetPriority(oldThreadPriority, parameters.Logger);

                if (affinity != null && oldAffinity != null)
                {
                    process.TrySetAffinity(oldAffinity.Value, parameters.Logger);
                }
            }

            return exitCode;
        }
    }
}