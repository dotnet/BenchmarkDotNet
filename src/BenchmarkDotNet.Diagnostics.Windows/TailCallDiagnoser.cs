﻿using System.Collections.Generic;
using BenchmarkDotNet.Running;
using Microsoft.Diagnostics.Tracing.Session;
using BenchmarkDotNet.Loggers;
using System;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    //See https://blogs.msdn.microsoft.com/clrcodegeneration/2009/05/11/jit-etw-tracing-in-net-framework-4/
    public class TailCallDiagnoser : JitDiagnoser
    {
        private static readonly string LogSeparator = new string('-', 20);
        public const string DiagnoserId = nameof(TailCallDiagnoser);
        public override IEnumerable<string> Ids => new[] { DiagnoserId };

        private readonly bool logFailuresOnly = true;
        private readonly bool filterByNamespace = true;
        private string expectedNamespace;

        public TailCallDiagnoser()
        {

        }
        /// <summary>
        /// creates the new TailCallDiagnoser instace
        /// </summary>
        /// <param name="logFailuresOnly">only the methods that failed to get tail called. True by default.</param>
        /// <param name="filterByNamespace">only the methods from declaring type's namespace. Set to false if you want to see all Jit tail events. True by default.</param>
        public TailCallDiagnoser(bool logFailuresOnly, bool filterByNamespace)
        {
            this.logFailuresOnly = logFailuresOnly;
            this.filterByNamespace = filterByNamespace;
        }

        protected override void AttachToEvents(TraceEventSession traceEventSession, Benchmark benchmark)
        {
            expectedNamespace = benchmark.Target.Method.DeclaringType.Namespace;

            Logger.WriteLine();
            Logger.WriteLineHeader(LogSeparator);
            Logger.WriteLineInfo($"{benchmark.DisplayInfo}");
            Logger.WriteLineHeader(LogSeparator);

            traceEventSession.Source.Clr.MethodTailCallSucceeded += jitData =>
            {
                if (!logFailuresOnly && ShouldPrintEventInfo(new Tuple<string, string>(jitData.CallerNamespace, jitData.CalleeNamespace)))
                {
                    if (StatsPerProcess.TryGetValue(jitData.ProcessID, out object ignored))
                    {
                        Logger.WriteLineHelp($"Caller: {jitData.CallerNamespace}.{jitData.CallerName} - {jitData.CallerNameSignature}");
                        Logger.WriteLineHelp($"Callee: {jitData.CalleeNamespace}.{jitData.CalleeName} - {jitData.CalleeNameSignature}");
                        Logger.WriteLineHelp($"Tail prefix: {nameof(jitData.TailPrefix)}");
                        Logger.WriteLineHelp($"Tail call type: {jitData.TailCallType}");
                        Logger.WriteLineHeader(LogSeparator);
                    }
                }
            };
            traceEventSession.Source.Clr.MethodTailCallFailed += jitData =>
            {
                if (ShouldPrintEventInfo(new Tuple<string,string>(jitData.CallerNamespace, jitData.CalleeNamespace)))
                {
                    if (StatsPerProcess.TryGetValue(jitData.ProcessID, out object ignored))
                    {
                        Logger.WriteLineHelp($"Caller: {jitData.CallerNamespace}.{jitData.CallerName} - {jitData.CallerNameSignature}");
                        Logger.WriteLineHelp($"Callee: {jitData.CalleeNamespace}.{jitData.CalleeName} - {jitData.CalleeNameSignature}");
                        Logger.WriteLineError($"Fail Reason: {jitData.FailReason}");
                        Logger.WriteLineHeader(LogSeparator);
                    }
                }
            };
        }

        private bool ShouldPrintEventInfo(Tuple<string, string> namespaces) => !filterByNamespace || namespaces.Item1.StartsWith(expectedNamespace) || namespaces.Item2.StartsWith(expectedNamespace);
    }
}