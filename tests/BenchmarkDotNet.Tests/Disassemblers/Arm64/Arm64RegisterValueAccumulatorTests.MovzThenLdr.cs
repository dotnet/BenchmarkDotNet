using AsmArm64;
using AwesomeAssertions;
using static AsmArm64.Arm64InstructionFactory;
using static AsmArm64.Arm64RegisterX;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

// MOVZ->LDR tests
public partial class Arm64RegisterValueAccumulatorTests
{
    [Fact]
    public void MovzThenLdr_SameRegister_ShouldHaveValue()
    {
        // Arrange
        using var clrRuntime = new MockMemory()
           .AddPointer(MovzOffset, ExpectedResultAddress)
           .ToMockClrRuntime();
        var accumulator = CreateValueAccumulator(clrRuntime);
        var rawInstructions = new[]
        {
            MOVZ(X0, MovzOffset),                             // movz x0, #0x1000
            LDR(X0, new Arm64ImmediateMemoryAccessor(X0, 0)), // ldr x0, [x0]
        };
        var instructions = rawInstructions.ToArm64Instructions();
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeTrue();
        accumulator.TrackedRegister.Should().Be(X0);
        accumulator.Value.Should().Be(ExpectedResultAddress); // It should be value that is returned by ReadPointer.
    }

    [Fact]
    public void MovzThenLdr_DifferentRegister_ShouldHaveValue()
    {
        // Arrange
        using var clrRuntime = new MockMemory()
           .AddPointer(MovzOffset, ExpectedResultAddress)
           .ToMockClrRuntime();
        var accumulator = CreateValueAccumulator(clrRuntime);
        var rawInstructions = new[]
        {
            MOVZ(X0, MovzOffset),                             // movz x0, #0x1000
            LDR(X1, new Arm64ImmediateMemoryAccessor(X0, 0)), // ldr x1, [x0]
        };
        var instructions = rawInstructions.ToArm64Instructions();
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeTrue();
        accumulator.TrackedRegister.Should().Be(X1); // Value is loaded to X1 register.
        accumulator.Value.Should().Be(ExpectedResultAddress); // It should be value that is returned by ReadPointer.
    }

    [Fact]
    public void MovzThenLdr_WithDisplacement_ShouldNotHaveValue()
    {
        // Arrange
        using var clrRuntime = new MockMemory()
           .AddPointer(MovzOffset, ExpectedResultAddress)
           .ToMockClrRuntime();
        var accumulator = CreateValueAccumulator(clrRuntime);
        var rawInstructions = new[]
        {
            MOVZ(X0, MovzOffset),                                 // movz x0, #0x1000
            LDR(X0, new Arm64ImmediateMemoryAccessor(X0, 0x100)), // ldr x0, [x0, #0x100]
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
    public void MovzThenLdr_WithIndexRegister_ShouldNotHaveValue()
    {
        // Arrange
        using var clrRuntime = new MockMemory()
           .AddPointer(MovzOffset, ExpectedResultAddress)
           .ToMockClrRuntime();
        var accumulator = CreateValueAccumulator(clrRuntime);
        var rawInstructions = new[]
        {
            MOVZ(X0, MovzOffset),                                                             // movz x0, #0x1000
            LDR(X0, new Arm64RegisterXExtendMemoryAccessor(X0, X1, new Arm64MemoryExtend())), // ldr x0, [x0, x1]
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
    public void MovzThenLdrLiteral_ShouldNotHaveValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var rawInstructions = new[]
        {
            MOVZ(X0, MovzOffset),                                 // movz x0, #0x1000
            LDR(X0, new Arm64ImmediateMemoryAccessor(X0, 0x100)), // ldr x0, #0x100
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
    public void MovzThenLdr_ZeroAddress_ShouldNotHaveValue()
    {
        // Arrange
        using var clrRuntime = new MockMemory()
           .AddPointer(MovzOffset, 0)
           .ToMockClrRuntime();
        var accumulator = CreateValueAccumulator(clrRuntime);
        var rawInstructions = new[]
        {
            MOVZ(X0, MovzOffset),                             // movz x0, #0x1000
            LDR(X1,  new Arm64ImmediateMemoryAccessor(X0,0)), // ldr x1, [x0]
        };
        var instructions = rawInstructions.ToArm64Instructions();
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeFalse(); // When LDR load zero. Accumulated state is expected to be resetted.
    }

    [Fact]
    public void MovzThenLdr_UnreadableAddress_ShouldNotHaveValue()
    {
        // Arrange
        using var clrRuntime = new MockMemory()
           .AddFalsePointer(MovzOffset)
           .ToMockClrRuntime();
        var accumulator = CreateValueAccumulator(clrRuntime);
        var rawInstructions = new[]
        {
            MOVZ(X0, MovzOffset),                            // movz x0, #0x1000
            LDR(X1, new Arm64ImmediateMemoryAccessor(X0,0)), // ldr x1, [x0]
        };
        var instructions = rawInstructions.ToArm64Instructions();
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]); // No readable pointer is registered, so ReadPointer fails.

        // Assert
        accumulator.HasValue.Should().BeFalse(); // Unreadable address drops tracking instead of aborting disassembly.
        accumulator.Value.Should().Be(0UL);
    }
}

