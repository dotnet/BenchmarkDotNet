using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit
{
    internal class InProcessEmitExecutor(bool executeOnSeparateThread) : IExecutor
    {
        public async ValueTask<ExecuteResult> ExecuteAsync(ExecuteParameters executeParameters, CancellationToken cancellationToken)
        {
            var host = new InProcessHost(executeParameters.BenchmarkCase, executeParameters.Logger, executeParameters.Diagnoser, cancellationToken);

            int exitCode = -1;
            if (executeOnSeparateThread)
            {
                var taskCompletionSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                var runThread = new Thread(async () =>
                {
                    try
                    {
                        taskCompletionSource.SetResult(await ExecuteCore(host, executeParameters).ConfigureAwait(false));
                    }
                    catch (Exception ex)
                    {
                        taskCompletionSource.SetException(ex);
                    }
                });

                if (executeParameters.BenchmarkCase.Descriptor.WorkloadMethod.GetCustomAttributes<STAThreadAttribute>(false).Any()
                    && OsDetector.IsWindows())
                {
                    runThread.SetApartmentState(ApartmentState.STA);
                }

                runThread.IsBackground = true;

                runThread.Start();

                exitCode = await taskCompletionSource.Task.ConfigureAwait(true);
                runThread.Join();
            }
            else
            {
                exitCode = await ExecuteCore(host, executeParameters).ConfigureAwait(true);
            }
            host.HandleInProcessDiagnoserResults(executeParameters.BenchmarkCase, executeParameters.CompositeInProcessDiagnoser);

            return ExecuteResult.FromRunResults(host.RunResults, exitCode);
        }

        private async ValueTask<int> ExecuteCore(IHost host, ExecuteParameters parameters)
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

                exitCode = await InProcessEmitRunner.Run(host, parameters).ConfigureAwait(false);
            }
            catch (Exception ex) when (!ExceptionHelper.IsProperCancelation(ex, host.CancellationToken))
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