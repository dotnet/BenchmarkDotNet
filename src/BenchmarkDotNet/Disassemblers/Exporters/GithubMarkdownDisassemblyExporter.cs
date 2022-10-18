using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Disassemblers.Exporters
{
    internal class GithubMarkdownDisassemblyExporter : ExporterBase
    {
        private readonly IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> results;
        private readonly DisassemblyDiagnoserConfig config;

        internal GithubMarkdownDisassemblyExporter(IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> results, DisassemblyDiagnoserConfig config)
        {
            this.results = results;
            this.config = config;
        }

        protected override string FileExtension => "md";
        protected override string FileCaption => "asm";

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            foreach (var benchmarkCase in summary.BenchmarksCases.Where(results.ContainsKey))
            {
                logger.WriteLine($"## {summary[benchmarkCase].GetRuntimeInfo()}");

                Export(logger, results[benchmarkCase], config);
            }
        }

        internal static void Export(ILogger logger, DisassemblyResult disassemblyResult, DisassemblyDiagnoserConfig config, bool quotingCode = true)
        {
            int methodIndex = 0;
            foreach (var method in disassemblyResult.Methods.Where(method => string.IsNullOrEmpty(method.Problem)))
            {
                if (quotingCode)
                {
                    logger.WriteLine("```assembly");
                }

                logger.WriteLine($"; {method.Name}");

                var pretty = DisassemblyPrettifier.Prettify(method, disassemblyResult, config, $"M{methodIndex++:00}");

                ulong totalSizeInBytes = 0;
                foreach (var element in pretty)
                {
                    if (element is DisassemblyPrettifier.Label label)
                    {
                        logger.WriteLine($"{label.TextRepresentation}:");
                    }
                    else if (element.Source is Sharp sharp)
                    {
                        logger.WriteLine($"; {sharp.Text.Replace("\n", "\n; ")}"); // they are multiline and we need to add ; for each line
                    }
                    else if (element.Source is Asm asm)
                    {
                        checked
                        {
                            totalSizeInBytes += (uint)asm.InstructionLength;
                        }

                        logger.WriteLine($"       {element.TextRepresentation}");
                    }
                    else if (element.Source is MonoCode mono)
                    {
                        logger.WriteLine(mono.Text);
                    }
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