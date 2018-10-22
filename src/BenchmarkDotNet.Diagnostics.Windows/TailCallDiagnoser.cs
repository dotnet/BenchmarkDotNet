using System.Collections.Generic;
using BenchmarkDotNet.Running;
using Microsoft.Diagnostics.Tracing.Session;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    /// <summary>
    /// See <see href="https://blogs.msdn.microsoft.com/clrcodegeneration/2009/05/11/jit-etw-tracing-in-net-framework-4/">MSDN blog post about JIT tracing events</see>
    /// and <see href="https://georgeplotnikov.github.io/articles/tale-tail-call-dotnet">detailed blog post by George Plotnikov</see> for more info
    /// </summary>
    public class TailCallDiagnoser : JitDiagnoser
    {
        private static readonly string LogSeparator = new string('-', 20);

        private readonly bool logFailuresOnly = true;
        private readonly bool filterByNamespace = true;
        private string expectedNamespace;

        public TailCallDiagnoser() { }

        /// <summary>
        /// creates the new TailCallDiagnoser instance
        /// </summary>
        /// <param name="logFailuresOnly">only the methods that failed to get tail called. True by default.</param>
        /// <param name="filterByNamespace">only the methods from declaring type's namespace. Set to false if you want to see all Jit tail events. True by default.</param>
        public TailCallDiagnoser(bool logFailuresOnly = true, bool filterByNamespace = true)
        {
            this.logFailuresOnly = logFailuresOnly;
            this.filterByNamespace = filterByNamespace;
        }

        public override IEnumerable<string> Ids => new[] { nameof(TailCallDiagnoser) };

        protected override void AttachToEvents(TraceEventSession traceEventSession, BenchmarkCase benchmarkCase)
        {
            expectedNamespace = benchmarkCase.Descriptor.WorkloadMethod.DeclaringType.Namespace ?? benchmarkCase.Descriptor.WorkloadMethod.DeclaringType.FullName;

            Logger.WriteLine();
            Logger.WriteLineHeader(LogSeparator);
            Logger.WriteLineInfo($"{benchmarkCase.DisplayInfo}");
            Logger.WriteLineHeader(LogSeparator);

            traceEventSession.Source.Clr.MethodTailCallSucceeded += jitData =>
            {
                if (!logFailuresOnly && ShouldPrintEventInfo(jitData.CallerNamespace, jitData.CalleeNamespace))
                {
                    if (StatsPerProcess.TryGetValue(jitData.ProcessID, out object ignored))
                    {
                        Logger.WriteLineHelp($"Caller: {jitData.CallerNamespace}.{jitData.CallerName} - {jitData.CallerNameSignature}");
                        Logger.WriteLineHelp($"Callee: {jitData.CalleeNamespace}.{jitData.CalleeName} - {jitData.CalleeNameSignature}");
                        Logger.WriteLineHelp($"Tail prefix: {jitData.TailPrefix}");
                        Logger.WriteLineHelp($"Tail call type: {jitData.TailCallType}");
                        Logger.WriteLineHeader(LogSeparator);
                    }
                }
            };
            traceEventSession.Source.Clr.MethodTailCallFailed += jitData =>
            {
                if (ShouldPrintEventInfo(jitData.CallerNamespace, jitData.CalleeNamespace))
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
            traceEventSession.Source.Clr.MethodTailCallFailedAnsi += jitData => // this is new event exposed by .NET Core 2.2 https://github.com/dotnet/coreclr/commit/95a9055dbe5f6233f75ee2d7b6194e18cc4977fd
            {
                if (ShouldPrintEventInfo(jitData.CallerNamespace, jitData.CalleeNamespace))
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

        private bool ShouldPrintEventInfo(string callerNamespace, string calleeNamespace) =>
            !filterByNamespace
                    || (!string.IsNullOrEmpty(callerNamespace) && callerNamespace.StartsWith(expectedNamespace))
                    || (!string.IsNullOrEmpty(calleeNamespace) && calleeNamespace.StartsWith(expectedNamespace));
    }
}
