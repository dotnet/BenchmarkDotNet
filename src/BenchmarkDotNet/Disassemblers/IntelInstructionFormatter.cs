using Iced.Intel;

namespace BenchmarkDotNet.Disassemblers
{
    internal static class IntelInstructionFormatter
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
