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
                Asm asm when asm.IntelInstruction.HasValue => IntelInstructionFormatter.Format(asm.IntelInstruction.Value, formatter, printInstructionAddresses, pointerSize),
                Asm asm when asm.Arm64Instruction is not null => Arm64InstructionFormatter.Format(asm, formatter.Options, printInstructionAddresses, pointerSize, symbols),
                Sharp sharp => sharp.Text,
                MonoCode mono => mono.Text,
                _ => throw new NotSupportedException(),
            };
    }
}
