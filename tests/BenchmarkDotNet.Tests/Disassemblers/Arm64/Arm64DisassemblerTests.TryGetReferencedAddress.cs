using AsmArm64;
using AwesomeAssertions;
using BenchmarkDotNet.Disassemblers;
using Microsoft.Diagnostics.Runtime.Interfaces;
using static AsmArm64.Arm64RegisterX;
using static AsmArm64.Arm64InstructionFactory;
using Arm64Disassembler = BenchmarkDotNet.Disassemblers.Arm64Disassembler;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public partial class Arm64DisassemblerTests
{
    private Arm64RegisterValueAccumulator CreateValueAccumulator(
        IClrRuntime clrRuntime,
        ulong initialValue = 0,
        Arm64RegisterAny trackedRegister = default,
        Arm64RegisterValueAccumulator.State state = Arm64RegisterValueAccumulator.State.ExpectingMovk)
    {
        var valueAccumulator = new Arm64RegisterValueAccumulator();
        valueAccumulator.Init(clrRuntime);
        valueAccumulator.Reset(state: state, value: initialValue, register: trackedRegister);

        if (initialValue != 0)
        {
            valueAccumulator.HasValue.Should().BeTrue();
            valueAccumulator.Value.Should().Be(initialValue);
        }
        valueAccumulator.TrackedRegister.Should().Be(trackedRegister);

        return valueAccumulator;
    }

    [Fact]
    public void TryGetReferencedAddress_With_BR()
    {
        // Arrange
        using var clrRuntime = CreateMockClrRuntime();
        var valueAccumulator = CreateValueAccumulator(clrRuntime, trackedRegister: X0, initialValue: 0x100);
        var instruction = BR(X0).ToArm64Instruction();

        // Act
        var result = Arm64Disassembler.TryGetReferencedAddress(
            instruction,
            DummyBaseAddress,
            valueAccumulator,
            pointerSize: 0, // This parameter is not used.
            out var referencedAddress,
            out var isReferencedAddressIndirect);

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
        var valueAccumulator = CreateValueAccumulator(clrRuntime, trackedRegister: X0, initialValue: 0x100);
        var instruction = BLR(X0).ToArm64Instruction();

        // Act
        var result = Arm64Disassembler.TryGetReferencedAddress(
            instruction,
            DummyBaseAddress,
            valueAccumulator,
            pointerSize: 0, // This parameter is not used.
            out var referencedAddress,
            out var isReferencedAddressIndirect);

        // Assert
        result.Should().BeTrue();
        referencedAddress.Should().Be(0x100);
        isReferencedAddressIndirect.Should().BeTrue();
    }

    [Fact]
    public void TryGetReferencedAddress_WithAdrpThenBr_ResolvesPageAddress()
    {
        // Arrange
        using var clrRuntime = CreateMockClrRuntime();
        var accumulator = new Arm64RegisterValueAccumulator();
        accumulator.Init(clrRuntime);
        accumulator.Feed(ADRP(X0, 0x2000).ToArm64Instruction(), DummyBaseAddress); // adrp x0, #0x2000
        var instruction = BR(X0).ToArm64Instruction();

        // Act
        var result = Arm64Disassembler.TryGetReferencedAddress(
            instruction,
            DummyBaseAddress,
            accumulator,
            pointerSize: 0, // This parameter is not used.
            out var referencedAddress,
            out var isReferencedAddressIndirect);

        // Assert
        result.Should().BeTrue();
        referencedAddress.Should().Be(DummyBaseAddress + 0x2000); // There is no ADD between ADRP and BR, so the page address itself must resolve.
        isReferencedAddressIndirect.Should().BeTrue();
    }

    [Fact]
    public void TryGetReferencedAddress_WithAdrpAtUnalignedAddress_ResolvesPageAlignedAddress()
    {
        // Arrange
        const ulong AdrpAddress = DummyBaseAddress + 0x1234; // Not page aligned.
        using var clrRuntime = CreateMockClrRuntime();
        var accumulator = new Arm64RegisterValueAccumulator();
        accumulator.Init(clrRuntime);
        accumulator.Feed(ADRP(X0, 0x2000).ToArm64Instruction(), AdrpAddress); // adrp x0, #0x2000
        var instruction = BR(X0).ToArm64Instruction();

        // Act
        var result = Arm64Disassembler.TryGetReferencedAddress(
            instruction,
            DummyBaseAddress,
            accumulator,
            pointerSize: 0, // This parameter is not used.
            out var referencedAddress,
            out var isReferencedAddressIndirect);

        // Assert
        result.Should().BeTrue();
        referencedAddress.Should().Be(DummyBaseAddress + 0x3000); // ADRP is relative to the page containing the instruction.
        isReferencedAddressIndirect.Should().BeTrue();
    }

    [Fact]
    public void TryGetReferencedAddress_WithAdrpNegativeOffset_ResolvesPageAddress()
    {
        // Arrange
        using var clrRuntime = CreateMockClrRuntime();
        var accumulator = new Arm64RegisterValueAccumulator();
        accumulator.Init(clrRuntime);
        accumulator.Feed(ADRP(X0, -0x1000).ToArm64Instruction(), DummyBaseAddress + 0x1234); // adrp x0, #-0x1000
        var instruction = BR(X0).ToArm64Instruction();

        // Act
        var result = Arm64Disassembler.TryGetReferencedAddress(
            instruction,
            DummyBaseAddress,
            accumulator,
            pointerSize: 0, // This parameter is not used.
            out var referencedAddress,
            out var isReferencedAddressIndirect);

        // Assert
        result.Should().BeTrue();
        referencedAddress.Should().Be(DummyBaseAddress); // (DummyBaseAddress + 0x1000) - 0x1000
        isReferencedAddressIndirect.Should().BeTrue();
    }

    [Fact]
    public void TryGetReferencedAddress_With_BranchRelative()
    {
        // Arrange
        using var clrRuntime = CreateMockClrRuntime();
        var valueAccumulator = CreateValueAccumulator(clrRuntime, trackedRegister: X0);
        var rawInstruction = Arm64InstructionFactory.B(0x200);
        var instruction = Arm64Instruction.Decode(rawInstruction);

        // Act
        var result = Arm64Disassembler.TryGetReferencedAddress(
            instruction,
            DummyBaseAddress,
            valueAccumulator,
            pointerSize: 0, // This parameter is not used.
            out var referencedAddress,
            out var isReferencedAddressIndirect);

        // Assert
        result.Should().BeTrue();
        referencedAddress.Should().Be(DummyBaseAddress + 0x200);
        isReferencedAddressIndirect.Should().BeFalse();
    }

    [Fact]
    public void TryGetReferencedAddress_With_BranchRelative_Minus()
    {
        // Arrange
        using var clrRuntime = CreateMockClrRuntime();
        var valueAccumulator = CreateValueAccumulator(clrRuntime, trackedRegister: X0);
        var instruction = B(-0x200).ToArm64Instruction();

        // Act
        var result = Arm64Disassembler.TryGetReferencedAddress(
            instruction,
            DummyBaseAddress,
            valueAccumulator,
            pointerSize: 0, // This parameter is not used.
            out var referencedAddress,
            out var isReferencedAddressIndirect);

        // Assert
        result.Should().BeTrue();
        referencedAddress.Should().Be(DummyBaseAddress - 0x200);
        isReferencedAddressIndirect.Should().BeFalse();
    }

    [Fact]
    public void TryGetReferencedAddress_DontMatchCondition_ShouldReturnFalse()
    {
        // Arrange
        using var clrRuntime = CreateMockClrRuntime();
        var valueAccumulator = CreateValueAccumulator(clrRuntime, trackedRegister: X0, initialValue: 0x0);
        var instruction = RET().ToArm64Instruction();  // `ret` instruction is not BranchRelative group

        // Act
        var result = Arm64Disassembler.TryGetReferencedAddress(
            instruction,
            DummyBaseAddress,
            valueAccumulator,
            pointerSize: 0, // This parameter is not used.
            out var referencedAddress,
            out var isReferencedAddressIndirect);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TryGetReferencedAddress_With_BR_DifferentRegister_ShouldReturnFalse()
    {
        // Arrange
        using var clrRuntime = CreateMockClrRuntime();
        var valueAccumulator = CreateValueAccumulator(clrRuntime, trackedRegister: X0, initialValue: 0x100);
        var instruction = BR(X1).ToArm64Instruction();

        // Act
        var result = Arm64Disassembler.TryGetReferencedAddress(
            instruction,
            DummyBaseAddress,
            valueAccumulator,
            pointerSize: 0, // This parameter is not used.
            out var referencedAddress,
            out var isReferencedAddressIndirect);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TryGetReferencedAddress_With_TBZ_ResolvesLabelOffset()
    {
        // Arrange
        using var clrRuntime = CreateMockClrRuntime();
        var valueAccumulator = CreateValueAccumulator(clrRuntime, trackedRegister: X0);
        var rawInstruction = Arm64InstructionFactory.TBZ(X0, 3, new Arm64LabelOffset(0x200)); // tbz x0, #3, #0x200
        var instruction = Arm64Instruction.Decode(rawInstruction);

        // Act
        var result = Arm64Disassembler.TryGetReferencedAddress(
            instruction,
            DummyBaseAddress,
            valueAccumulator,
            pointerSize: 0, // This parameter is not used.
            out var referencedAddress,
            out var isReferencedAddressIndirect);

        // Assert
        result.Should().BeTrue();
        // The bit position imm value (3) is ignored.
        referencedAddress.Should().Be(DummyBaseAddress + 0x200);
        isReferencedAddressIndirect.Should().BeFalse();
    }

    [Fact]
    public void TryGetReferencedAddress_With_TBNZ_ResolvesLabelOffset()
    {
        // Arrange
        using var clrRuntime = CreateMockClrRuntime();
        var valueAccumulator = CreateValueAccumulator(clrRuntime, trackedRegister: X0);
        var instruction = TBNZ(X0, 3, new Arm64LabelOffset(0x200)).ToArm64Instruction();

        // Act
        var result = Arm64Disassembler.TryGetReferencedAddress(
            instruction,
            DummyBaseAddress,
            valueAccumulator,
            pointerSize: 0, // This parameter is not used.
            out var referencedAddress,
            out var isReferencedAddressIndirect);

        // Assert
        // The bit position (3) must not be mistaken for the branch target.
        result.Should().BeTrue();
        referencedAddress.Should().Be(DummyBaseAddress + 0x200);
        isReferencedAddressIndirect.Should().BeFalse();
    }
}
