using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using StreamWriter = BenchmarkDotNet.Portability.StreamWriter;

namespace BenchmarkDotNet.Exporters
{
    public class PrettyGithubMarkdownDisassemblyExporter : IExporter
    {
        private readonly IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> results;

        public PrettyGithubMarkdownDisassemblyExporter(IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> results) => this.results = results;

        public string Name => nameof(PrettyGithubMarkdownDisassemblyExporter);

        public void ExportToLog(Summary summary, ILogger logger) { }

        public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger)
            => summary.BenchmarksCases
                      .Where(results.ContainsKey)
                      .Select(benchmark => Export(summary, benchmark));

        private string Export(Summary summary, BenchmarkCase benchmarkCase)
        {
            string filePath = $"{Path.Combine(summary.ResultsDirectoryPath, benchmarkCase.FolderInfo)}-asm.pretty.md";
            if (File.Exists(filePath))
                File.Delete(filePath);

            using (var stream = StreamWriter.FromPath(filePath))
            {
                Export(new StreamLogger(stream), results[benchmarkCase], benchmarkCase);
            }

            return filePath;
        }

        private static void Export(ILogger logger, DisassemblyResult disassemblyResult, BenchmarkCase benchmarkCase)
        {
            int methodIndex = 0;
            foreach (var method in disassemblyResult.Methods.Where(method => string.IsNullOrEmpty(method.Problem)))
            {
                logger.WriteLine("```assembly");
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
                logger.WriteLine("```");
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
        }
    }
}