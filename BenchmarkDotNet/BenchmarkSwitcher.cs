using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Plugins;
using BenchmarkDotNet.Plugins.Exporters;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet
{
    public class BenchmarkSwitcher
    {
        public Type[] Types { get; }

        public BenchmarkSwitcher(Type[] types)
        {
            Types = types;
        }

        private readonly BenchmarkConsoleLogger logger = new BenchmarkConsoleLogger();

        public void Run(string[] args)
        {
            args = ReadArgumentList(args);
            RunBenchmarks(args);
        }

        private string[] ReadArgumentList(string[] args)
        {
            while (args.Length == 0)
            {
                PrintAvailable();
                logger.WriteLineHelp("You should select the target benchmark. Please, print a number of a benchmark (e.g. #0) or a benchmark caption:");
                var line = Console.ReadLine() ?? "";
                args = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                logger.NewLine();
            }
            return args;
        }

        private void RunBenchmarks(string[] args)
        {
            for (int i = 0; i < Types.Length; i++)
            {
                var type = Types[i];
                if (args.Any(arg => type.Name.ToLower().StartsWith(arg.ToLower())) || args.Contains("#" + i) || args.Contains("" + i) || args.Contains("*"))
                {
                    logger.WriteLineHeader("Target competition: " + type.Name);
                    List<BenchmarkReport> reports;
                    using (var logStreamWriter = new StreamWriter(type.Name + ".log"))
                    {
                        var runner = new BenchmarkRunner(BencmarkPluginMode.Manual).
                            AddLoggers(new BenchmarkConsoleLogger(), new BenchmarkStreamLogger(logStreamWriter)).
                            AddExporters(BenchmarkMarkdownExporter.Default);
                        reports = runner.Run(type).ToList();
                    }
                    // TODO: use exporters
                    BenchmarkMarkdownExporter.Default.SaveToFile(reports, type.Name + "-report.md");
                    BenchmarkCsvExporter.Default.SaveToFile(reports, type.Name + "-report.csv");
                    logger.NewLine();
                }
            }
            if (args.Length > 0 && (args[0].StartsWith("http://") || args[0].StartsWith("https://")))
            {
                var url = args[0];
                Uri uri = new Uri(url);
                var name = uri.IsFile ? Path.GetFileName(uri.LocalPath) : "URL";
                using (var logStreamWriter = new StreamWriter(name + ".log"))
                {
                    var runner = new BenchmarkRunner(BencmarkPluginMode.Manual).
                        AddLoggers(new BenchmarkConsoleLogger(), new BenchmarkStreamLogger(logStreamWriter)).
                        AddExporters(BenchmarkMarkdownExporter.Default);
                    runner.RunUrl(url);
                }
            }
        }

        private void PrintAvailable()
        {
            logger.WriteLineHelp("Available Benchmark(s):");
            int numberWidth = Types.Length.ToString().Length;
            for (int i = 0; i < Types.Length; i++)
                logger.WriteLineHelp(string.Format(CultureInfo.InvariantCulture, "  #{0} {1}", i.ToString().PadRight(numberWidth), Types[i].Name));
            logger.NewLine();
            logger.NewLine();
        }
    }
}