using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Configs
{
    // TODO: refactoring
    public class ConfigParser
    {
        public IConfig Parse(string[] args)
        {
            var config = new ManualConfig();
            foreach (var arg in args.Where(arg => arg.Contains("=")))
            {
                var split = arg.ToLowerInvariant().Split('=');
                var values = split[1].Split(',');
                switch (split[0])
                {
                    case "jobs":
                        foreach (var value in values)
                            config.Add(ParseJobs(value));
                        break;
                    case "columns":
                        foreach (var value in values)
                            config.Add(ParseColumns(value));
                        break;
                    case "exporters":
                        break;
                    case "diagnosers":
                        break;
                    case "analysers":
                        break;
                    case "loggers":
                        break;
                }
            }
            return config;
        }

        private IColumn[] ParseColumns(string value)
        {
            switch (value)
            {
                case "mean":
                    return new[] { StatisticColumn.Mean };
                case "stderror":
                    return new[] { StatisticColumn.StdError };
                case "stddev":
                    return new[] { StatisticColumn.StdDev };
                case "operationpersecond":
                    return new[] { StatisticColumn.OperationsPerSecond };
                case "min":
                    return new[] { StatisticColumn.Min };
                case "q1":
                    return new[] { StatisticColumn.Q1 };
                case "median":
                    return new[] { StatisticColumn.Median };
                case "q3":
                    return new[] { StatisticColumn.Q3 };
                case "max":
                    return new[] { StatisticColumn.Max };
                case "allstatistics":
                    return StatisticColumn.AllStatistics;
                case "place":
                    return new[] { PlaceColumn.ArabicNumber };

            }
            return new IColumn[0];
        }

        private IJob[] ParseJobs(string value)
        {
            switch (value)
            {
                case "default":
                    return new[] { Job.Default };
                case "legacyx86":
                    return new[] { Job.LegacyX86 };
                case "legacyx64":
                    return new[] { Job.LegacyX64 };
                case "ryujitx64":
                    return new[] { Job.RyuJitX64 };
                case "dry":
                    return new[] { Job.Dry };
                case "alljits":
                    return Job.AllJits;
                case "clr":
                    return new[] { Job.Clr };
                case "mono":
                    return new[] { Job.Mono };
            }
            return new IJob[0];
        }
    }
}