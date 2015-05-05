using System;
using System.Globalization;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Logging;
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
                logger.WriteLineHelp("Argument list is empty. Please, print the argument list:");
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
                if (args.Any(arg => competition.Name.ToLower().StartsWith(arg.ToLower())) || args.Contains("#" + i) || args.Contains("*"))
                {
                    logger.WriteLineHeader("Target competition: " + competition.Name);
                    using (var logStreamWriter = new StreamWriter(competition.Name + ".log"))
                    {
                        var loggers = new IBenchmarkLogger[] {new BenchmarkConsoleLogger(), new BenchmarkStreamLogger(logStreamWriter)};
                        var runner = new BenchmarkRunner(loggers);
                        runner.RunCompetition(Activator.CreateInstance(competition), BenchmarkSettings.Parse(args));
                    }
                    logger.NewLine();
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