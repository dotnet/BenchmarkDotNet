// TODO: Remove #if directive when migrated to xunit.v3 or migrated to AsmArm64 based implementation.
#if NET
using AsmArm64;
using AwesomeAssertions;
using BenchmarkDotNet.Disassemblers;
using Iced.Intel;
using System.Text.RegularExpressions;
using static AsmArm64.Arm64RegisterW;
using static AsmArm64.Arm64RegisterX;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public partial class Arm64InstructionFormatterTests
{
    private readonly ITestOutputHelper Output;

    private static readonly FormatterOptions FormatterOptions = new()
    {
        // FirstOperandCharIndex = 10, // Use DisassemblyDiagnoserConfig default config value.
    };

    public Arm64InstructionFormatterTests(ITestOutputHelper output)
    {
        Output = output;
    }

    [Theory]
    [MemberData(nameof(TestData.TestData01), MemberType = typeof(TestData))]
    public void FormatInstruction(uint rawInstruction, string expected)
    {
        // Arrange
        var asm = rawInstruction.ToArm64Asm(address: 0);

        // Act
        var result = asm.Format(FormatterOptions);

        // Assert
        result.Should().Be(expected);

        // Verify AsmArm64 compatibility
        var instruction = rawInstruction.ToAsmArm64Instruction();
        var text = instruction.ToString("X", null);
        text = ReplaceNoCompatibleText(instruction, text);

        text.Should().Be(expected);

    }

    [Fact]
    public void FormatInstruction_B_WithReferencedAddress()
    {
        // Arrange
        var rawInstruction = Arm64InstructionFactory.B(0x100);
        var asm = rawInstruction.ToArm64Asm(0, 0x10_000);

        var options = new FormatterOptions()
        {
            FirstOperandCharIndex = 6,
        };

        var symbols = new Dictionary<ulong, string>
        {
        };

        // Act
        var result = Arm64InstructionFormatter.Format(asm, options, printInstructionAddresses: true, pointerSize: 8, symbols);

        // TODO:
        // Assert
        result.Should().Be("0 b     #0x100");
    }

    // Temporary workaround code to pass AsmArm64/Capstone compatibility test.
    // See: https://github.com/xoofx/AsmArm64/issues/15
    private static string ReplaceNoCompatibleText(Arm64Instruction instruction, string text)
    {
        // Arm64LabelOffset seems not support `X` format, so we need to manually replace label from decimal format to hex format
        if (instruction.Flags.HasFlag(Arm64InstructionFlags.HasLabel))
        {
            text = Regex.Replace(text, @"#(-?\d+)\b", m =>
            {
                var value = int.Parse(m.Groups[1].Value);
                return Math.Abs(value) < 10
                    ? $"#{m.Groups[1].Value}"
                    : $"#0x{value:X}";
            });
        }

        // AsmArm64 use aliases for mnemonic for some instructions, so we need to replace them with the original instruction name
        switch (instruction.Id)
        {
            case Arm64InstructionId.MOV_movz_32_movewide:
            case Arm64InstructionId.MOV_movz_64_movewide:
                text = Regex.Replace(text, "^mov", "movz");
                break;
            default:
                break;
        }

        return text;
    }

    public static class TestData
    {
        public static TheoryData<uint, string> TestData01 => new()
        {
            { Arm64InstructionFactory.B(0x8),                     "b #8"}, // Label value that lower than 0x10 use decimal format.
            { Arm64InstructionFactory.B(0x100),                   "b #0x100"},
            // TODO: MOVZ instruction is printed as `mov` in AsmArm64.
            { Arm64InstructionFactory.MOVZ(W0, 0x100),            "movz w0, #0x100"},
            { Arm64InstructionFactory.MOVZ(X0, 0x100),            "movz x0, #0x100"},
            // TODO: MOVZ with shift amount representation is different in AsmArm64.
            // { Arm64InstructionFactory.MOVZ(X0, 0x100, amount:16), "movz x0, #0x100, lsl #16"}
            // TODO: Minus label offset representation is different between AsmArm64 and Gee.External.Capstone.  
            // { Arm64InstructionFactory.B(-8),                      "b #-8"}, // Gee.External.Capstone don't support minus label offset.
        };
    }
}

file static class ExtensionMethods
{
    public static string Format(
        this Arm64Asm asm,
        FormatterOptions options,
        bool printInstructionAddresses = false,
        Dictionary<ulong, string>? addressMappings = null)
    {
        uint pointerSize = 8;
        return Arm64InstructionFormatter.Format(asm, options, printInstructionAddresses, pointerSize, addressMappings ?? []);
    }

    public static Arm64Asm ToArm64Asm(this uint rawInstruction, ulong address, ulong? referencedAddress = null, bool isReferencedAddressIndirect = false)
    {
        if (referencedAddress <= ushort.MaxValue)
            referencedAddress = null;

        return new Arm64Asm
        {
            InstructionPointer = address,
            InstructionLength = 4,
            ReferencedAddress = referencedAddress,
            IsReferencedAddressIndirect = isReferencedAddressIndirect,
            Instruction = rawInstruction.ToCapstoneArm64Instruction(address),
        };
    }
}
#endif
