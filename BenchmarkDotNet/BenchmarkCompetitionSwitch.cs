using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Export;
using BenchmarkDotNet.Logging;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet
{
    public class BenchmarkCompetitionSwitch
    {
        public Type[] Competitions { get; }

        public BenchmarkCompetitionSwitch(Type[] competitions)
        {
            Competitions = competitions;
        }

        private readonly BenchmarkConsoleLogger logger = new BenchmarkConsoleLogger();

        public void Run(string[] args)
        {
            args = ReadArgumentList(args);
            RunCompetitions(args);
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

        private void RunCompetitions(string[] args)
        {
            for (int i = 0; i < Competitions.Length; i++)
            {
                var competition = Competitions[i];
                if (args.Any(arg => competition.Name.ToLower().StartsWith(arg.ToLower())) || args.Contains("#" + i) || args.Contains("" + i) || args.Contains("*"))
                {
                    logger.WriteLineHeader("Target competition: " + competition.Name);
                    List<BenchmarkReport> reports;
                    using (var logStreamWriter = new StreamWriter(competition.Name + ".log"))
                    {
                        var loggers = new IBenchmarkLogger[] { new BenchmarkConsoleLogger(), new BenchmarkStreamLogger(logStreamWriter) };
                        var runner = new BenchmarkRunner(loggers);
                        reports = runner.RunCompetition(Activator.CreateInstance(competition), BenchmarkSettings.Parse(args)).ToList();
                    }
                    MarkdownReportExporter.Default.SaveToFile(reports, competition.Name + "-report.md");
                    CsvReportExporter.Default.SaveToFile(reports, competition.Name + "-report.csv");
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
                    var loggers = new IBenchmarkLogger[] { new BenchmarkConsoleLogger(), new BenchmarkStreamLogger(logStreamWriter) };
                    var runner = new BenchmarkRunner(loggers);
                    runner.RunUrl(url, BenchmarkSettings.Parse(args));
                }
            }
        }

        private void PrintAvailable()
        {
            logger.WriteLineHelp("Available competitions:");
            int numberWidth = Competitions.Length.ToString().Length;
            for (int i = 0; i < Competitions.Length; i++)
                logger.WriteLineHelp(string.Format(CultureInfo.InvariantCulture, "  #{0} {1}", i.ToString().PadRight(numberWidth), Competitions[i].Name));
            logger.NewLine();
            logger.NewLine();
        }
    }
}