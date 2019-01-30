using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Exporters
{
    public class PrettyGithubMarkdownDisassemblyExporter : ExporterBase
    {
        private readonly IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> results;

        public PrettyGithubMarkdownDisassemblyExporter(IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> results)
        {
            this.results = results;
        }

        protected override string FileExtension => "md";
        protected override string FileCaption => "asm.pretty";

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            foreach (var benchmarkCase in summary.BenchmarksCases.Where(results.ContainsKey))
            {
                logger.WriteLine($"## {summary[benchmarkCase].GetRuntimeInfo()}");
                Export(logger, results[benchmarkCase]);
            }
        }

        internal static void Export(ILogger logger, DisassemblyResult disassemblyResult, bool quotingCode = true)
        {
            int methodIndex = 0;
            foreach (var method in disassemblyResult.Methods.Where(method => string.IsNullOrEmpty(method.Problem)))
            {
                if (quotingCode)
                {
                    logger.WriteLine("```assembly");
                }

                logger.WriteLine($"; {method.Name}");

                var pretty = DisassemblyPrettifier.Prettify(method, $"M{methodIndex++:00}");

                uint totalSizeInBytes = 0;
                foreach (var element in pretty)
                {
                    if (element is DisassemblyPrettifier.Label label)
                    {
                        logger.WriteLine($"{label.TextRepresentation}:");

                        continue;
                    }
                    if (element.Source is Asm asm)
                    {
                        totalSizeInBytes += asm.SizeInBytes;
                    }

                    logger.WriteLine($"       {element.TextRepresentation}");
                }

                logger.WriteLine($"; Total bytes of code {totalSizeInBytes}");
                if (quotingCode)
                {
                    logger.WriteLine("```");
                }
            }

            foreach (var withProblems in disassemblyResult.Methods
                .Where(method => !string.IsNullOrEmpty(method.Problem))
                .GroupBy(method => method.Problem))
            {
                logger.WriteLine($"**{withProblems.Key}**");
                foreach (var withProblem in withProblems)
                {
                    logger.WriteLine(withProblem.Name);
                }
            }

            logger.WriteLine();
        }
    }
}