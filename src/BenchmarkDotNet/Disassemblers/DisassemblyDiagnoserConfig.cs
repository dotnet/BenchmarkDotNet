using BenchmarkDotNet.Disassemblers.Exporters;
using Iced.Intel;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;

namespace BenchmarkDotNet.Diagnosers
{
    public class DisassemblyDiagnoserConfig
    {
        /// <param name="maxDepth">Includes called methods to given level. 1 by default, indexed from 1. To print just the benchmark set it to 0.</param>
        /// <param name="filters">Glob patterns applied to full method signatures by the the disassembler.</param>
        /// <param name="syntax">The disassembly syntax. MASM is the default.</param>
        /// <param name="formatterOptions">Code formatter options. If not provided, the recommended settings will be used.</param>
        /// <param name="printSource">C#|F#|VB source code will be printed. False by default.</param>
        /// <param name="printInstructionAddresses">Print instruction addresses. False by default</param>
        /// <param name="exportGithubMarkdown">Exports to GitHub markdown. True by default.</param>
        /// <param name="exportHtml">Exports to HTML with clickable links. False by default.</param>
        /// <param name="exportCombinedDisassemblyReport">Exports all benchmarks to a single HTML report. Makes it easy to compare different runtimes or methods (each becomes a column in HTML table).</param>
        /// <param name="exportDiff">Exports a diff of the assembly code to the Github markdown format. False by default.</param>
        [PublicAPI]
        public DisassemblyDiagnoserConfig(
            int maxDepth = 1,
            DisassemblySyntax syntax = DisassemblySyntax.Masm,
            string[] filters = null,
            FormatterOptions formatterOptions = null,
            bool printSource = false,
            bool printInstructionAddresses = false,
            bool exportGithubMarkdown = true,
            bool exportHtml = false,
            bool exportCombinedDisassemblyReport = false,
            bool exportDiff = false)
        {
            if (!(syntax is DisassemblySyntax.Masm or DisassemblySyntax.Intel or DisassemblySyntax.Att))
            {
                throw new ArgumentOutOfRangeException(nameof(syntax), syntax, "Invalid syntax");
            }

            MaxDepth = maxDepth;
            Filters = filters ?? Array.Empty<string>();
            Syntax = syntax;
            Formatting = formatterOptions ?? GetDefaults(syntax);
            PrintSource = printSource;
            PrintInstructionAddresses = printInstructionAddresses;
            ExportGithubMarkdown = exportGithubMarkdown;
            ExportHtml = exportHtml;
            ExportCombinedDisassemblyReport = exportCombinedDisassemblyReport;
            ExportDiff = exportDiff;
        }

        public bool PrintSource { get; }
        public bool PrintInstructionAddresses { get; }
        public int MaxDepth { get; }
        public string[] Filters { get; }
        public DisassemblySyntax Syntax { get; }
        public FormatterOptions Formatting { get; }
        public bool ExportGithubMarkdown { get; }
        public bool ExportHtml { get; }
        public bool ExportCombinedDisassemblyReport { get; }
        public bool ExportDiff { get; }

        // user can specify a formatter without symbol solver
        // so we need to clone the formatter with settings and provide our symbols solver
        internal Formatter GetFormatterWithSymbolSolver(IReadOnlyDictionary<ulong, string> addressToNameMapping)
            => Syntax switch
            {
                DisassemblySyntax.Att => new GasFormatter(Formatting, new SymbolResolver(addressToNameMapping)),
                DisassemblySyntax.Intel => new IntelFormatter(Formatting, new SymbolResolver(addressToNameMapping)),
                _ => new MasmFormatter(Formatting, new SymbolResolver(addressToNameMapping)),
            };

        private static FormatterOptions GetDefaults(DisassemblySyntax syntax)
        {
            FormatterOptions options = syntax switch
            {
                DisassemblySyntax.Att => FormatterOptions.CreateGas(),
                DisassemblySyntax.Intel => FormatterOptions.CreateIntel(),
                _ => FormatterOptions.CreateMasm(),
            };

            options.FirstOperandCharIndex = 10; // pad right the mnemonic
            options.HexSuffix = default; // don't print "h" at the end of every hex address
            options.TabSize = 0; // use spaces
            return options;
        }
    }
}