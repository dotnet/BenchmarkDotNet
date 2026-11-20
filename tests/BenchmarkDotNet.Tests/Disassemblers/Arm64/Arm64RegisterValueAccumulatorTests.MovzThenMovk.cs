using AwesomeAssertions;
using static AsmArm64.Arm64RegisterX;
using static AsmArm64.Arm64InstructionFactory;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

// MOVZ->MOVK tests.
public partial class Arm64RegisterValueAccumulatorTests
{
    [Fact]
    public void MovzThenMovz_ShouldStartNewValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var rawInstructions = new[]
        {
            MOVZ(X0, 0x1111),             // movz x0, #0x1111
            MOVZ(X1, 0x2222),             // movz x1, #0x2222
            MOVK(X1, 0x3333, amount: 16), // movk x1, #0x3333, lsl #16
        };
        var instructions = rawInstructions.ToArm64Instructions();
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]); // Reset accumulated value.
        accumulator.Feed(instructions[2]);

        // Assert
        accumulator.HasValue.Should().BeTrue();
        accumulator.TrackedRegister.Should().Be(X1);
        accumulator.Value.Should().Be(0x3333_2222); // X1 register value is used.
    }

    [Fact]
    public void MovzThenMovk_ShouldHaveValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var rawInstructions = new[]
        {
            MOVZ(X0, 0x1234),            // movz x0, #0x1234
            MOVK(X0, 0x5678, amount: 16) // movk x0, #0x5678, lsl #16
        };
        var instructions = rawInstructions.ToArm64Instructions();

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeTrue();
        accumulator.Value.Should().Be(0x5678_1234L);
        accumulator.TrackedRegister.Should().Be(X0);
    }

    [Fact]
    public void MovzThenMovk_WithSameShiftValue_ShouldHasAccumulatedValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var rawInstructions = new[]
        {
            MOVZ(X0, 0x1111),             // movz x0, #0x1111
            MOVK(X0, 0x2222, amount: 16), // movk x0, #0x2222, lsl #16
            MOVK(X0, 0x0022, amount: 16)  // movk x0, #0x0022, lsl #16
        };
        var instructions = rawInstructions.ToArm64Instructions();

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);
        accumulator.Feed(instructions[2]);

        // Assert
        accumulator.HasValue.Should().BeTrue();
        accumulator.Value.Should().Be(0x0022_1111L); // Shifted value should overwrite bits.
    }

    [Fact]
    public void MovzThenMovk_WithSkippedShift_ShouldAccumulateValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var rawInstructions = new[]
        {
            MOVZ(X0, 0x1234),            // movz x0, #0x1234
            MOVK(X0, 0x5678, amount: 32) // movk x0, #0x5678, lsl #32
        };
        var instructions = rawInstructions.ToArm64Instructions();

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]); // Each MOVK clears and sets its own 16-bit lane, so order does not matter.

        // Assert
        accumulator.HasValue.Should().BeTrue();
        accumulator.Value.Should().Be(0x5678_0000_1234UL);
        accumulator.TrackedRegister.Should().Be(X0);
    }

    [Fact]
    public void MovzThenMovk_DifferentRegister_ShouldHaveValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var rawInstructions = new[]
        {
            MOVZ(X0, 0x1234),             // movz x0, #0x1234
            MOVK(X1, 0x5678, amount: 16), // movk x1, #0x5678, lsl #16
        };
        var instructions = rawInstructions.ToArm64Instructions();

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeTrue();
        accumulator.Value.Should().Be(0x1234);
        accumulator.TrackedRegister.Should().Be(X0);
    }
}
