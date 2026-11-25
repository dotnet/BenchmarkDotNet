using AwesomeAssertions;
using BenchmarkDotNet.Disassemblers;
using static AsmArm64.Arm64RegisterX;
using static AsmArm64.Arm64InstructionFactory;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

// MOVZ with unconditional branch tests.
public partial class Arm64RegisterValueAccumulatorTests
{
    [Fact]
    public void MovzThenUnconditionalB_ShouldResetValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var rawInstructions = new[]
        {
            MOVZ(X0, 0x1000), // movz x0, #0x1000
            B(0x100),         // b #0x100
        };
        var instructions = rawInstructions.ToArm64Instructions();
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeFalse();
        accumulator.TrackedRegister.Should().Be(default);
    }


    [Fact]
    public void MovzThenTbz_ShouldKeepValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var rawInstructions = new[]
        {
            MOVZ(X0, 0x1000),   // movz x0, #0x1000
            TBZ(X0, 63, 0x100)  // tbz x0, #63, #0x100
        };
        var instructions = rawInstructions.ToArm64Instructions();
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeTrue();
        accumulator.Value.Should().Be(0x1000UL); // TBZ is a conditional branch that does not overwrite the tracked register,
        accumulator.TrackedRegister.Should().Be(X0);

        instructions[1].BelongsToGroup(Arm64InstructionGroupId.ARM64_GRP_JUMP).Should().BeTrue();
        instructions[1].BelongsToGroup(Arm64InstructionGroupId.ARM64_GRP_BRANCH_RELATIVE).Should().BeTrue();
    }

    [Fact]
    public void MovzThenRet_ShouldResetValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var rawInstructions = new[]
        {
            MOVZ(X0, 0x1000), // movz x0, #0x1000
            RET(),            // ret
        };
        var instructions = rawInstructions.ToArm64Instructions();
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeFalse();
        // accumulator.RegisterId.Should().Be(Arm64RegisterId.Invalid);
    }

    [Fact]
    public void MovzThenDrps_ShouldResetValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var rawInstructions = new[]
        {
            MOVZ(X0, 0x1000), // movz x0, #0x1000
            DRPS(),           // drps
        };
        var instructions = rawInstructions.ToArm64Instructions();

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeFalse();
        accumulator.TrackedRegister.Should().Be(default);
    }

    [Fact]
    public void MovzThenBl_ShouldResetValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var rawInstructions = new[]
        {
            MOVZ(X0, 0x1000),  // movz x0, #0x1000
            BL(0x100),         // bl 0x100
        };
        var instructions = rawInstructions.ToArm64Instructions();
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeFalse();
        accumulator.TrackedRegister.Should().Be(default);
    }

    [Fact]
    public void MovzThenBlr_ShouldResetValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var rawInstructions = new[]
        {
            MOVZ(X0, 0x1000), // movz x0, #0x1000
            BLR(X0),          // bl x0
        };
        var instructions = rawInstructions.ToArm64Instructions();

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeFalse();
        accumulator.TrackedRegister.Should().Be(default);
    }
}
