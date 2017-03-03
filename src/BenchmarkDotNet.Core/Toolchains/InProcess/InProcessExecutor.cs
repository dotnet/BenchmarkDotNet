using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.InProcess
{
    /// <summary>
    /// Implementation of <see cref="IExecutor" /> for in-process benchmarks.
    /// </summary>
    [PublicAPI]
    [SuppressMessage("ReSharper", "ArrangeBraces_using")]
    public class InProcessExecutor : IExecutor
    {
        private static readonly TimeSpan UnderDebuggerTimeout = TimeSpan.FromDays(1);

        /// <summary> Default timeout for in-process benchmarks. </summary>
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

        /// <summary>Initializes a new instance of the <see cref="InProcessExecutor" /> class.</summary>
        /// <param name="timeout">Timeout for the run.</param>
        /// <param name="codegenMode">Describes how benchmark action code is generated.</param>
        /// <param name="logOutput"><c>true</c> if the output should be logged.</param>
        public InProcessExecutor(TimeSpan timeout, BenchmarkActionCodegen codegenMode, bool logOutput)
        {
            if (timeout == TimeSpan.Zero)
                timeout = DefaultTimeout;

            ExecutionTimeout = timeout;
            CodegenMode = codegenMode;
            LogOutput = logOutput;
        }

        /// <summary>Timeout for the run.</summary>
        /// <value>The timeout for the run.</value>
        public TimeSpan ExecutionTimeout { get; }

        /// <summary>Describes how benchmark action code is generated.</summary>
        /// <value>Benchmark action code generation mode.</value>
        public BenchmarkActionCodegen CodegenMode { get; }

        /// <summary>Gets a value indicating whether the output should be logged.</summary>
        /// <value><c>true</c> if the output should be logged; otherwise, <c>false</c>.</value>
        public bool LogOutput { get; }

        /// <summary>Executes the specified benchmark.</summary>
        /// <param name="buildResult">The build result.</param>
        /// <param name="benchmark">The benchmark.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="resolver">The resolver.</param>
        /// <param name="diagnoser">The diagnoser.</param>
        /// <returns>Execution result.</returns>
        public ExecuteResult Execute(
            BuildResult buildResult, Benchmark benchmark, ILogger logger, IResolver resolver, IDiagnoser diagnoser = null)
        {
            // TODO: preallocate buffer for output (no direct logging)?
            var hostLogger = LogOutput ? logger : new CompositeLogger();
            var host = new InProcessHost(benchmark, hostLogger, diagnoser);

            int exitCode = -1;
            var runThread = new Thread(() => exitCode = ExecuteCore(host, benchmark, logger));
            if (benchmark.Target.Method.GetCustomAttributes<STAThreadAttribute>(false).Any())
            {
                // TODO: runThread.SetApartmentState(ApartmentState.STA);
            }
            runThread.IsBackground = true;

            var timeout = HostEnvironmentInfo.GetCurrent().HasAttachedDebugger ? UnderDebuggerTimeout : ExecutionTimeout;

            runThread.Start();

            // TODO: to timespan overload (not available for .Net Core for now).
            if (!runThread.Join((int) timeout.TotalMilliseconds))
                throw new InvalidOperationException(
                    $"Benchmark {benchmark.DisplayInfo} takes to long to run. " +
                    "Prefer to use out-of-process toolchains for long-running benchmarks.");

            return GetExecutionResult(host.RunResults, exitCode, logger);
        }

        private int ExecuteCore(IHost host, Benchmark benchmark, ILogger logger)
        {
            int exitCode = -1;
            var process = Process.GetCurrentProcess();
            var oldPriority = process.PriorityClass;
            var oldAffinity = process.ProcessorAffinity;

            var affinity = benchmark.Job.ResolveValueAsNullable(EnvMode.AffinityCharacteristic);
            try
            {
                process.TrySetPriority(ProcessPriorityClass.High, logger);
                if (affinity != null)
                {
                    process.TrySetAffinity(affinity.Value, logger);
                }
                // TODO: set thread priority to highest

                exitCode = InProcessRunner.Run(host, benchmark, CodegenMode);
            }
            catch (Exception ex)
            {
                logger.WriteLineError($"// ! {GetType().Name}, exception: {ex}");
            }
            finally
            {
                // TODO: restore thread priority

                process.TrySetPriority(oldPriority, logger);
                if (affinity != null)
                {
                    process.EnsureProcessorAffinity(oldAffinity);
                }
            }

            return exitCode;
        }

        private ExecuteResult GetExecutionResult(RunResults runResults, int exitCode, ILogger logger)
        {
            if (exitCode != 0)
            {
                return new ExecuteResult(true, exitCode, new string[0], new string[0]);
            }

            var lines = new List<string>();
            foreach (var measurement in runResults.GetMeasurements())
            {
                lines.Add(measurement.ToOutputLine());
            }
            lines.Add(runResults.GCStats.WithTotalOperations(runResults.TotalOperationsCount).ToOutputLine());

            return new ExecuteResult(true, 0, lines.ToArray(), new string[0]);
        }
    }
}