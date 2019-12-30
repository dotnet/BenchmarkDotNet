using Iced.Intel;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Diagnosers
{
    public class DisassemblyDiagnoserConfig
    {
        /// <param name="maxDepth">Includes called methods to given level. 1 by default, indexed from 1. To print just the benchmark set it to 0.</param>
        /// <param name="formatter">Assembly code formatter. If not provided, MasmFormatter with the recommended settings will be used.</param>
        /// <param name="printSource">C#|F#|VB source code will be printed. False by default.</param>
        /// <param name="printInstructionAddresses">Print instruction addresses. False by default</param>
        /// <param name="exportGithubMarkdown">Exports to GitHub markdown. True by default.</param>
        /// <param name="exportHtml">Exports to HTML with clickable links. False by default.</param>
        /// <param name="exportCombinedDisassemblyReport">Exports all benchmarks to a single HTML report. Makes it easy to compare different runtimes or methods (each becomes a column in HTML table).</param>
        /// <param name="exportDiff">Exports a diff. False by default.</param>
        [PublicAPI]
        public DisassemblyDiagnoserConfig(
            int maxDepth = 1,
            Formatter formatter = null,
            bool printSource = false,
            bool printInstructionAddresses = false,
            bool exportGithubMarkdown = true,
            bool exportHtml = false,
            bool exportCombinedDisassemblyReport = false,
            bool exportDiff = false)
        {
            MaxDepth = maxDepth;
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
        public Formatter Formatter { get; }
        public bool ExportGithubMarkdown { get; }
        public bool ExportHtml { get; }
        public bool ExportCombinedDisassemblyReport { get; }
        public bool ExportDiff { get; }

        private static Formatter CreateDefaultFormatter()
        {
            var formatter = new MasmFormatter();
            formatter.Options.FirstOperandCharIndex = 10; // pad right the mnemonic
            formatter.Options.HexSuffix = default; // don't print "h" at the end of every hex address
            formatter.Options.TabSize = 0; // use spaces
            return formatter;
        }
    }
}