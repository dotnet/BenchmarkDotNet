// TODO: Remove #if directive when migrated to xunit.v3 or migrated to AsmArm64 based implementation.
#if NET
using AsmArm64;
using AwesomeAssertions;
using Gee.External.Capstone.Arm64;
using static AsmArm64.Arm64RegisterX;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

// MOVZ with conditional branch instruction tests
public partial class Arm64RegisterValueAccumulatorTests
{
    [Fact]
    public void MovzThenCbz_ShouldKeepValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var instructions = new[]
        {
            Arm64TestInstructions.Movz(X0, 0x1000), // movz x0, #0x1000
            Arm64TestInstructions.Cbz(X0, 0x0100),  // cbz x0, #0x0100
        };
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeTrue();
        accumulator.Value.Should().Be(0x1000);
        accumulator.RegisterId.Should().Be(Arm64RegisterId.ARM64_REG_X0);
    }

    [Fact]
    public void MovzThenCbnz_ShouldKeepValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var instructions = new[]
        {
            Arm64TestInstructions.Movz(X0, 0x1000), // movz x0, #0x1000
            Arm64TestInstructions.Cbnz(X0, 0x0100), // cbnz x0, #0x0100 
        };
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeTrue();
        accumulator.Value.Should().Be(0x1000);
        accumulator.RegisterId.Should().Be(Arm64RegisterId.ARM64_REG_X0);
    }

    [Fact]
    public void MovzThenConditionalB_ShouldKeepValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var instructions = new[]
        {
            Arm64TestInstructions.Movz(X0, 0x1000),                                        // movz x0, #0x1000
            Arm64TestInstructions.B(Arm64ConditionalKind.EQ, 0x100), // b.eq x0, #0x0100 
        };
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeTrue();
        accumulator.Value.Should().Be(0x1000); // branch instruction's immediate value is not affects accumulated value.
        accumulator.RegisterId.Should().Be(Arm64RegisterId.ARM64_REG_X0);
    }
}
#endif
