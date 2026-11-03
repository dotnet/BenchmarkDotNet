using AsmArm64;
using BenchmarkDotNet.Extensions;
using Iced.Intel;
using System.Globalization;
using System.Text;

namespace BenchmarkDotNet.Disassemblers
{
    internal static class Arm64InstructionFormatter
    {
        internal static readonly Arm64InstructionFormattingOptions DefaultFormattingOptions = new()
        {
            AliasMode = Arm64InstructionAliasMode.Preferred,
            FormatProvider = CultureInfo.InvariantCulture,
            ImmediateFormat = Arm64NumericFormat.Auto,
            LabelOffsetFormat = Arm64NumericFormat.Auto,
            MemoryOffsetFormat = Arm64NumericFormat.Auto,
            UseUppercaseHex = true,
            UseUppercaseText = false,
            PrintDefaultOperands = false,
        };

        internal static readonly Arm64InstructionFormattingOptions BaseInstructionAliasModeFormattingOptions = new()
        {
            AliasMode = Arm64InstructionAliasMode.BaseInstruction,
            FormatProvider = CultureInfo.InvariantCulture,
            ImmediateFormat = Arm64NumericFormat.Auto,
            LabelOffsetFormat = Arm64NumericFormat.Auto,
            MemoryOffsetFormat = Arm64NumericFormat.Auto,
            UseUppercaseHex = true,
            UseUppercaseText = false,
            PrintDefaultOperands = false,
        };

        internal static string Format(Arm64Instruction instruction)
        {
            var asm = new Arm64Asm
            {
                RawInstruction = instruction.RawInstruction,
                DisassembleSyntax = DisassembleSyntax.Gas,
            };
            return Format(asm, new Iced.Intel.FormatterOptions { }, printInstructionAddresses: false, 8, new Dictionary<ulong, string>());
        }

        internal static (string Mnemonic, string OperandsText) GetInstructionDetails(
            Arm64Instruction instruction,
            ulong instructionAddress,
            IReadOnlyDictionary<ulong, string>? symbols = null)
        {
            var formattingOptions = GetFormattingOptions(instruction);
            var instructionText = FormatInstruction(instruction, formattingOptions, instructionAddress, symbols);
            if (instructionText.IsBlank())
                return ("", "");

            var spaceIndex = instructionText.IndexOf(' ');
            return spaceIndex < 0
                ? (instructionText, "")
                : (instructionText[..spaceIndex], instructionText[(spaceIndex + 1)..]);
        }

        private static string FormatInstruction(
            Arm64Instruction instruction,
            Arm64InstructionFormattingOptions formattingOptions,
            ulong instructionAddress,
            IReadOnlyDictionary<ulong, string>? symbols)
        {
            // If base address is not specified, keep the raw relative label offsets.
            if (instructionAddress == 0)
                return instruction.ToString(formattingOptions);

            var labelDidNotFit = false;

            // Symbol names can be long, so grow the buffer until every resolved label fits.
            for (int bufferLength = 256; bufferLength <= 64 * 1024; bufferLength *= 2)
            {
                labelDidNotFit = false;
                var buffer = new char[bufferLength];

                if (instruction.TryFormat(formattingOptions, buffer, out int written, TryResolveLabel) && !labelDidNotFit)
                    return new string(buffer, 0, written);
            }

            return instruction.ToString(formattingOptions);

            // Helper local function to resolve each label operand while formatting.
            bool TryResolveLabel(long labelOffset, Span<char> destination, out int charsWritten)
            {
                var address = instruction.GetTargetAddress(instructionAddress, labelOffset);
                var text = symbols is not null && symbols.TryGetValue(address, out var name)
                    ? name
                    : $"#0x{address:X}";

                if (!text.AsSpan().TryCopyTo(destination))
                {
                    // Returning false makes AsmArm64 use the numeric fallback so that we can retry with a larger buffer.
                    labelDidNotFit = true;
                    charsWritten = 0;
                    return false;
                }

                charsWritten = text.Length;
                return true;
            }
        }

        internal static Arm64InstructionFormattingOptions GetFormattingOptions(Arm64Instruction instruction)
        {
            // AsmArm64 automatically resolve instruction mnemonic to preferred alias.
            // Too keep Gee.Extenal.Capstone disassembly results compatibility.
            // Use `Arm64InstructionAliasMode.BaseInstruction` for MOVN and MOVZ instructions.
            // Full list of instruction that use aliases: https://github.com/xoofx/AsmArm64/blob/main/src/AsmArm64/generated/Arm64InstructionAliasTable.gen.cs

            if (instruction.Mnemonic != Arm64Mnemonic.MOV)
                return DefaultFormattingOptions;

            var baseMnemonic = instruction.AsBaseInstruction().Mnemonic;
            switch (baseMnemonic)
            {
                case Arm64Mnemonic.MOVN:
                case Arm64Mnemonic.MOVZ:
                    return BaseInstructionAliasModeFormattingOptions;

                default:
                    return DefaultFormattingOptions;
            }
        }

        // FormatterOptions is an Intel-specific concept that comes from the Iced library, but since our users can pass custom
        // Iced Formatter to DisassemblyDiagnoserConfig and it provides all the settings we need, we just reuse it here.
        internal static string Format(
            Arm64Asm asm,
            FormatterOptions formatterOptions,
            bool printInstructionAddresses,
            uint pointerSize,
            IReadOnlyDictionary<ulong, string> symbols)
        {
            StringBuilder output = new(256);

            if (printInstructionAddresses)
            {
                FormatInstructionPointer(asm.InstructionPointer, formatterOptions, pointerSize, output);
            }

            var (mnemonic, operandsText) = GetInstructionDetails(asm.Instruction, asm.InstructionPointer, symbols);

            var padRight = operandsText != ""
                ? Math.Max(formatterOptions.FirstOperandCharIndex, mnemonic.Length + 1)
                : 0;
            output.Append(mnemonic.PadRight(padRight));
            output.Append(operandsText);

            return output.ToString();
        }

        private static void FormatInstructionPointer(ulong address, FormatterOptions formatterOptions, uint pointerSize, StringBuilder output)
        {
            string ipFormat = formatterOptions.LeadingZeroes
                ? pointerSize == 4 ? "X8" : "X16"
                : "X";

            output.Append(address.ToString(ipFormat));
            output.Append(' ');
        }
    }
}
