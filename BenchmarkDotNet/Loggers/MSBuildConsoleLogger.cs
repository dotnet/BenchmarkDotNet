using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace BenchmarkDotNet.Loggers
{
    internal class MsBuildConsoleLogger : Logger
    {
        private ILogger Logger { get; set; }

        public MsBuildConsoleLogger(ILogger logger)
        {
            Logger = logger;
        }

        public override void Initialize(IEventSource eventSource)
        {
            // By default, just show errors not warnings
            if (eventSource != null)
                eventSource.ErrorRaised += OnEventSourceErrorRaised;
        }

        private void OnEventSourceErrorRaised(object sender, BuildErrorEventArgs e) =>
            Logger.WriteLineError("// {0}({1},{2}): error {3}: {4}", e.File, e.LineNumber, e.ColumnNumber, e.Code, e.Message);
    }
}
