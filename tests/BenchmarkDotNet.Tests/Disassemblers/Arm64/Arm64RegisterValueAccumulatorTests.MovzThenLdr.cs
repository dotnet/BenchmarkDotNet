// TODO: Remove #if directive when migrated to xunit.v3 or migrated to AsmArm64 based implementation.
#if NET
using AsmArm64;
using AwesomeAssertions;
using Gee.External.Capstone.Arm64;
using static AsmArm64.Arm64RegisterX;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

// MOVZ->LDR tests
public partial class Arm64RegisterValueAccumulatorTests
{
    [Fact]
    public void MovzThenLdr_SameRegister_ShouldHaveValue()
    {
        // Arrange
        var expectedAddress = 0x1234UL;
        using var clrRuntime = CreateMockClrRuntime(expectedAddress);
        var accumulator = CreateValueAccumulator(clrRuntime);
        var instructions = new[]
        {
            Arm64TestInstructions.Movz(X0, 0x1000),          // movz x0, #0x1000
            Arm64TestInstructions.Ldr(X0, baseRegister: X0), // ldr x0, [x0]
        };
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeTrue();
        accumulator.RegisterId.Should().Be(Arm64RegisterId.ARM64_REG_X0);
        accumulator.Value.Should().Be((long)expectedAddress); // It should be value that is returned by ReadPointer.
    }

    [Fact]
    public void MovzThenLdr_DifferentRegister_ShouldHaveValue()
    {
        // Arrange
        var expectedAddress = 0x1234UL;
        using var clrRuntime = CreateMockClrRuntime(expectedAddress);
        var accumulator = CreateValueAccumulator(clrRuntime);
        var instructions = new[]
        {
            Arm64TestInstructions.Movz(X0, 0x1000),                          // movz x0, #0x1000
            Arm64TestInstructions.Ldr(X1, baseRegister: X0, immediate: 0x0), // ldr x1, [x0]
        };
        PrintInstructions(instructions);

        // Act
        accumulator.Feed(instructions[0]);
        accumulator.Feed(instructions[1]);

        // Assert
        accumulator.HasValue.Should().BeTrue();
        accumulator.RegisterId.Should().Be(Arm64RegisterId.ARM64_REG_X1); // Value is loaded to X1 register.
        accumulator.Value.Should().Be((long)expectedAddress); // It should be value that is returned by ReadPointer.
    }

    [Fact]
    public void MovzThenLdr_WithDisplacement_ShouldNotHaveValue()
    {
        // Arrange
        var expectedAddress = 0x1234UL;
        using var clrRuntime = CreateMockClrRuntime(expectedAddress);
        var accumulator = CreateValueAccumulator(clrRuntime);
        var instructions = new[]
        {
            Arm64TestInstructions.Movz(X0, 0x1000),                            // movz x0, #0x1000
            Arm64TestInstructions.Ldr(X0, baseRegister: X0, immediate: 0x100), // ldr x0, [x0, #0x100]
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
    public void MovzThenLdr_WithIndexRegister_ShouldNotHaveValue()
    {
        // Arrange
        var expectedAddress = 0x1234UL;
        using var clrRuntime = CreateMockClrRuntime(expectedAddress);
        var accumulator = CreateValueAccumulator(clrRuntime);
        var instructions = new[]
        {
            Arm64TestInstructions.Movz(X0, 0x1000),                             // movz x0, #0x1000
            Arm64TestInstructions.Ldr(X0, baseRegister: X0, indexRegister: X1), // ldr x0, [x0, x1]
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
    public void MovzThenLdrLiteral_ShouldNotHaveValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();
        var instructions = new[]
        {
            Arm64TestInstructions.Movz(X0, 0x1000), // movz x0, #0x1000
            Arm64TestInstructions.Ldr(X0, 0x100),   // ldr x0, #0x100
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
}
#endif
