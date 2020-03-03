using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DisassemblyDiagnoserAttribute : Attribute, IConfigSource
    {
        /// <param name="maxDepth">Includes called methods to given level. 1 by default, indexed from 1. To print just the benchmark set it to 0.</param>
        /// <param name="printSource">C#|F#|VB source code will be printed. False by default.</param>
        /// <param name="printInstructionAddresses">Print instruction addresses. False by default</param>
        /// <param name="exportGithubMarkdown">Exports to GitHub markdown. True by default.</param>
        /// <param name="exportHtml">Exports to HTML with clickable links. False by default.</param>
        /// <param name="exportCombinedDisassemblyReport">Exports all benchmarks to a single HTML report. Makes it easy to compare different runtimes or methods (each becomes a column in HTML table).</param>
        /// <param name="exportDiff">Exports a diff. False by default.</param>
        public DisassemblyDiagnoserAttribute(
            int maxDepth = 1,
            bool printSource = false,
            bool printInstructionAddresses = false,
            bool exportGithubMarkdown = true,
            bool exportHtml = false,
            bool exportCombinedDisassemblyReport = false,
            bool exportDiff = false)
        {
            Config = ManualConfig.CreateEmpty().AddDiagnoser(
                new DisassemblyDiagnoser(
                    new DisassemblyDiagnoserConfig(
                        maxDepth: maxDepth,
                        printSource: printSource,
                        printInstructionAddresses: printInstructionAddresses,
                        exportGithubMarkdown: exportGithubMarkdown,
                        exportHtml: exportHtml,
                        exportCombinedDisassemblyReport: exportCombinedDisassemblyReport,
                        exportDiff: exportDiff)));
        }

        public IConfig Config { get; }
    }
}