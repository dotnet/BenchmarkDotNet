#if !DNX451
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace BenchmarkDotNet.Plugins.Loggers
{
    internal class MSBuildConsoleLogger : Logger
    {
        private IBenchmarkLogger Logger { get; set; }

        public MSBuildConsoleLogger(IBenchmarkLogger logger)
        {
            Logger = logger;
        }

        public override void Initialize(IEventSource eventSource)
        {
            // By default, just show errors not warnings
            eventSource.ErrorRaised +=
                (sender, e) => Logger.WriteLineError("// {0}({1},{2}): error {3}: {4}", e.File, e.LineNumber, e.ColumnNumber, e.Code, e.Message);
        }
    }
}
#endif