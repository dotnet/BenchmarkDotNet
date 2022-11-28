using Gee.External.Capstone.Arm64;
using Iced.Intel;
using System.Collections.Generic;
using System.Text;

namespace BenchmarkDotNet.Disassemblers
{
    internal static class Arm64InstructionFormatter
    {
        // FormatterOptions is an Intel-specific concept that comes from the Iced library, but since our users can pass custom
        // Iced Formatter to DisassemblyDiagnoserConfig and it provides all the settings we need, we just reuse it here.
        internal static string Format(Arm64Asm asm, FormatterOptions formatterOptions,
            bool printInstructionAddresses, uint pointerSize, IReadOnlyDictionary<ulong, string> symbols)
        {
            StringBuilder output = new ();
            Arm64Instruction instruction = asm.Instruction;

            if (printInstructionAddresses)
            {
                FormatInstructionPointer(instruction, formatterOptions, pointerSize, output);
            }

            output.Append(instruction.Mnemonic.ToString().PadRight(formatterOptions.FirstOperandCharIndex));

            if (asm.ReferencedAddress.HasValue && !asm.IsReferencedAddressIndirect && symbols.TryGetValue(asm.ReferencedAddress.Value, out string name))
            {
                string partToReplace = $"#0x{asm.ReferencedAddress.Value:x}";
                output.Append(instruction.Operand.Replace(partToReplace, name));
            }
            else
            {
                output.Append(instruction.Operand);
            }

            return output.ToString();
        }

        private static void FormatInstructionPointer(Arm64Instruction instruction, FormatterOptions formatterOptions, uint pointerSize, StringBuilder output)
        {
            string ipFormat = formatterOptions.LeadingZeroes
                ? pointerSize == 4 ? "X8" : "X16"
                : "X";

            output.Append(instruction.Address.ToString(ipFormat));
            output.Append(' ');
        }
    }
}
