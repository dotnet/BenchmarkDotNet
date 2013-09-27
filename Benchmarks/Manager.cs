using System.IO;
using BenchmarkDotNet;

namespace Benchmarks
{
    public class Manager
    {
        public string OutputFileName { get; set; }

        public void ProcessCompetition(BenchmarkCompetition competition)
        {
            if (!string.IsNullOrWhiteSpace(OutputFileName))
            {
                using (var writer = new StreamWriter(OutputFileName))
                {
                    ConsoleHelper.SetOut(writer);
                    competition.PrintResults();
                    ConsoleHelper.RestoreDefaultOut();
                }
            }
        }
    }
}