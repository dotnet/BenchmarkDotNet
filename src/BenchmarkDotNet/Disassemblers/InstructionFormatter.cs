using Iced.Intel;
using System;

namespace BenchmarkDotNet.Disassemblers
{
    internal static class CodeFormatter
    {
        internal static string Format(SourceCode sourceCode, Formatter formatter, bool printInstructionAddresses, uint pointerSize)
        {
            switch (sourceCode)
            {
                case Asm asm when asm.IntelInstruction.HasValue:
                    return InstructionFormatter.Format(asm.IntelInstruction.Value, formatter, printInstructionAddresses, pointerSize);
                case Asm asm when asm.Arm64Instruction is not null:
                {
                    string operand = asm.Arm64Instruction.Operand;

                    // Symbolize branch and call instructions target with immediate address argument
                    if (asm.Arm64Instruction.Details.BelongsToGroup(Gee.External.Capstone.Arm64.Arm64InstructionGroupId.ARM64_GRP_BRANCH_RELATIVE) &&
                        asm.ReferencedAddress != null &&
                        !asm.IsReferencedAddressIndirect &&
                        (asm.AddressToNameMapping.TryGetValue(asm.ReferencedAddress.Value, out string text) ||
                        asm?.AddressToLabelMapping.TryGetValue(asm.ReferencedAddress.Value, out text) == true))
                    {
                        string partToReplace = $"#0x{asm.ReferencedAddress.Value:x}";
                        operand = operand.Replace(partToReplace, text);
                    }

                    string instructionAddress = printInstructionAddresses ? $"{asm.Arm64Instruction.Address:X16} " : string.Empty;
                    return $"{instructionAddress}{asm.Arm64Instruction.Mnemonic} {operand}";
                }
                case Sharp sharp:
                    return sharp.Text;
                case MonoCode mono:
                    return mono.Text;
                default:
                    throw new NotSupportedException();
            }
        }
    }

    internal static class InstructionFormatter
    {
        internal static string Format(Instruction instruction, Formatter formatter, bool printInstructionAddresses, uint pointerSize)
        {
            var output = new StringOutput();

            if (printInstructionAddresses)
            {
                FormatInstructionPointer(instruction, formatter, pointerSize, output);
            }

            formatter.Format(instruction, output);

            return output.ToString();
        }

        private static void FormatInstructionPointer(Instruction instruction, Formatter formatter, uint pointerSize, StringOutput output)
        {
            string ipFormat = formatter.Options.LeadingZeroes
                ? pointerSize == 4 ? "X8" : "X16"
                : "X";

            output.Write(instruction.IP.ToString(ipFormat), FormatterTextKind.Text);
            output.Write(" ", FormatterTextKind.Text);
        }
    }
}
