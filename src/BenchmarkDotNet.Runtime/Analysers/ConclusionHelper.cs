using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Analysers
{
    public static class ConclusionHelper
    {
        public static void Print(ILogger logger, List<Conclusion> conclusions)
        {
            PrintFiltered(conclusions, ConclusionKind.Warning, "Warnings", logger.WriteLineError);
            PrintFiltered(conclusions, ConclusionKind.Hint, "Hints", logger.WriteLineHint);
        }

        private static void PrintFiltered(List<Conclusion> conclusions, ConclusionKind kind, string title, Action<string> printLine)
        {
            var filtered = conclusions.Where(c => c.Kind == kind).ToArray();
            if (filtered.Any())
            {
                printLine("");
                printLine($"// * {title} *");
                foreach (var group in filtered.GroupBy(c => c.AnalyserId))
                {
                    printLine($"{group.Key}");
                    var values = group.ToList();
                    int maxTitleWidth = values.Max(c => GetTitle(c).Length);
                    foreach (var conclusion in values)
                        printLine("  " + GetTitle(conclusion).PadRight(maxTitleWidth, ' ') + " -> " + conclusion.Message);
                }
            }
        }

        private static string GetTitle(Conclusion conclusion)
        {
            if (conclusion.Report == null)
                return "Summary";
            var b = conclusion.Report?.Benchmark;
            return b != null ? $"{b.Target.DisplayInfo}: {b.Job.Id}" : "[Summary]";
        }
    }
}