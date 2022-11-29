using Iced.Intel;
using System;
using System.Collections.Generic;

namespace BenchmarkDotNet.Disassemblers
{
    internal static class CodeFormatter
    {
        internal static string Format(SourceCode sourceCode, Formatter formatter, bool printInstructionAddresses, uint pointerSize, IReadOnlyDictionary<ulong, string> symbols)
            => sourceCode switch
            {
                IntelAsm intel => IntelInstructionFormatter.Format(intel.Instruction, formatter, printInstructionAddresses, pointerSize),
                Arm64Asm arm64 => Arm64InstructionFormatter.Format(arm64, formatter.Options, printInstructionAddresses, pointerSize, symbols),
                Sharp sharp => sharp.Text,
                MonoCode mono => mono.Text,
                _ => throw new NotSupportedException(),
            };
    }
}
