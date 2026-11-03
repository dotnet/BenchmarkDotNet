using AsmArm64;
using AwesomeAssertions;
using static AsmArm64.Arm64InstructionFactory;
using static AsmArm64.Arm64RegisterX;
using BenchmarkDotNet.Disassemblers;


namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public partial class Arm64InstructionExtensionsTests
{
    [Theory]
    [InlineData(Arm64ShiftKind.LSL, 0, 0x1234L, 0x1234L)]
    [InlineData(Arm64ShiftKind.LSL, 4, 0x1234L, 0x12340L)]
    [InlineData(Arm64ShiftKind.LSL, 12, 0x123L, 0x123000L)]
    public void TryShiftImmediate_Lsl_ShouldShiftLeft(Arm64ShiftKind kind, byte amount, long value, long expected)
    {
        // Act
        var result = Arm64InstructionExtensions.TryShiftImmediateValue(value, kind, amount, out var shifted);

        // Assert
        result.Should().BeTrue();
        shifted.Should().Be(expected);
    }

    [Theory]
    [InlineData(Arm64ShiftKind.LSR, 0, 0x1234L, 0x1234L)]
    [InlineData(Arm64ShiftKind.LSR, 4, 0x1234L, 0x123L)]
    [InlineData(Arm64ShiftKind.LSR, 4, -1L, 0x0FFFFFFFFFFFFFFFL)]
    public void TryShiftImmediate_Lsr_ShouldShiftRightLogically(Arm64ShiftKind kind, byte amount, long value, long expected)
    {
        // Act
        var result = Arm64InstructionExtensions.TryShiftImmediateValue(value, kind, amount, out var shifted);

        // Assert
        result.Should().BeTrue();
        shifted.Should().Be(expected);
    }

    [Theory]
    [InlineData(Arm64ShiftKind.ASR, 0, 0x1234L, 0x1234L)]
    [InlineData(Arm64ShiftKind.ASR, 4, 0x1234L, 0x123L)]
    [InlineData(Arm64ShiftKind.ASR, 4, -16L, -1L)]
    public void TryShiftImmediate_Asr_ShouldShiftRightArithmetically(Arm64ShiftKind kind, byte amount, long value, long expected)
    {
        // Act
        var result = Arm64InstructionExtensions.TryShiftImmediateValue(value, kind, amount, out var shifted);

        // Assert
        result.Should().BeTrue();
        shifted.Should().Be(expected);
    }

    [Theory]
    [InlineData(Arm64ShiftKind.ROR, 0, 0x1234L, 0x1234L)]
    [InlineData(Arm64ShiftKind.ROR, 4, 0x1234L, 0x4000000000000123L)]
    public void TryShiftImmediate_Ror_ShouldRotateRight(Arm64ShiftKind kind, byte amount, long value, long expected)
    {
        // Act
        var result = Arm64InstructionExtensions.TryShiftImmediateValue(value, kind, amount, out var shifted);

        // Assert
        result.Should().BeTrue();
        shifted.Should().Be(expected);
    }

    [Theory]
    [InlineData(Arm64ShiftKind.MSL)]
    [InlineData(Arm64ShiftKind.Invalid)]
    public void TryShiftImmediate_UnsupportedKind_ShouldReturnFalse(Arm64ShiftKind kind)
    {
        // Act
        var result = Arm64InstructionExtensions.TryShiftImmediateValue(0x1234, kind, 4, out var shifted);

        // Assert
        result.Should().BeFalse();
        shifted.Should().Be(0);
    }
}
