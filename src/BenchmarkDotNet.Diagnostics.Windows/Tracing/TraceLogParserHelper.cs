using System;
using System.Collections.Generic;
using BenchmarkDotNet.Reports;
using Microsoft.Diagnostics.Tracing.Etlx;

namespace BenchmarkDotNet.Diagnostics.Windows.Tracing {
    public class TraceLogParserHelper
    {
        public static IEnumerable<Metric> Parse(string etlFilePath, Func<TraceLogEventSource, IEnumerable<Metric>> traceLogEventSourceParserAction)
        {
            using (var traceLog = new TraceLog(TraceLog.CreateFromEventTraceLogFile(etlFilePath)))
            {
                return traceLogEventSourceParserAction(traceLog.Events.GetSource());
            }
        }
    }
}