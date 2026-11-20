using AsmArm64;
using AwesomeAssertions;
using BenchmarkDotNet.Disassemblers;
using static AsmArm64.Arm64RegisterW;
using static AsmArm64.Arm64RegisterX;
using static AsmArm64.Arm64RegisterSP;
using static AsmArm64.Arm64InstructionFactory;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public partial class Arm64InstructionFormatterTests : Arm64DisassemblerTestBase
{
    [Theory]
    [MemberData(nameof(TestData.FormatOptionPatterns), MemberType = typeof(TestData))]
    public void FormatInstruction(uint rawInstruction, string expected)
    {
        // Arrange
        var asm = ToArm64Asm(rawInstruction, address: 0);

        // Act
        var result = Format(asm);

        // Assert
        result.Should().Be(expected);
    }

    public static partial class TestData
    {
        public static TheoryData<uint, string> FormatOptionPatterns => new()
        {
            // Test cases to validate LabelOffsetFormat
            { B(0x8),                     "b #8"},    // single digits representable value should be rendered with decimal format.
            { B(0x100),                   "b #0x100"},
            { B(0xC),                     "b #0xC"},

            // Test cases to validate ImmediateFormat
            { MOVZ(W0, 0x8),              "movz w0, #8"},
            { MOVZ(X0, 0x100),            "movz x0, #0x100"},
            { MOVZ(X0, 0xFFF),            "movz x0, #0xFFF"},
            { MOVZ(X0, 0x100, amount:16), "movz x0, #0x100, lsl #16"}, // Shift amount should be rendered as decimal.

            // Test cases to validate MemoryOffsetFormat
            { LDR(X0, new Arm64ImmediateMemoryAccessor(SP, 0x8)),   "ldr x0, [sp, #8]" },
            { LDR(X0, new Arm64ImmediateMemoryAccessor(SP, 0x100)), "ldr x0, [sp, #0x100]" },
            { LDR(X0, new Arm64ImmediateMemoryAccessor(SP, 0xf00)), "ldr x0, [sp, #0xF00]" },

            // Test cases to validate minus label offset.
            { B(-0x8),             "b #-8"},
            { B(-0x100),           "b #-0x100"},
            { ADRP(X0, -0x1000),   "adrp x0, #-0x1000"},

            // Test case to validate instruction aliases.
            { CMP(X0,0x00),        "cmp x0, #0"},
            { SUBS(XZR, X0, 0x00), "cmp x0, #0"}, // Resolved to CMP alias

            // Others
            { NOP(), "nop"},
        };
    }
}
