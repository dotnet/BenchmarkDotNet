using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Running
{
    public class BenchmarkSwitcher
    {
        public Type[] Types { get; }

        public BenchmarkSwitcher(Type[] types)
        {
            Types = types;
        }

        private readonly ConsoleLogger logger = new ConsoleLogger();

        public void Run(string[] args = null)
        {
            args = ReadArgumentList(args ?? new string[0]);
            RunBenchmarks(args);
        }

        private string[] ReadArgumentList(string[] args)
        {
            while (args.Length == 0)
            {
                PrintAvailable();
                var benchmarkCaptionExample = Types.Length == 0 ? "Intro_00" : Types.First().Name;
                logger.WriteLineHelp($"You should select the target benchmark. Please, print a number of a benchmark (e.g. '0') or a benchmark caption (e.g. '{benchmarkCaptionExample}'):");
                var line = Console.ReadLine() ?? "";
                args = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                logger.NewLine();
            }
            return args;
        }

        private IEnumerable<Summary> RunBenchmarks(string[] args)
        {
            var globalChronometer = Chronometer.Start();
            var summaries = new List<Summary>();
            var config = ManualConfig.Parse(args);
            for (int i = 0; i < Types.Length; i++)
            {
                var type = Types[i];
                if (args.Any(arg => type.Name.ToLower().StartsWith(arg.ToLower())) || args.Contains("#" + i) || args.Contains("" + i) || args.Contains("*"))
                {
                    logger.WriteLineHeader("Target type: " + type.Name);
                    summaries.Add(BenchmarkRunner.Run(type, config));
                    logger.NewLine();
                }
            }
            // TODO: move this logic to the RunUrl method
            if (args.Length > 0 && (args[0].StartsWith("http://") || args[0].StartsWith("https://")))
            {
                var url = args[0];
                Uri uri = new Uri(url);
                var name = uri.IsFile ? Path.GetFileName(uri.LocalPath) : "URL";
                summaries.Add(BenchmarkRunner.RunUrl(url, config));
            }
            var clockSpan = globalChronometer.Stop();
            BenchmarkRunner.LogTotalTime(logger, clockSpan.GetTimeSpan(), "Global total time");
            return summaries;
        }

        private void PrintAvailable()
        {
            if (Types.IsEmpty())
            {
                logger.WriteLineError("You don't have any benchmarks");
                return;
            }
            logger.WriteLineHelp($"Available Benchmark{(Types.Length > 1 ? "s" : "")}:");
            int numberWidth = Types.Length.ToString().Length;
            for (int i = 0; i < Types.Length; i++)
                logger.WriteLineHelp(string.Format(CultureInfo.InvariantCulture, "  #{0} {1}", i.ToString().PadRight(numberWidth), Types[i].Name));
            logger.NewLine();
            logger.NewLine();
        }
    }
}