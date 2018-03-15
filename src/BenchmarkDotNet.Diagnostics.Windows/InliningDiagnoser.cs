﻿using System.Collections.Generic;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using Microsoft.Diagnostics.Tracing.Session;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public class InliningDiagnoser : JitDiagnoser
    {
        private static readonly string LogSeparator = new string('-', 20);

        private readonly bool logFailuresOnly = true;
        private readonly bool filterByNamespace = true;

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

        public override IEnumerable<string> Ids => new[] { nameof(InliningDiagnoser) };

        protected override void AttachToEvents(TraceEventSession session, Benchmark benchmark)
        {
            var expectedNamespace = benchmark.Target.Method.DeclaringType.Namespace;

            Logger.WriteLine();
            Logger.WriteLineHeader(LogSeparator);
            Logger.WriteLineInfo($"{benchmark.DisplayInfo}");
            Logger.WriteLineHeader(LogSeparator);

            session.Source.Clr.MethodInliningSucceeded += jitData =>
            {
                // Inliner = the parent method (the inliner calls the inlinee)
                // Inlinee = the method that is going to be "inlined" inside the inliner (it's caller)                
                if (StatsPerProcess.TryGetValue(jitData.ProcessID, out object ignored))
                {
                    var shouldPrint = !logFailuresOnly
                        && (!filterByNamespace
                            || jitData.InlinerNamespace.StartsWith(expectedNamespace)
                            || jitData.InlineeNamespace.StartsWith(expectedNamespace));

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
                if (StatsPerProcess.TryGetValue(jitData.ProcessID, out object ignored))
                {
                    var shouldPrint = !filterByNamespace
                                      || jitData.InlinerNamespace.StartsWith(expectedNamespace)
                                      || jitData.InlineeNamespace.StartsWith(expectedNamespace);

                    if (shouldPrint)
                    {
                        Logger.WriteLineError($"Inliner: {jitData.InlinerNamespace}.{jitData.InlinerName} - {jitData.InlinerNameSignature}");
                        Logger.WriteLineError($"Inlinee: {jitData.InlineeNamespace}.{jitData.InlineeName} - {jitData.InlineeNameSignature}");
                        // See https://blogs.msdn.microsoft.com/clrcodegeneration/2009/10/21/jit-etw-inlining-event-fail-reasons/
                        Logger.WriteLineError($"Fail Reason: {jitData.FailReason}");
                        Logger.WriteLineHeader(LogSeparator);
                    }
                }
            };
        }
    }
}
