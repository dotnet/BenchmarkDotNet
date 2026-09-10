#if NET8_0_OR_GREATER
using AsmArm64;
using AwesomeAssertions;
using BenchmarkDotNet.Disassemblers;
using Microsoft.Diagnostics.Runtime.Interfaces;
using static AsmArm64.Arm64RegisterX;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public partial class Arm64DisassemblerTests
{
    private Arm64RegisterValueAccumulator CreateValueAccumulator(Arm64RegisterX register, ushort initialValue, IClrRuntime clrRuntime)
    {
        var valueAccumulator = new Arm64RegisterValueAccumulator();
        valueAccumulator.Init(clrRuntime);

        // Set initial state by processing Movz instruction.
        valueAccumulator.Feed(Arm64TestInstructions.Movz(register, initialValue));

        valueAccumulator.HasValue.Should().BeTrue();
        valueAccumulator.Value.Should().Be(initialValue);
        // TODO: Add Register validation after migrated to AsmArm64

        return valueAccumulator;
    }

    [Fact]
    public void TryGetReferencedAddress_With_BR()
    {
        // Arrange
        using var clrRuntime = CreateMockClrRuntime();
        var valueAccumulator = CreateValueAccumulator(X0, 0x100, clrRuntime);
        var rawInstruction = Arm64InstructionFactory.BR(X0);

        // Act
        var result = Arm64DisassemblerHelper.TryGetReferencedAddress(
            rawInstruction.ToCapstoneArm64Instruction(),
            valueAccumulator,
            pointerSize: 0, // This parameter is not used.
            out ulong referencedAddress,
            out bool isReferencedAddressIndirect);

        // Assert
        result.Should().BeTrue();
        referencedAddress.Should().Be(0x100);
        isReferencedAddressIndirect.Should().BeTrue();
    }

    [Fact]
    public void TryGetReferencedAddress_With_BLR()
    {
        // Arrange
        using var clrRuntime = CreateMockClrRuntime();
        var valueAccumulator = CreateValueAccumulator(X0, 0x100, clrRuntime);
        var rawInstruction = Arm64InstructionFactory.BLR(X0);

        // Act
        var result = Arm64DisassemblerHelper.TryGetReferencedAddress(
            rawInstruction.ToCapstoneArm64Instruction(),
            valueAccumulator,
            pointerSize: 0, // This parameter is not used.
            out ulong referencedAddress,
            out bool isReferencedAddressIndirect);

        // Assert
        result.Should().BeTrue();
        referencedAddress.Should().Be(0x100);
        isReferencedAddressIndirect.Should().BeTrue();
    }

    [Fact]
    public void TryGetReferencedAddress_With_BranchRelative()
    {
        // Arrange
        using var clrRuntime = CreateMockClrRuntime();
        var valueAccumulator = CreateValueAccumulator(X0, 0x100, clrRuntime);
        var rawInstruction = Arm64InstructionFactory.B(0x200);

        // Act
        var result = Arm64DisassemblerHelper.TryGetReferencedAddress(
            rawInstruction.ToCapstoneArm64Instruction(DummyBaseAddress),
            valueAccumulator,
            pointerSize: 0, // This parameter is not used.
            out ulong referencedAddress,
            out bool isReferencedAddressIndirect);

        // Assert
        result.Should().BeTrue();
        referencedAddress.Should().Be(DummyBaseAddress + 0x200); // Verify absolute address is returned.
        isReferencedAddressIndirect.Should().BeFalse();
    }

    [Fact]
    public void TryGetReferencedAddress_DontMatchCondition_ShouldReturnFalse()
    {
        // Arrange
        using var clrRuntime = CreateMockClrRuntime();
        var valueAccumulator = CreateValueAccumulator(X0, 0x100, clrRuntime);
        var rawInstruction = Arm64InstructionFactory.RET(); // `ret` instruction is not BranchRelative group

        // Act
        var result = Arm64DisassemblerHelper.TryGetReferencedAddress(
            rawInstruction.ToCapstoneArm64Instruction(),
            valueAccumulator,
            pointerSize: 0, // This parameter is not used.
            out ulong referencedAddress,
            out bool isReferencedAddressIndirect);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TryGetReferencedAddress_With_BR_DifferentRegister_ShouldReturnFalse()
    {
        // Arrange
        using var clrRuntime = CreateMockClrRuntime();
        var valueAccumulator = CreateValueAccumulator(X0, 0x100, clrRuntime);
        var rawInstruction = Arm64InstructionFactory.BR(X1);

        // Act
        var result = Arm64DisassemblerHelper.TryGetReferencedAddress(
            rawInstruction.ToCapstoneArm64Instruction(),
            valueAccumulator,
            pointerSize: 0, // This parameter is not used.
            out ulong referencedAddress,
            out bool isReferencedAddressIndirect);

        // Assert
        result.Should().BeFalse();
    }
}
#endif