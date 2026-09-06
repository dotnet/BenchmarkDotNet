#if NET8_0_OR_GREATER
using AsmArm64;
using AwesomeAssertions;
using BenchmarkDotNet.Disassemblers;
using static AsmArm64.Arm64RegisterX;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public partial class Arm64DisassemblerTests
{
    private Arm64RegisterValueAccumulator CreateValueAccumulator(ushort initialValue)
    {
        var valueAccumulator = new Arm64RegisterValueAccumulator();
        valueAccumulator.Init(DummyClrRuntime);

        valueAccumulator.Feed(Arm64TestInstructions.Movz(X0, initialValue));

        valueAccumulator.HasValue.Should().BeTrue();
        valueAccumulator.Value.Should().Be(initialValue);

        return valueAccumulator;
    }

    [Fact]
    public void TryGetReferencedAddress_With_BR()
    {
        // Arrange
        var rawInstruction = Arm64InstructionFactory.BR(X0);
        var valueAccumulator = CreateValueAccumulator(0x100);

        // Act
        var result = Arm64DisassemblerHelper.TryGetReferencedAddress(
          rawInstruction.ToCapstoneArm64Instruction(),
           valueAccumulator,
           pointerSize: 0, // Thiss parameter is not used.
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
        var rawInstruction = Arm64InstructionFactory.BLR(X0);
        var valueAccumulator = CreateValueAccumulator(0x100);

        // Act
        var result = Arm64DisassemblerHelper.TryGetReferencedAddress(
           rawInstruction.ToCapstoneArm64Instruction(),
           valueAccumulator,
           pointerSize: 0, // Thiss parameter is not used.
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
        var rawInstruction = Arm64InstructionFactory.B(0x100);
        var valueAccumulator = CreateValueAccumulator(0x100);

        // Act
        var result = Arm64DisassemblerHelper.TryGetReferencedAddress(
           rawInstruction.ToCapstoneArm64Instruction(),
           valueAccumulator,
           pointerSize: 0, // Thiss parameter is not used.
           out ulong referencedAddress,
           out bool isReferencedAddressIndirect);

        // Assert
        result.Should().BeTrue();
        referencedAddress.Should().Be(0x100);
        isReferencedAddressIndirect.Should().BeFalse();
    }

    [Fact]
    public void TryGetReferencedAddress_DontMatchCondition_ShouldReturnFalse()
    {
        // Arrange
        var rawInstruction = Arm64InstructionFactory.RET(); // `ret` instruction is not BranchRelative group
        var valueAccumulator = CreateValueAccumulator(0x100);

        // Act
        var result = Arm64DisassemblerHelper.TryGetReferencedAddress(
           rawInstruction.ToCapstoneArm64Instruction(),
           valueAccumulator,
           pointerSize: 0, // Thiss parameter is not used.
           out ulong referencedAddress,
           out bool isReferencedAddressIndirect);

        // Assert
        result.Should().BeFalse();
    }
}
#endif