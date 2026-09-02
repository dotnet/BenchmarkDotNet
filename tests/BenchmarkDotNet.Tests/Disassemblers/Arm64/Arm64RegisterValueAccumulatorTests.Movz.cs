using AwesomeAssertions;
using Gee.External.Capstone.Arm64;
using static AsmArm64.Arm64RegisterX;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

// MOVZ tests.
public partial class Arm64RegisterValueAccumulatorTests
{
    [Fact(Skip = "On current implementation, shifted immediate value is not supported.")]
    public void Movz_WithShiftedImmediateValue_ShouldStartNewValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var instructions = new[]
        {
            Arm64TestInstructions.Movz(X0, 0x1111, 0),     // movz x0, #0x1111
            Arm64TestInstructions.Movz(X0, 0x2222, 16),    // movz x0, #0x2222, lsl #16
            Arm64TestInstructions.Movz(X0, 0x3333, 32),    // movz x0, #0x3333, lsl #32
        };
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);
        accumulator.Feed(instructions[2]);

        // Assert
        accumulator.HasValue.Should().BeTrue();
        accumulator.RegisterId.Should().Be(Arm64RegisterId.ARM64_REG_X0);
        accumulator.Value.Should().Be(0x3333_2222_1111);
    }
}
