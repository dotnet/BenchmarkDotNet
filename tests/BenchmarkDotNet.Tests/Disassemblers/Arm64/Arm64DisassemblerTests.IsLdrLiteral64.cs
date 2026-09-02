#if NET8_0_OR_GREATER
using AsmArm64;
using AwesomeAssertions;
using static AsmArm64.Arm64RegisterX;
using static AsmArm64.Arm64RegisterW;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public partial class Arm64DisassemblerTests
{
    [Fact]
    public void IsLdrLiteral64()
    {
        // Arrange
        const int expectedOffsetBytes = 0x100;
        var rawInstruction = Arm64InstructionFactory.LDR(X5, label: expectedOffsetBytes);

        // Act
        var result = Arm64DisassemblerHelper.IsLdrLiteral64(rawInstruction, out var rt, out var offsetBytes);

        // Assert
        result.Should().BeTrue();

        rt.Should().Be(5);
        offsetBytes.Should().Be(expectedOffsetBytes);

        // Additional assertions
        var instruction = Arm64Instruction.Decode(rawInstruction);
        instruction.Id.Should().Be(Arm64InstructionId.LDR_64_loadlit);

        var registerOperand = (Arm64RegisterOperand)instruction.GetOperand(0);
        registerOperand.Value.Should().Be((Arm64RegisterAny)X5);

        var labelOperand = (Arm64LabelOperand)instruction.GetOperand(1);
        labelOperand.Offset.Should().Be(expectedOffsetBytes);
    }

    [Fact]
    public void IsLdrLiteral64_WithMinValue()
    {
        // Arrange
        const int expectedOffsetBytes = -1_048_576;
        var rawInstruction = Arm64InstructionFactory.LDR(X0, label: new Arm64LabelOffset(expectedOffsetBytes));

        // Act
        var result = Arm64DisassemblerHelper.IsLdrLiteral64(rawInstruction, out var rt, out var offsetBytes);

        // Assert
        result.Should().BeTrue();
        rt.Should().Be(0);
        offsetBytes.Should().Be(expectedOffsetBytes);

        // Additional assertions
        var instruction = Arm64Instruction.Decode(rawInstruction);
        instruction.Id.Should().Be(Arm64InstructionId.LDR_64_loadlit);

        var registerOperand = (Arm64RegisterOperand)instruction.GetOperand(0);
        registerOperand.Value.Should().Be((Arm64RegisterAny)X0);


        var labelOperand = (Arm64LabelOperand)instruction.GetOperand(1);
        labelOperand.Offset.Should().Be(expectedOffsetBytes);
    }

    [Fact]
    public void IsLdrLiteral64_WithMaxValue()
    {
        // Arrange
        const int expectedOffsetBytes = 1_048_572;
        var rawInstruction = Arm64InstructionFactory.LDR(X0, label: new Arm64LabelOffset(expectedOffsetBytes));

        // Act
        var result = Arm64DisassemblerHelper.IsLdrLiteral64(rawInstruction, out var rt, out var offsetBytes);

        // Assert
        result.Should().BeTrue();
        rt.Should().Be(0);
        offsetBytes.Should().Be(expectedOffsetBytes);

        // Additional assertions
        var instruction = Arm64Instruction.Decode(rawInstruction);
        instruction.Id.Should().Be(Arm64InstructionId.LDR_64_loadlit);

        var registerOperand = (Arm64RegisterOperand)instruction.GetOperand(0);
        registerOperand.Value.Should().Be((Arm64RegisterAny)X0);

        var labelOperand = (Arm64LabelOperand)instruction.GetOperand(1);
        labelOperand.Offset.Should().Be(expectedOffsetBytes);
    }

    [Fact]
    public void IsLdrLiteral64_With32bitRegister_ShouldBeFalse()
    {
        // Arrange
        var rawInstruction = Arm64InstructionFactory.LDR(W5, label: 0x100);

        // Act
        var result = Arm64DisassemblerHelper.IsLdrLiteral64(rawInstruction, out var rt, out var offsetBytes);

        // Assert
        result.Should().BeFalse();

        // Additional assertions
        var instruction = Arm64Instruction.Decode(rawInstruction);
        instruction.Id.Should().Be(Arm64InstructionId.LDR_32_loadlit);

        var registerOperand = (Arm64RegisterOperand)instruction.GetOperand(0);
        registerOperand.Value.Should().Be((Arm64RegisterAny)W5);
    }
}
#endif