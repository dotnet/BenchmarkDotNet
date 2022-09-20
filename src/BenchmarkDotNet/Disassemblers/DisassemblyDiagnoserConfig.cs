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
        /// <param name="formatter">Assembly code formatter. If not provided, MasmFormatter with the recommended settings will be used.</param>
        /// <param name="printSource">C#|F#|VB source code will be printed. False by default.</param>
        /// <param name="printInstructionAddresses">Print instruction addresses. False by default</param>
        /// <param name="exportGithubMarkdown">Exports to GitHub markdown. True by default.</param>
        /// <param name="exportHtml">Exports to HTML with clickable links. False by default.</param>
        /// <param name="exportCombinedDisassemblyReport">Exports all benchmarks to a single HTML report. Makes it easy to compare different runtimes or methods (each becomes a column in HTML table).</param>
        /// <param name="exportDiff">Exports a diff of the assembly code to the Github markdown format. False by default.</param>
        [PublicAPI]
        public DisassemblyDiagnoserConfig(
            int maxDepth = 1,
            string[] filters = null,
            Formatter formatter = null,
            bool printSource = false,
            bool printInstructionAddresses = false,
            bool exportGithubMarkdown = true,
            bool exportHtml = false,
            bool exportCombinedDisassemblyReport = false,
            bool exportDiff = false)
        {
            MaxDepth = maxDepth;
            Filters = filters ?? Array.Empty<string>();
            Formatter = formatter ?? CreateDefaultFormatter();
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
        public bool ExportGithubMarkdown { get; }
        public bool ExportHtml { get; }
        public bool ExportCombinedDisassemblyReport { get; }
        public bool ExportDiff { get; }

        // it's private to make sure that GetFormatterWithSymbolSolver is always used
        private Formatter Formatter { get; }

        private static Formatter CreateDefaultFormatter()
        {
            var formatter = new MasmFormatter();
            formatter.Options.FirstOperandCharIndex = 10; // pad right the mnemonic
            formatter.Options.HexSuffix = default; // don't print "h" at the end of every hex address
            formatter.Options.TabSize = 0; // use spaces
            return formatter;
        }

        // user can specify a formatter without symbol solver
        // so we need to clone the formatter with settings and provide our symbols solver
        internal Formatter GetFormatterWithSymbolSolver(IReadOnlyDictionary<ulong, string> addressToNameMapping)
        {
            var symbolSolver = new SymbolResolver(addressToNameMapping);

            switch (Formatter)
            {
                case MasmFormatter masmFormatter:
                    return new MasmFormatter(masmFormatter.Options, symbolSolver);
                case NasmFormatter nasmFormatter:
                    return new NasmFormatter(nasmFormatter.Options, symbolSolver);
                case GasFormatter gasFormatter:
                    return new GasFormatter(gasFormatter.Options, symbolSolver);
                case IntelFormatter intelFormatter:
                    return new IntelFormatter(intelFormatter.Options, symbolSolver);
                default:
                    // we don't know how to translate it so we just return the original one
                    // it's better not to solve symbols rather than throw exception ;)
                    return Formatter;
            }
        }
    }
}