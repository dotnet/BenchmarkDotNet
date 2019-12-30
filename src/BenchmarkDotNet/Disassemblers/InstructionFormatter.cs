using BenchmarkDotNet.Diagnosers;
using Iced.Intel;
using System;
using System.Collections.Generic;
using System.Text;

namespace BenchmarkDotNet.Disassemblers
{
    internal static class CodeFormatter
    {
        internal static string Format(SourceCode sourceCode, DisassemblyDiagnoserConfig config, uint pointerSize, IReadOnlyDictionary<ulong, string> addressesMapping)
        {
            switch(sourceCode)
            {
                case Asm asm:
                    return InstructionFormatter.Format(asm.Instruction, config, pointerSize, addressesMapping);
                case Sharp sharp:
                    return sharp.Text;
                default:
                    throw new NotSupportedException();
            }
        }
    }

    internal static class InstructionFormatter
    {
        internal static string Format(Instruction instruction, DisassemblyDiagnoserConfig config, uint pointerSize, IReadOnlyDictionary<ulong, string> addressesMappings)
        {
            var internalBuffer = new StringBuilder(); // we need it to get the length (not exposed by StringBuilderFormatterOutput directly)
            var output = new StringBuilderFormatterOutput(internalBuffer);

            if (config.PrintInstructionAddresses)
            {
                FormatInstructionPointer(instruction, config.Formatter, pointerSize, output);
            }

            if (ClrMdDisassembler.TryGetReferencedAddress(instruction, pointerSize, out ulong referencedAddress) && addressesMappings.ContainsKey(referencedAddress))
            {
                // this instruction refers to an address that we know how to transalte
                // so we format the mnemonic and operands on our own, replacing the hex address with a user friendly string
                FormatMnemonic(instruction, config.Formatter, internalBuffer, output);

                FormatOperands(instruction, config.Formatter, pointerSize, addressesMappings, output);
            }
            else
            {
                config.Formatter.Format(instruction, output);
            }

            return output.ToString();
        }

        private static void FormatInstructionPointer(Instruction instruction, Formatter formatter, uint pointerSize, StringBuilderFormatterOutput output)
        {
            string ipFormat = formatter.Options.LeadingZeroes
                ? pointerSize == 4 ? "X8" : "X16"
                : "X";

            output.Write(instruction.IP.ToString(ipFormat), FormatterOutputTextKind.Text);
            output.Write(" ", FormatterOutputTextKind.Text);
        }

        private static void FormatMnemonic(Instruction instruction, Formatter formatter, StringBuilder internallBuffer, StringBuilderFormatterOutput output)
        {
            var lengthBefore = internallBuffer.Length;
            formatter.FormatMnemonic(instruction, output);
            var lengthAfter = internallBuffer.Length;

            for (int i = lengthAfter; i < lengthBefore + formatter.Options.FirstOperandCharIndex; i++)
            {
                output.Write(" ", FormatterOutputTextKind.Text);
            }
        }

        private static void FormatOperands(Instruction instruction, Formatter formatter, uint pointerSize, IReadOnlyDictionary<ulong, string> addressesMapping, StringBuilderFormatterOutput output)
        {
            for (int instructionOperand = 0; instructionOperand < instruction.OpCount; instructionOperand++)
            {
                // GetFormatterOperand returns -1 if the instruction operand isn't used by the formatter
                if (formatter.GetFormatterOperand(instruction, instructionOperand) == -1)
                    continue;

                if (TryGetAddress(instruction, instructionOperand, pointerSize, out ulong address) && addressesMapping.TryGetValue(address, out string mapping))
                {
                    // we use the user friendly mapping (method|label name) instead of the hex address
                    output.Write(mapping, FormatterOutputTextKind.Text);
                }
                else
                {
                    formatter.FormatOperand(instruction, output, instructionOperand);
                }

                output.Write(" ", FormatterOutputTextKind.Text); // todo: ","
            }

            // A formatter can add and remove operands, here we handle the case where it adds some
            for (int formatterInstructionOperand = instruction.OpCount; formatterInstructionOperand < formatter.GetOperandCount(instruction); formatterInstructionOperand++)
            {
                formatter.FormatOperand(instruction, output, formatterInstructionOperand);
                output.Write(" ", FormatterOutputTextKind.Text);
            }
        }

        private static bool TryGetAddress(in Instruction instruction, int instructionOperand, uint pointerSize, out ulong address)
        {
            switch (instruction.GetOpKind(instructionOperand))
            {
                case OpKind.NearBranch16:
                case OpKind.NearBranch32:
                case OpKind.NearBranch64:
                    address = instruction.NearBranchTarget;
                    return true;
                case OpKind.Immediate16:
                case OpKind.Immediate8to16:
                case OpKind.Immediate8to32:
                case OpKind.Immediate8to64:
                case OpKind.Immediate32to64:
                case OpKind.Immediate32 when pointerSize == 4:
                case OpKind.Immediate64:
                    address = instruction.GetImmediate(instructionOperand);
                    return true;
                case OpKind.Memory64:
                    address = instruction.MemoryAddress64;
                    return true;
                case OpKind.Memory when instruction.IsIPRelativeMemoryOperand:
                    address = instruction.IPRelativeMemoryAddress;
                    return true;
                case OpKind.Memory:
                    address = instruction.MemoryDisplacement;
                    return true;
                default:
                    address = default;
                    return false;
            }
        }
    }
}
