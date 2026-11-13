using AsmArm64;
using AwesomeAssertions;
using static AsmArm64.Arm64RegisterX;
using static AsmArm64.Arm64InstructionFactory;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

// MOVZ with conditional branch instruction tests
public partial class Arm64RegisterValueAccumulatorTests
{
    [Fact]
    public void MovzThenCbz_ShouldKeepValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var rawInstructions = new[]
        {
            MOVZ(X0, 0x1000), // movz x0, #0x1000
            CBZ(X0, 0x0100),  // cbz x0, #0x0100
        };
        var instructions = rawInstructions.ToArm64Instructions();
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeTrue();
        accumulator.Value.Should().Be(0x1000);
        accumulator.TrackedRegister.Should().Be(X0);
    }

    [Fact]
    public void MovzThenCbnz_ShouldKeepValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var rawInstructions = new[]
        {
            MOVZ(X0, 0x1000), // movz x0, #0x1000
            CBNZ(X0, 0x0100), // cbnz x0, #0x0100
        };
        var instructions = rawInstructions.ToArm64Instructions();
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeTrue();
        accumulator.Value.Should().Be(0x1000);
        accumulator.TrackedRegister.Should().Be(X0);
    }

    [Fact]
    public void MovzThenConditionalB_ShouldKeepValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var rawInstructions = new[]
        {
            MOVZ(X0, 0x1000),                  // movz x0, #0x1000
            B(Arm64ConditionalKind.EQ, 0x100), // b.eq x0, #0x0100 
        };
        var instructions = rawInstructions.ToArm64Instructions();
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeTrue();
        accumulator.Value.Should().Be(0x1000); // branch instruction's immediate value is not affects accumulated value.
        accumulator.TrackedRegister.Should().Be(X0);
    }
}
