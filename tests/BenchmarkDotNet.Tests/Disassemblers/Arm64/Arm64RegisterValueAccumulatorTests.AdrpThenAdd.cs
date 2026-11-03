using AwesomeAssertions;
using static AsmArm64.Arm64RegisterX;
using static AsmArm64.Arm64InstructionFactory;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

// ADPR->ADD tests.
public partial class Arm64RegisterValueAccumulatorTests
{
    [Fact]
    public void AdrpThenAdd_ShouldCalculateAddress()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var rawInstructions = new[]
        {
            ADRP(X0, 0x2000),   // adrp x0, #0x2000
            ADD(X0, X0, 0x123), // add x0, x0, #0x123
        };
        var instructions = rawInstructions.ToArm64Instructions();
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeTrue();
        accumulator.Value.Should().Be(0x2123L);
        accumulator.TrackedRegister.Should().Be(X0);
    }

    [Fact]
    public void AdrpThenAdd_WithNegativeOffset_ShouldClampToZero()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var rawInstructions = new[]
        {
            ADRP(X0, -0x1000),  // adrp x0, #-0x1000
            ADD(X0, X0, 0x100), // add x0, x0, #0x100
        };
        var instructions = rawInstructions.ToArm64Instructions();
        PrintInstructions(instructions); // Note: Capstone print minus offset as `0xFFFFFFFFFFFFF000`

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        // A negative combined value is invalid as a real address, so it is clamped to 0.
        accumulator.HasValue.Should().BeFalse();
        accumulator.Value.Should().Be(0UL);
    }

    [Fact]
    public void AdrpThenAdd_ThenAdd_ShouldResetValue()
    {
        var accumulator = CreateValueAccumulator();
        var rawInstructions = new[]
        {
            ADRP(X0, 0x1000),   // adrp x0, #0x1000
            ADD(X0, X0, 0x100), // add x0, x0, #0x100
            ADD(X0, X0, 0x200), // add x0, x0, #0x200
        };
        var instructions = rawInstructions.ToArm64Instructions();
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);
        accumulator.Feed(instructions[2]);

        // Assert
        accumulator.HasValue.Should().BeFalse();
    }

    [Fact]
    public void AdrpThenAdd_WithShiftedImm_ShouldCalculateAddress()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var rawInstructions = new[]
        {
            ADRP(X0, 0x1000000),           // adrp x0, #0x1000000
            ADD(X0, X0, 0xFFF, amount:12), // add x0, x0, #0xFFF, lsl #12
        };
        var instructions = rawInstructions.ToArm64Instructions();
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeTrue();
        accumulator.Value.Should().Be(0x1FFF000);
    }

    [Fact]
    public void AdrpThenAdd_DifferentDestinationRegister_ShouldKeepValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var rawInstructions = new[]
        {
            ADRP(X0, 0x2000),   // adrp x0, #0x2000
            ADD(X1, X0, 0x100), // add x1, x0, #0x100
        };
        var instructions = rawInstructions.ToArm64Instructions();
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        // The ADD writes X1, so the tracked X0 value (page address) is still valid.
        accumulator.HasValue.Should().BeTrue();
        accumulator.Value.Should().Be(0x2000UL);
        accumulator.TrackedRegister.Should().Be(X0);
    }

    [Fact]
    public void AdrpThenOther_ThenAdd_ShouldResetValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var rawInstructions = new[]
        {
            ADRP(X0, 0x1000),   // adrp x0, #0x1000
            MOVZ(X0, 0x100),    // movz x0, #0x100
            ADD(X0, X0, 0x100), // add x1, x0, #0x100
        };
        var instructions = rawInstructions.ToArm64Instructions();
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);
        accumulator.Feed(instructions[2]);

        // Assert
        accumulator.HasValue.Should().BeFalse(); // Register is not matched
    }
}
