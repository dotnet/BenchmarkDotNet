using BenchmarkDotNet.Logging;

namespace BenchmarkDotNet.Reports
{
    public static class BenchmarkReportHelper
    {
        public static void LogRunReport(IBenchmarkLogger logger, IBenchmarkRunReport runReport)
        {
            logger.WriteLineResult("Ticks: {0}, Ms: {1}", runReport.Ticks, runReport.Milliseconds);
        }

        public static void Log(IBenchmarkLogger logger, IBenchmarkRunReportsStatistic statistic, bool detailedMode)
        {
            if (detailedMode)
            {
                Log(logger, statistic.Ticks);
                Log(logger, statistic.Milliseconds);
            }
            else
            {
                logger.WriteLineStatistic(
                    "Stats: MedianTicks={0}, MedianMs={1}, Error={2:00.00}%",
                    statistic.Ticks.Median,
                    statistic.Milliseconds.Median,
                    statistic.Ticks.Error * 100);
            }
        }

        public static void Log(IBenchmarkLogger logger, IBenchmarkMeasurementStatistic statistic)
        {
            logger.WriteLineStatistic(
                "{0}: Min={1}, Max={2}, Med={3}, StdDev={4:0}, Error={5:00.00}%",
                statistic.Name,
                statistic.Min,
                statistic.Max,
                statistic.Median,
                statistic.StandardDeviation,
                statistic.Error * 100);
        }
    }
}