// TODO: Remove #if directive when migrated to xunit.v3 or migrated to AsmArm64 based implementation.
#if NET
using AwesomeAssertions;
using Gee.External.Capstone.Arm64;
using static AsmArm64.Arm64RegisterX;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

// ADPR->ADD tests.
public partial class Arm64RegisterValueAccumulatorTests
{
    [Fact]
    public void AdrpThenAdd_ShouldCalculateAddress()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var instructions = new[]
        {
            Arm64TestInstructions.Adrp(X0, 0x2000),   // adrp x0, #0x2000
            Arm64TestInstructions.Add(X0, X0, 0x123), // add x0, x0, #0x123
        };
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeTrue();
        accumulator.Value.Should().Be(0x2123L);
        accumulator.RegisterId.Should().Be(Arm64RegisterId.ARM64_REG_X0);
    }

    [Fact]
    public void AdrpThenAdd_WithNegativeOffset_ShouldCalculateAddress()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var instructions = new[]
        {
            Arm64TestInstructions.Adrp(X0, -0x1000),  // adrp x0, #-0x1000
            Arm64TestInstructions.Add(X0, X0, 0x100), // add x0, x0, #0x100
        };
        PrintInstructions(instructions); // Note: Capstone print minus offset as `0xFFFFFFFFFFFFF000`

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeTrue();
        accumulator.Value.Should().Be(-0x1000 + 0x100);
    }

    [Fact]
    public void AdrpThenAdd_ThenAdd_ShouldResetValue()
    {
        var accumulator = CreateValueAccumulator();
        var instructions = new[]
        {
            Arm64TestInstructions.Adrp(X0, 0x1000),   // adrp x0, #0x1000
            Arm64TestInstructions.Add(X0, X0, 0x100), // add x0, x0, #0x100
            Arm64TestInstructions.Add(X0, X0, 0x200), // add x0, x0, #0x200
        };
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);
        accumulator.Feed(instructions[2]);

        // Assert
        accumulator.HasValue.Should().BeFalse();
    }

    [Fact(Skip = "Shifted value is not supported on current implementation.")]
    public void AdrpThenAdd_WithShiftedImm_ShouldCalculateAddress()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var instructions = new[]
        {
            Arm64TestInstructions.Adrp(X0, 0x1000000),    // adrp x0, #0x1000000
            Arm64TestInstructions.Add(X0, X0, 0xFFF, 12), // add x0, x0, #0xFFF, lsl #12
        };
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeTrue();
        accumulator.Value.Should().Be(0x1FFF000);
    }

    [Fact]
    public void AdrpThenAdd_DifferentDestinationRegister_ShouldResetValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var instructions = new[]
        {
            Arm64TestInstructions.Adrp(X0, 0x2000),   // adrp x0, #0x2000
            Arm64TestInstructions.Add(X1, X0, 0x100), // add x1, x0, #0x100
        };
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeFalse(); // Register is not matched
    }

    [Fact(Skip = "On current implementation, `State.ExpectingAdd` don't reset value when unexpected instruction passed.")]
    public void AdrpThenOther_ThenAdd_ShouldResetValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var instructions = new[]
        {
            Arm64TestInstructions.Adrp(X0, 0x1000),   // adrp x0, #0x1000
            Arm64TestInstructions.Movz(X0, 0x100),    // movz x0, #0x100
            Arm64TestInstructions.Add(X0, X0, 0x100), // add x1, x0, #0x100
        };
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);
        accumulator.Feed(instructions[2]);

        // Assert
        accumulator.HasValue.Should().BeFalse(); // Register is not matched
    }
}
#endif
