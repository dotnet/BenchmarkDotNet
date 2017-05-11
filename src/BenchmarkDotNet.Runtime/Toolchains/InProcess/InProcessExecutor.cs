﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Parameters;
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
        /// <summary> Default timeout for in-process benchmarks. </summary>
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

        private static readonly TimeSpan UnderDebuggerTimeout = TimeSpan.FromDays(1);
        private readonly IDotNetStandardWorkarounds dotNetStandardWorkarounds;

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
            dotNetStandardWorkarounds = ServicesProvider.DotNetStandardWorkarounds;
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
        public ExecuteResult Execute(ExecuteParameters executeParameters)
        {
            // TODO: preallocate buffer for output (no direct logging)?
            var hostLogger = LogOutput ? executeParameters.Logger : NullLogger.Instance;
            var host = new InProcessHost(executeParameters.Benchmark, hostLogger, executeParameters.Diagnoser, executeParameters.Config);

            int exitCode = -1;
            var runThread = new Thread(() => exitCode = ExecuteCore(host, executeParameters.Benchmark, executeParameters.Logger));

            dotNetStandardWorkarounds.SetApartmentState(runThread, executeParameters.Benchmark);
            runThread.IsBackground = true;

            var timeout = HostEnvironmentInfo.GetCurrent().HasAttachedDebugger ? UnderDebuggerTimeout : ExecutionTimeout;

            runThread.Start();

            if (!runThread.Join((int)timeout.TotalMilliseconds))
                throw new InvalidOperationException(
                    $"Benchmark {executeParameters.Benchmark.DisplayInfo} takes to long to run. " +
                    "Prefer to use out-of-process toolchains for long-running benchmarks.");

            return GetExecutionResult(host.RunResults, exitCode, executeParameters.Logger);
        }

        private int ExecuteCore(IHost host, Benchmark benchmark, ILogger logger)
        {
            int exitCode = -1;
            var process = Process.GetCurrentProcess();
            var oldPriority = process.PriorityClass;
            var oldAffinity = process.ProcessorAffinity;

            var thread = dotNetStandardWorkarounds.CurrentThread;
            var oldThreadPriority = dotNetStandardWorkarounds.GetPriority(thread);

            var affinity = benchmark.Job.ResolveValueAsNullable(EnvMode.AffinityCharacteristic);
            try
            {
                ProcessExtensions.TrySetPriority(process, ProcessPriorityClass.High, logger);

                dotNetStandardWorkarounds.SetPriority(thread, dotNetStandardWorkarounds.ThreadPriorityHighest, logger);

                if (affinity != null)
                {
                    ProcessExtensions.TrySetAffinity(process, affinity.Value, logger);
                }

                exitCode = InProcessRunner.Run(host, benchmark, CodegenMode);
            }
            catch (Exception ex)
            {
                logger.WriteLineError($"// ! {GetType().Name}, exception: {ex}");
            }
            finally
            {
                ProcessExtensions.TrySetPriority(process, oldPriority, logger);

                dotNetStandardWorkarounds.SetPriority(thread, oldThreadPriority, logger);

                if (affinity != null)
                {
                    ProcessExtensions.EnsureProcessorAffinity(process, oldAffinity);
                }
            }

            return exitCode;
        }

        private ExecuteResult GetExecutionResult(RunResults runResults, int exitCode, ILogger logger)
        {
            if (exitCode != 0)
            {
                return new ExecuteResult(true, exitCode, Array.Empty<string>(), Array.Empty<string>());
            }

            var lines = new List<string>();
            foreach (var measurement in runResults.GetMeasurements())
            {
                lines.Add(measurement.ToOutputLine());
            }
            lines.Add(runResults.GCStats.WithTotalOperations(runResults.TotalOperationsCount).ToOutputLine());

            return new ExecuteResult(true, 0, lines.ToArray(), Array.Empty<string>());
        }
    }
}