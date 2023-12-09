using System;
using System.Diagnostics;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Diagnostics.dotTrace
{
    public class Progress : IProgress<double>
    {
        private static readonly TimeSpan ReportInterval = TimeSpan.FromSeconds(0.1);

        private readonly ILogger logger;
        private readonly string title;

        public Progress(ILogger logger, string title)
        {
            this.logger = logger;
            this.title = title;
        }

        private int lastProgress;
        private Stopwatch? stopwatch;

        public void Report(double value)
        {
            int progress = (int)Math.Floor(value);
            bool needToReport = stopwatch == null ||
                                (stopwatch != null && stopwatch?.Elapsed > ReportInterval) ||
                                progress == 100;

            if (lastProgress != progress && needToReport)
            {
                logger.WriteLineInfo($"{title}: {progress}%");
                lastProgress = progress;
                stopwatch = Stopwatch.StartNew();
            }
        }
    }
}