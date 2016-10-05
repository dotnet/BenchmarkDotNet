using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using Microsoft.Diagnostics.Tracing.Session;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public class InliningDiagnoser : JitDiagnoser
    {
        protected override void AttachToEvents(TraceEventSession session, Benchmark benchmark)
        {
            var expected = benchmark.Target.Method.DeclaringType.Namespace + "." +
                           benchmark.Target.Method.DeclaringType.Name;

            Logger.WriteLine();
            Logger.WriteLineHeader(new string('-', 20));
            Logger.WriteLineInfo($"{benchmark.DisplayInfo}");
            Logger.WriteLineHeader(new string('-', 20));

            session.Source.Clr.MethodInliningSucceeded += jitData =>
            {
                // Inliner = the parent method (the inliner calls the inlinee)
                // Inlinee = the method that is going to be "inlined" inside the inliner (it's caller)                
                object _ignored;
                if (StatsPerProcess.TryGetValue(jitData.ProcessID, out _ignored))
                {
                    var shouldPrint = jitData.InlinerNamespace == expected ||
                                      jitData.InlineeNamespace == expected;
                    if (shouldPrint)
                    {
                        Logger.WriteLineHelp($"Inliner: {jitData.InlinerNamespace}.{jitData.InlinerName} - {jitData.InlinerNameSignature}");
                        Logger.WriteLineHelp($"Inlinee: {jitData.InlineeNamespace}.{jitData.InlineeName} - {jitData.InlineeNameSignature}");
                        Logger.WriteLineHeader(new string('-', 20));
                    }
                }
            };

            session.Source.Clr.MethodInliningFailed += jitData =>
            {
                object _ignored;
                if (StatsPerProcess.TryGetValue(jitData.ProcessID, out _ignored))
                {
                    var shouldPrint = jitData.InlinerNamespace == expected ||
                                      jitData.InlineeNamespace == expected;
                    if (shouldPrint)
                    {
                        Logger.WriteLineError($"Inliner: {jitData.InlinerNamespace}.{jitData.InlinerName} - {jitData.InlinerNameSignature}");
                        Logger.WriteLineError($"Inlinee: {jitData.InlineeNamespace}.{jitData.InlineeName} - {jitData.InlineeNameSignature}");
                        // See https://blogs.msdn.microsoft.com/clrcodegeneration/2009/10/21/jit-etw-inlining-event-fail-reasons/
                        Logger.WriteLineError($"Fail Reason: {jitData.FailReason}");
                        Logger.WriteLineHeader(new string('-', 20));
                    }
                }
            };
        }
    }
}
