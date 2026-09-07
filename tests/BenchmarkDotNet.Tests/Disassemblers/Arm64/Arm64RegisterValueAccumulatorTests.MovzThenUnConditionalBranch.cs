// TODO: Remove #if directive when migrated to xunit.v3 or migrated to AsmArm64 based implementation.
#if NET
using AwesomeAssertions;
using Gee.External.Capstone.Arm64;
using static AsmArm64.Arm64RegisterX;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

// MOVZ with unconditional branch tests.
public partial class Arm64RegisterValueAccumulatorTests
{
    [Fact]
    public void MovzThenUnconditionalB_ShouldResetValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var instructions = new[]
        {
            Arm64TestInstructions.Movz(X0, 0x1000), // movz x0, #0x1000
            Arm64TestInstructions.B(0x100),         // b #0x100
        };
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeFalse();

        // TODO: Current accumulator don't reset registerId.
        // accumulator.RegisterId.Should().Be(Arm64RegisterId.Invalid);
    }


    [Fact]
    public void MovzThenTbz_ShouldResetValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var instructions = new[]
        {
            Arm64TestInstructions.Movz(X0, 0x1000),   // movz x0, #0x1000
            Arm64TestInstructions.Tbz(X0, 63, 0x100)  // tbz x0, #63, #0x100
        };
        PrintInstructions(instructions);

        // Act        
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeFalse();

        // TODO: Current accumulator don't reset registerId.
        // accumulator.RegisterId.Should().Be(Arm64RegisterId.Invalid);

        var details = instructions[1].Details;
        details.BelongsToGroup(Arm64InstructionGroupId.ARM64_GRP_JUMP).Should().BeTrue();
        details.BelongsToGroup(Arm64InstructionGroupId.ARM64_GRP_BRANCH_RELATIVE).Should().BeTrue();
    }

    [Fact(Skip = "Capstone 4.0.2 don't grouping RET instruction as JUMP group.")]
    public void MovzThenRet_ShouldResetValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var instructions = new[]
        {
            Arm64TestInstructions.Movz(X0, 0x1000), // movz x0, #0x1000
            Arm64TestInstructions.Ret(),            // ret
        };
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeFalse();
        // accumulator.RegisterId.Should().Be(Arm64RegisterId.Invalid);
    }

    [Fact(Skip = "Capstone 4.0.2 don't grouping DRPS instruction as JUMP group.")]
    public void MovzThenDrps_ShouldResetValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var instructions = new[]
        {
            Arm64TestInstructions.Movz(X0, 0x1000), // movz x0, #0x1000
            Arm64TestInstructions.Drps(),           // drps
        };

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeFalse();

        // TODO: Current implementation don't reset state.
        // accumulator.RegisterId.Should().Be(Arm64RegisterId.Invalid);
    }

    [Fact]
    public void MovzThenBl_ShouldResetValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var instructions = new[]
        {
            Arm64TestInstructions.Movz(X0, 0x1000),  // movz x0, #0x1000
            Arm64TestInstructions.Bl(0x100),         // bl 0x100
        };
        PrintInstructions(instructions);

        // Act        
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert       
        accumulator.HasValue.Should().BeFalse();

        // TODO: Current implementation don't reset state.
        // accumulator.RegisterId.Should().Be(Arm64RegisterId.Invalid);
    }

    [Fact]
    public void MovzThenBlr_ShouldResetValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var instructions = new[]
        {
            Arm64TestInstructions.Movz(X0, 0x1000), // movz x0, #0x1000
            Arm64TestInstructions.Blr(X0),          // bl x0
        };

        // Act        
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert       
        accumulator.HasValue.Should().BeFalse();

        // TODO: Current implementation don't reset state.
        // accumulator.RegisterId.Should().Be(Arm64RegisterId.Invalid);
    }
}
#endif
