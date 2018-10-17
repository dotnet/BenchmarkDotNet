using JetBrains.Annotations;

namespace BenchmarkDotNet.Diagnosers
{
    public class DisassemblyDiagnoserConfig
    {
        [PublicAPI] public static readonly DisassemblyDiagnoserConfig Asm = new DisassemblyDiagnoserConfig(printAsm: true);
        [PublicAPI] public static readonly DisassemblyDiagnoserConfig AsmFullRecursive = new DisassemblyDiagnoserConfig(printAsm: true, printPrologAndEpilog: true, recursiveDepth: int.MaxValue);
        [PublicAPI] public static readonly DisassemblyDiagnoserConfig IL = new DisassemblyDiagnoserConfig(printAsm: false, printIL: true);
        [PublicAPI] public static readonly DisassemblyDiagnoserConfig AsmAndIL = new DisassemblyDiagnoserConfig(printAsm: true, printIL: true);
        [PublicAPI] public static readonly DisassemblyDiagnoserConfig All = new DisassemblyDiagnoserConfig(true, true, true, true, int.MaxValue);

        /// <param name="printIL">IL will be printed. False by default.</param>
        /// <param name="printAsm">ASM will be printed. True by default.</param>
        /// <param name="printSource">C# source code will be printed. False by default.</param>
        /// <param name="printPrologAndEpilog">ASM for prolog and epilog will be printed. False by default.</param>
        /// <param name="recursiveDepth">Includes called methods to given level. 1 by default, indexed from 1. To print just benchmark set to 0</param>
        [PublicAPI]
        public DisassemblyDiagnoserConfig(bool printAsm = true, bool printIL = false, bool printSource = false, bool printPrologAndEpilog = false,
            int recursiveDepth = 1)
        {
            PrintAsm = printAsm;
            PrintIL = printIL;
            PrintSource = printSource;
            PrintPrologAndEpilog = printPrologAndEpilog;
            RecursiveDepth = recursiveDepth;
        }

        public bool PrintAsm { get; }
        public bool PrintIL { get; }
        public bool PrintSource { get; }
        public bool PrintPrologAndEpilog { get; }
        public int RecursiveDepth { get; }
    }
}