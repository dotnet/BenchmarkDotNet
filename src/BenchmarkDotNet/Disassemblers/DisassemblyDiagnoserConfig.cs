using JetBrains.Annotations;

namespace BenchmarkDotNet.Diagnosers
{
    public class DisassemblyDiagnoserConfig
    {
        [PublicAPI]
        public static readonly DisassemblyDiagnoserConfig Asm = new DisassemblyDiagnoserConfig(printAsm: true);

        [PublicAPI]
        public static readonly DisassemblyDiagnoserConfig AsmFullRecursive = new DisassemblyDiagnoserConfig(printAsm: true, recursiveDepth: int.MaxValue);

        [PublicAPI]
        public static readonly DisassemblyDiagnoserConfig All = new DisassemblyDiagnoserConfig(true, true, int.MaxValue, true);

        /// <param name="printAsm">ASM will be printed. True by default.</param>
        /// <param name="printSource">C# source code will be printed. False by default.</param>
        /// <param name="recursiveDepth">Includes called methods to given level. 1 by default, indexed from 1. To print just benchmark set to 0</param>
        /// <param name="printDiff">Diff will be printed. False by default.</param>
        [PublicAPI]
        public DisassemblyDiagnoserConfig(bool printAsm = true, bool printSource = false, int recursiveDepth = 1, bool printDiff = false)
        {
            PrintAsm = printAsm;
            PrintSource = printSource;
            RecursiveDepth = recursiveDepth;
            PrintDiff = printDiff;
        }

        public bool PrintAsm { get; }
        public bool PrintSource { get; }
        public int RecursiveDepth { get; }
        public bool PrintDiff { get; }
    }
}