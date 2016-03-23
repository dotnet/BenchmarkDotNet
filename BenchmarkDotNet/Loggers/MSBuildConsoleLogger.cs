#if CLASSIC
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
            Logger.WriteLineError($"// {e.File}({e.LineNumber},{e.ColumnNumber}): error {e.Code}: {e.Message}");
    }
}
#endif