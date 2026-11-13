using AsmArm64;
using AwesomeAssertions;
using static AsmArm64.Arm64RegisterX;
using static AsmArm64.Arm64InstructionFactory;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public partial class Arm64InstructionFormatterTests : Arm64DisassemblerTestBase
{
    [Theory]
    [InlineData(0x2000, "adrp x0, #0xF00000003000")]
    [InlineData(-0x1000, "adrp x0, #0xF00000000000")]
    public void FormatInstruction_WithAdrp_ResolvesLabelRelativeToContainingPage(int labelOffset, string expected)
    {
        // Arrange
        const ulong InstructionAddress = DummyBaseAddress + 0x1234; // Not page aligned.
        var asm = ToArm64Asm(ADRP(X0, labelOffset), InstructionAddress);

        // Act
        var result = Format(asm);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void FormatInstruction_WithLdrLiteral_ReplacesAddressWithLabel()
    {
        // Arrange
        var asm = ToArm64Asm(LDR(X0, 0x100), DummyBaseAddress); // ldr x0, #0x100
        var symbols = new Dictionary<ulong, string>
        {
            [DummyBaseAddress + 0x100] = "M00_L00",
        };

        // Act
        var result = Format(asm, addressMappings: symbols);

        // Assert
        result.Should().Be("ldr x0, M00_L00");
    }

    [Theory]
    [InlineData(-4, "b #-4")]
    [InlineData(-8, "b #-8")]
    [InlineData(-12, "b #-0xC")]
    [InlineData(-16, "b #-0x10")]
    public void FormatInstruction_WithBranchNegativeSmallOffset_AtZeroAddress_KeepsRawOffset(int labelOffset, string expected)
    {
        // Arrange
        var asm = ToArm64Asm(B(new Arm64LabelOffset(labelOffset)), 0);

        // Act
        var result = Format(asm);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void FormatInstruction_WithLongSymbolName_ResolvesLabel()
    {
        // Arrange
        var symbolName = new string('x', 512);
        var asm = ToArm64Asm(LDR(X0, 0x100), DummyBaseAddress); // ldr x0, #0x100
        var symbols = new Dictionary<ulong, string>
        {
            [DummyBaseAddress + 0x100] = symbolName,
        };

        // Act
        var result = Format(asm, addressMappings: symbols);

        // Assert
        result.Should().Be($"ldr x0, {symbolName}");
    }
}
