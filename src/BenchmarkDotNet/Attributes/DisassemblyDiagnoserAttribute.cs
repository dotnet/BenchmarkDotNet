using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DisassemblyDiagnoserAttribute : Attribute, IConfigSource
    {
        /// <param name="maxDepth">Includes called methods to given level. 1 by default, indexed from 1. To print just the benchmark set it to 0.</param>
        /// <param name="syntax">The disassembly syntax. MASM is the default.</param>
        /// <param name="printSource">C#|F#|VB source code will be printed. False by default.</param>
        /// <param name="printInstructionAddresses">Print instruction addresses. False by default</param>
        /// <param name="exportGithubMarkdown">Exports to GitHub markdown. True by default.</param>
        /// <param name="exportHtml">Exports to HTML with clickable links. False by default.</param>
        /// <param name="exportCombinedDisassemblyReport">Exports all benchmarks to a single HTML report. Makes it easy to compare different runtimes or methods (each becomes a column in HTML table).</param>
        /// <param name="exportDiff">Exports a diff of the assembly code to the Github markdown format. False by default.</param>
        /// <param name="filters">Glob patterns applied to full method signatures by the the disassembler.</param>
        public DisassemblyDiagnoserAttribute(
            int maxDepth = 1,
            DisassemblySyntax syntax = DisassemblySyntax.Masm,
            bool printSource = false,
            bool printInstructionAddresses = false,
            bool exportGithubMarkdown = true,
            bool exportHtml = false,
            bool exportCombinedDisassemblyReport = false,
            bool exportDiff = false,
            params string[] filters)
        {
            Config = ManualConfig.CreateEmpty().AddDiagnoser(
                new DisassemblyDiagnoser(
                    new DisassemblyDiagnoserConfig(
                        maxDepth: maxDepth,
                        syntax: syntax,
                        filters: filters,
                        printSource: printSource,
                        printInstructionAddresses: printInstructionAddresses,
                        exportGithubMarkdown: exportGithubMarkdown,
                        exportHtml: exportHtml,
                        exportCombinedDisassemblyReport: exportCombinedDisassemblyReport,
                        exportDiff: exportDiff)));
        }

        // CLS-Compliant Code requires a constructor without an array in the argument list
        protected DisassemblyDiagnoserAttribute()
        {
            Config = ManualConfig.CreateEmpty().AddDiagnoser(
                new DisassemblyDiagnoser(
                    new DisassemblyDiagnoserConfig()));
        }

        public IConfig Config { get; }
    }
}