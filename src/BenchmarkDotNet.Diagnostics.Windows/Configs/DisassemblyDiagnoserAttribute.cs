using System;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Diagnostics.Windows.Configs
{
    public class DisassemblyDiagnoserAttribute : Attribute, IConfigSource
    {
        /// <param name="printIL">IL will be printed. False by default.</param>
        /// <param name="printAsm">ASM will be printed. True by default.</param>
        /// <param name="printSource">C# source code will be printed. False by default.</param>
        /// <param name="printPrologAndEpilog">ASM for prolog and epilog will be printed. False by default.</param>
        /// <param name="printRecursive">Includes called methods, not just the benchmark. False by default.</param>
        public DisassemblyDiagnoserAttribute(bool printAsm = true, bool printIL = false, bool printSource = false, bool printPrologAndEpilog = false, bool printRecursive = false)
        {
            Config = ManualConfig.CreateEmpty().With(new DisassemblyDiagnoser(printAsm, printIL, printSource, printPrologAndEpilog, printRecursive));
        }

        public IConfig Config { get; }
    }
}