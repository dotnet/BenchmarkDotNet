using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using Microsoft.Diagnostics.Tracing.Session;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public class InliningDiagnoser : JitDiagnoser<object>, IProfiler
    {
        private static readonly string LogSeparator = new string('-', 20);

        private readonly bool logFailuresOnly = true;
        private readonly bool filterByNamespace = true;
        private readonly string[]? allowedNamespaces = null;
        private string defaultNamespace;

        // ReSharper disable once EmptyConstructor parameterless ctor is mandatory for DiagnosersLoader.CreateDiagnoser
        public InliningDiagnoser() { }

        /// <summary>
        /// creates new InliningDiagnoser
        /// </summary>
        /// <param name="logFailuresOnly">only the methods that failed to get inlined. True by default.</param>
        /// <param name="filterByNamespace">only the methods from declaring type's namespace. Set to false if you want to see all Jit inlining events. True by default.</param>
        public InliningDiagnoser(bool logFailuresOnly = true, bool filterByNamespace = true)
        {
            this.logFailuresOnly = logFailuresOnly;
            this.filterByNamespace = filterByNamespace;
        }

        /// <summary>
        /// creates new InliningDiagnoser
        /// </summary>
        /// <param name="logFailuresOnly">only the methods that failed to get inlined. True by default.</param>
        /// <param name="allowedNamespaces">list of namespaces from which inlining message should be print.</param>
        public InliningDiagnoser(bool logFailuresOnly = true, string[]? allowedNamespaces = null)
        {
            this.logFailuresOnly = logFailuresOnly;
            this.allowedNamespaces = allowedNamespaces;
            this.filterByNamespace = true;
        }

        public override IEnumerable<string> Ids => new[] { nameof(InliningDiagnoser) };

        public string ShortName => "inlining";

        protected override void AttachToEvents(TraceEventSession session, BenchmarkCase benchmarkCase)
        {
            defaultNamespace = benchmarkCase.Descriptor.WorkloadMethod.DeclaringType.Namespace;

            Logger.WriteLine();
            Logger.WriteLineHeader(LogSeparator);
            Logger.WriteLineInfo($"{benchmarkCase.DisplayInfo}");
            Logger.WriteLineHeader(LogSeparator);

            session.Source.Clr.MethodInliningSucceeded += jitData =>
            {
                // Inliner = the parent method (the inliner calls the inlinee)
                // Inlinee = the method that is going to be "inlined" inside the inliner (it's caller)
                if (StatsPerProcess.TryGetValue(jitData.ProcessID, out _))
                {
                    var shouldPrint = !logFailuresOnly
                        && ShouldPrintEventInfo(jitData.InlinerNamespace, jitData.InlineeNamespace);

                    if (shouldPrint)
                    {
                        Logger.WriteLineHelp($"Inliner: {jitData.InlinerNamespace}.{jitData.InlinerName} - {jitData.InlinerNameSignature}");
                        Logger.WriteLineHelp($"Inlinee: {jitData.InlineeNamespace}.{jitData.InlineeName} - {jitData.InlineeNameSignature}");
                        Logger.WriteLineHeader(LogSeparator);
                    }
                }
            };

            session.Source.Clr.MethodInliningFailed += jitData =>
            {
                if (StatsPerProcess.TryGetValue(jitData.ProcessID, out _)
                    && ShouldPrintEventInfo(jitData.InlinerNamespace, jitData.InlineeNamespace))
                {
                    Logger.WriteLineError($"Inliner: {jitData.InlinerNamespace}.{jitData.InlinerName} - {jitData.InlinerNameSignature}");
                    Logger.WriteLineError($"Inlinee: {jitData.InlineeNamespace}.{jitData.InlineeName} - {jitData.InlineeNameSignature}");
                    // See https://blogs.msdn.microsoft.com/clrcodegeneration/2009/10/21/jit-etw-inlining-event-fail-reasons/
                    Logger.WriteLineError($"Fail Reason: {jitData.FailReason}");
                    Logger.WriteLineHeader(LogSeparator);
                }
            };

            session.Source.Clr.MethodInliningFailedAnsi += jitData => // this is new event exposed by .NET Core 2.2 https://github.com/dotnet/coreclr/commit/95a9055dbe5f6233f75ee2d7b6194e18cc4977fd
            {
                if (StatsPerProcess.TryGetValue(jitData.ProcessID, out _)
                    && ShouldPrintEventInfo(jitData.InlinerNamespace, jitData.InlineeNamespace))
                {
                    Logger.WriteLineError($"Inliner: {jitData.InlinerNamespace}.{jitData.InlinerName} - {jitData.InlinerNameSignature}");
                    Logger.WriteLineError($"Inlinee: {jitData.InlineeNamespace}.{jitData.InlineeName} - {jitData.InlineeNameSignature}");
                    // See https://blogs.msdn.microsoft.com/clrcodegeneration/2009/10/21/jit-etw-inlining-event-fail-reasons/
                    Logger.WriteLineError($"Fail Reason: {jitData.FailReason}");
                    Logger.WriteLineHeader(LogSeparator);
                }
            };
        }

        private bool ShouldPrintEventInfo(string inlinerNamespace, string inlineeNamespace)
            => !filterByNamespace ||
                (allowedNamespaces?.Any(x=> inlineeNamespace.StartsWith(x) || inlinerNamespace.StartsWith(x))
                    ?? (inlinerNamespace.StartsWith(defaultNamespace)) || inlineeNamespace.StartsWith(defaultNamespace));
    }
}
