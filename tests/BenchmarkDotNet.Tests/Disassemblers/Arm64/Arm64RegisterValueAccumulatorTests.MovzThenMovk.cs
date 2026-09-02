// TODO: Remove #if directive when migrated to xunit.v3 or migrated to AsmArm64 based implementation.
#if NET
using AwesomeAssertions;
using Gee.External.Capstone.Arm64;
using static AsmArm64.Arm64RegisterX;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

// MOVZ->MOVK tests.
public partial class Arm64RegisterValueAccumulatorTests
{
    [Fact]
    public void MovzThenMovz_ShouldStartNewValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var instructions = new[]
        {
            Arm64TestInstructions.Movz(X0, 0x1111),     // movz x0, #0x1111
            Arm64TestInstructions.Movz(X1, 0x2222),     // movz x1, #0x2222
            Arm64TestInstructions.Movk(X1, 0x3333, 16), // movk x1, #0x3333, lsl #16
        };
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]); // Reset accumulated value.
        accumulator.Feed(instructions[2]);

        // Assert
        accumulator.HasValue.Should().BeTrue();
        accumulator.RegisterId.Should().Be(Arm64RegisterId.ARM64_REG_X1);
        accumulator.Value.Should().Be(0x3333_2222); // X1 register value is used.
    }

    [Fact]
    public void MovzThenMovk_ShouldHaveValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();

        // Act
        accumulator.Feed(Arm64TestInstructions.Movz(X0, 0x1234));     // movz x0, #0x1234
        accumulator.Feed(Arm64TestInstructions.Movk(X0, 0x5678, 16)); // movk x0, #0x5678, lsl #16

        // Assert
        accumulator.HasValue.Should().BeTrue();
        accumulator.Value.Should().Be(0x5678_1234L);
        accumulator.RegisterId.Should().Be(Arm64RegisterId.ARM64_REG_X0);
    }

    [Fact(Skip = "Current value accumulator expecting sequencial 0/16/32/48 shfit for MOVK.")]
    public void MovzThenMovk_WithSameShiftValue_ShouldHasAccumulatedValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();

        // Act
        accumulator.Feed(Arm64TestInstructions.Movz(X0, 0x1111));     // movz x0, #0x1111
        accumulator.Feed(Arm64TestInstructions.Movk(X0, 0x2222, 16)); // movk x0, #0x2222, lsl #16
        accumulator.Feed(Arm64TestInstructions.Movk(X0, 0x0022, 16)); // movk x0, #0x0022, lsl #16

        // Assert
        accumulator.HasValue.Should().BeTrue();
        accumulator.Value.Should().Be(0x0022_1111L); // Shifted value should overwrite bits.
    }

    [Fact]
    public void MovzThenMovk_WithUnexpectedShift_ShouldResetValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var instructions = new[]
        {
            Arm64TestInstructions.Movz(X0, 0x1234),    // movz x0, #0x1234
            Arm64TestInstructions.Movk(X0, 0x5678, 32) // movk x0, #0x5678, lsl #32
        };

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]); // Reset accumulated value, because _expectedMovkShift is not matched.

        // Assert
        accumulator.HasValue.Should().BeFalse();
    }
}
#endif
