using System;
using System.Diagnostics;
using System.IO;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.InProcess
{
    /// <summary>Host API for in-process benchmarks.</summary>
    /// <seealso cref="IHost"/>
    public sealed class InProcessHost : IHost
    {
        [NotNull]
        private readonly ILogger logger;

        [CanBeNull]
        private readonly IDiagnoser diagnoser;

        [CanBeNull]
        private readonly DiagnoserActionParameters diagnoserActionParameters;

        /// <summary>Creates a new instance of <see cref="InProcessHost"/>.</summary>
        /// <param name="benchmark">Current benchmark.</param>
        /// <param name="logger">Logger for informational output.</param>
        /// <param name="diagnoser">Diagnosers, if attached.</param>
        /// <param name="config">Current config.</param>
        public InProcessHost(Benchmark benchmark, ILogger logger, IDiagnoser diagnoser, IConfig config)
        {
            if (benchmark == null)
                throw new ArgumentNullException(nameof(benchmark));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            this.logger = logger;
            this.diagnoser = diagnoser;
            IsDiagnoserAttached = diagnoser != null;

            if (diagnoser != null)
                diagnoserActionParameters = new DiagnoserActionParameters(
                    Process.GetCurrentProcess(),
                    benchmark,
                    config);
        }

        /// <summary><c>True</c> if there are diagnosers attached.</summary>
        /// <value><c>True</c> if there are diagnosers attached.</value>
        public bool IsDiagnoserAttached { get; }

        /// <summary>Results of the run.</summary>
        /// <value>Results of the run.</value>
        public RunResults RunResults { get; private set; }

        /// <summary>Passes text to the host.</summary>
        /// <param name="message">Text to write.</param>
        public void Write(string message) => logger.Write(message);

        /// <summary>Passes new line to the host.</summary>
        public void WriteLine() => logger.WriteLine();

        /// <summary>Passes text (new line appended) to the host.</summary>
        /// <param name="message">Text to write.</param>
        public void WriteLine(string message) => logger.WriteLine(message);

        /// <summary>Sends notification signal to the host.</summary>
        /// <param name="hostSignal">The signal to send.</param>
        public void SendSignal(HostSignal hostSignal)
        {
            switch (hostSignal)
            {
                case HostSignal.BeforeAnythingElse:
                    diagnoser?.BeforeAnythingElse(diagnoserActionParameters);
                    WriteLine(Engine.Signals.BeforeAnythingElse);
                    break;
                case HostSignal.AfterGlobalSetup:
                    diagnoser?.AfterGlobalSetup(diagnoserActionParameters);
                    WriteLine(Engine.Signals.AfterGlobalSetup);
                    break;
                case HostSignal.BeforeMainRun:
                    diagnoser?.BeforeMainRun(diagnoserActionParameters);
                    WriteLine(Engine.Signals.BeforeMainRun);
                    break;
                case HostSignal.BeforeGlobalCleanup:
                    diagnoser?.BeforeGlobalCleanup(diagnoserActionParameters);
                    WriteLine(Engine.Signals.BeforeGlobalCleanup);
                    break;
                case HostSignal.AfterAnythingElse:
                    WriteLine(Engine.Signals.AfterAnythingElse);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(hostSignal), hostSignal, null);
            }
        }

        /// <summary>Submits run results to the host.</summary>
        /// <param name="runResults">The run results.</param>
        public void ReportResults(RunResults runResults)
        {
            RunResults = runResults;

            using (var w = new StringWriter())
            {
                runResults.Print(w);
                logger.Write(w.GetStringBuilder().ToString());
            }
        }
    }
}