using AsmArm64;
using AwesomeAssertions;
using static AsmArm64.Arm64InstructionFactory;
using static AsmArm64.Arm64RegisterX;
using BenchmarkDotNet.Disassemblers;


namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public partial class Arm64InstructionExtensionsTests
{

    [Fact]
    public void TryGetShiftedImmediateValue_WithNoShiftOperand_ShouldReturnImmediate()
    {
        // Arrange
        var instruction = Arm64Instruction.Decode(ADD(X0, X0, 0x05)); // add x0, x0, #5

        // Act
        var result = instruction.TryGetShiftedImmediateValue(2, out var value);

        // Assert
        result.Should().BeTrue();
        value.Should().Be(5);
    }

    [Fact]
    public void TryGetShiftedImmediateValue_WithLslShift_ShouldApplyShift()
    {
        // Arrange
        var instruction = Arm64Instruction.Decode(ADD(X0, X0, 0x05, amount: 12)); // add x0, x0, #5, lsl #12

        /// Act
        var result = instruction.TryGetShiftedImmediateValue(2, out var value);

        // Assert
        result.Should().BeTrue();
        value.Should().Be(5L << 12);
    }

    [Fact]
    public void TryGetShiftedImmediateValue_WithOutOfRangeIndex_ShouldReturnFalse()
    {
        // Arrange
        var instruction = Arm64Instruction.Decode(ADD(X0, X0, 0x05)); // add x0, x0, #5

        // Act
        var result = instruction.TryGetShiftedImmediateValue(99, out var value);

        // Assert
        result.Should().BeFalse();
        value.Should().Be(0);
    }

    [Fact]
    public void TryGetShiftedImmediateValue_WithNonImmediateOperand_ShouldReturnFalse()
    {
        // Arrange
        var instruction = Arm64Instruction.Decode(ADD(X0, X0, 0x05)); // add x0, x0, #5

        // Act
        var result = instruction.TryGetShiftedImmediateValue(0, out var value);

        // Assert
        result.Should().BeFalse();
        value.Should().Be(0);
    }
}
