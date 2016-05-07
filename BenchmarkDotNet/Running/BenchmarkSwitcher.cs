using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
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

        public BenchmarkSwitcher(Assembly assembly)
        {
            // Use reflection for a more maintainable way of creating the benchmark switcher,
            // Benchmarks are listed in namespace order first (e.g. BenchmarkDotNet.Samples.CPU,
            // BenchmarkDotNet.Samples.IL, etc) then by name, so the output is easy to understand.
            Types = assembly
                .GetTypes()
                .Where(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                             .Any(m => MemberInfoExtensions.GetCustomAttributes<BenchmarkAttribute>(m, true).Any()))
                .OrderBy(t => t.Namespace)
                .ThenBy(t => t.Name)
                .ToArray();
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
                logger.WriteLine();
            }
            return args;
        }

        private IEnumerable<Summary> RunBenchmarks(string[] args)
        {
            var globalChronometer = Chronometer.Start();
            var summaries = new List<Summary>();
            if (ManualConfig.ShouldDisplayOptions(args))
            {
                ManualConfig.PrintOptions(logger);
                return Enumerable.Empty<Summary>();
            }

            var config = ManualConfig.Union(DefaultConfig.Instance, ManualConfig.Parse(args));
            for (int i = 0; i < Types.Length; i++)
            {
                var type = Types[i];
                if (args.Any(arg => type.Name.ToLower().StartsWith(arg.ToLower())) || args.Contains("#" + i) || args.Contains("" + i) || args.Contains("*"))
                {
                    logger.WriteLineHeader("Target type: " + type.Name);
                    summaries.Add(BenchmarkRunner.Run(type, config));
                    logger.WriteLine();
                }
            }
            // TODO: move this logic to the RunUrl method
#if CLASSIC
            if (args.Length > 0 && (args[0].StartsWith("http://") || args[0].StartsWith("https://")))
            {
                var url = args[0];
                Uri uri = new Uri(url);
                var name = uri.IsFile ? Path.GetFileName(uri.LocalPath) : "URL";
                summaries.Add(BenchmarkRunner.RunUrl(url, config));
            }
#endif
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
            logger.WriteLine();
            logger.WriteLine();
        }
    }
}