using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet.Attributes
{
    public class DisassemblyDiagnoserAttribute : Attribute, IConfigSource
    {
        /// <param name="printIL">IL will be printed. False by default.</param>
        /// <param name="printAsm">ASM will be printed. True by default.</param>
        /// <param name="printSource">C# source code will be printed. False by default.</param>
        /// <param name="printPrologAndEpilog">ASM for prolog and epilog will be printed. False by default.</param>
        /// <param name="recursiveDepth">Includes called methods to given level. 1 by default, indexed from 1. To print just benchmark set to 0</param>
        public DisassemblyDiagnoserAttribute(bool printAsm = true, bool printIL = false, bool printSource = false, bool printPrologAndEpilog = false, int recursiveDepth = 1)
        {
            Config = ManualConfig.CreateEmpty().With(
                DisassemblyDiagnoser.Create(
                    new DisassemblyDiagnoserConfig(printAsm, printIL, printSource, printPrologAndEpilog, recursiveDepth)));
        }

        public IConfig Config { get; }
    }
}