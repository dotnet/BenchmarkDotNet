using System;
using System.Diagnostics;
using System.IO;

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

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
        /// <param name="benchmarkCase">Current benchmark.</param>
        /// <param name="logger">Logger for informational output.</param>
        /// <param name="diagnoser">Diagnosers, if attached.</param>
        public InProcessHost(BenchmarkCase benchmarkCase, ILogger logger, IDiagnoser diagnoser)
        {
            if (benchmarkCase == null)
                throw new ArgumentNullException(nameof(benchmarkCase));

            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.diagnoser = diagnoser;
            IsDiagnoserAttached = diagnoser != null;
            Config = benchmarkCase.Config;

            if (diagnoser != null)
                diagnoserActionParameters = new DiagnoserActionParameters(
                    Process.GetCurrentProcess(),
                    benchmarkCase,
                    default);
        }

        /// <summary><c>True</c> if there are diagnosers attached.</summary>
        /// <value><c>True</c> if there are diagnosers attached.</value>
        [PublicAPI] public bool IsDiagnoserAttached { get; }

        /// <summary>Results of the run.</summary>
        /// <value>Results of the run.</value>
        public RunResults RunResults { get; private set; }

        /// <summary>Current config</summary>
        [PublicAPI] public IConfig Config { get; set; }

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
            if (!IsDiagnoserAttached) // no need to send the signal, nobody is listening for it
                return;

            if (diagnoser == null)
                throw new NullReferenceException(nameof(diagnoser));

            diagnoser.Handle(hostSignal, diagnoserActionParameters);
        }

        public void SendError(string message) => logger.WriteLine(LogKind.Error, $"{ValidationErrorReporter.ConsoleErrorPrefix} {message}");

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

        public void Dispose()
        {
            // do nothing on purpose
        }
    }
}