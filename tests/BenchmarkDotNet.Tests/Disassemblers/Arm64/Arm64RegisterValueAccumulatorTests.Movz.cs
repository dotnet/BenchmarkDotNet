using AwesomeAssertions;
using static AsmArm64.Arm64RegisterX;
using static AsmArm64.Arm64InstructionFactory;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

// MOVZ tests.
public partial class Arm64RegisterValueAccumulatorTests
{
    [Fact]
    public void Movz_WithShiftedImmediateValue_ShouldStartNewValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var rawInstructions = new[]
        {
            MOVZ(X0, 0x1111, amount: 0),     // movz x0, #0x1111
            MOVZ(X0, 0x2222, amount: 16),    // movz x0, #0x2222, lsl #16
            MOVZ(X0, 0x3333, amount: 32),    // movz x0, #0x3333, lsl #32
        };
        var instructions = rawInstructions.ToArm64Instructions();
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);
        accumulator.Feed(instructions[2]);

        // Assert
        accumulator.HasValue.Should().BeTrue();
        accumulator.TrackedRegister.Should().Be(X0);
        accumulator.Value.Should().Be(0x3333_0000_0000);
    }

    [Fact]
    public void Movz_ZeroImmediate_ShouldNotHaveValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var rawInstructions = new[]
        {
            MOVZ(X0, 0x0), // movz x0, #0x0
        };
        var instructions = rawInstructions.ToArm64Instructions();
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);

        // Assert: address 0 is never valid, so it is excluded from HasValue.
        accumulator.HasValue.Should().BeFalse();
        accumulator.Value.Should().Be(0UL);
    }
}
