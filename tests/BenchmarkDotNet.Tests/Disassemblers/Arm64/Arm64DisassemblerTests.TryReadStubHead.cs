#if NET8_0_OR_GREATER
using AsmArm64;
using AwesomeAssertions;
using Arm64RegisterX = AsmArm64.Arm64RegisterX;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public partial class Arm64DisassemblerTests
{
    [Fact]
    public void TryReadStubHead()
    {
        // Arrange
        var rawInstructions = new[]
        {
            Arm64InstructionFactory.DMB(Arm64BarrierOperationLimitKind.ISHLD),  // dmb ishld
            Arm64InstructionFactory.LDR(Arm64RegisterX.X10, 0x100),             // ldr x10 0x100
            Arm64InstructionFactory.LDR(Arm64RegisterX.X12, 0x200),             // ldr x12 0x200
            Arm64InstructionFactory.BR(Arm64RegisterX.X10),                     // br x10
        };
        PrintInstructions(rawInstructions);

        var dataReader = CreateMockDataReader(rawInstructions);

        ulong address = 0x100000;

        // Act
        var result = Arm64DisassemblerHelper.TryReadStubHead(
            dataReader,
            address,
            out ulong parseBase,
            out uint instr0,
            out uint instr1,
            out uint instr2);

        // Assert
        result.Should().BeTrue();
        parseBase.Should().Be(address + 4);     // Skip `dmb ishld` instruction
        instr0.Should().Be(rawInstructions[1]); // ldr x10 0x100
        instr1.Should().Be(rawInstructions[2]); // ldr x12 0x200
        instr2.Should().Be(rawInstructions[3]); // br x10
    }

    [Fact]
    public void TryReadStubHead_PreDotNet10()
    {
        // Arrange
        var rawInstructions = new[]
        {
            Arm64InstructionFactory.LDR(Arm64RegisterX.X10, 0x100), // ldr x10 0x100
            Arm64InstructionFactory.LDR(Arm64RegisterX.X12, 0x200), // ldr x12 0x200
            Arm64InstructionFactory.BR(Arm64RegisterX.X10),         // br x10
        };
        PrintInstructions(rawInstructions);

        var dataReader = CreateMockDataReader(rawInstructions);
        ulong address = 0x10000;

        // Act
        var result = Arm64DisassemblerHelper.TryReadStubHead(
            dataReader,
            address,
            out ulong parseBase,
            out uint instr0,
            out uint instr1,
            out uint instr2);

        // Assert
        result.Should().BeTrue();
        parseBase.Should().Be(address);
        instr0.Should().Be(rawInstructions[0]); // ldr x10 0x100
        instr1.Should().Be(rawInstructions[1]); // ldr x12 0x200
        instr2.Should().Be(rawInstructions[2]); // br x10
    }

    [Fact]
    public void TryReadStubHead_InsufficientInstructions_ShouldReturnFalse()
    {
        // Arrange
        var rawInstructions = new[]
        {
            Arm64InstructionFactory.DMB(Arm64BarrierOperationLimitKind.ISHLD), // dmb ishld
            Arm64InstructionFactory.LDR(Arm64RegisterX.X10, 0x100),            // ldr x10 0x100
            Arm64InstructionFactory.LDR(Arm64RegisterX.X12, 0x200),            // ldr x12 0x200
            // Arm64InstructionFactory.BR(Arm64RegisterX.X10),                 // br x10
        };
        PrintInstructions(rawInstructions);

        var dataReader = CreateMockDataReader(rawInstructions);
        ulong address = 0;

        // Act
        var result = Arm64DisassemblerHelper.TryReadStubHead(
            dataReader,
            address,
            out ulong parseBase,
            out uint instr0,
            out uint instr1,
            out uint instr2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TryReadStubHead_PreDotNet10_InsufficientInstructions_ShouldReturnFalse()
    {
        // Arrange
        var rawInstructions = new[]
        {
            Arm64InstructionFactory.LDR(Arm64RegisterX.X10, 0x100), // ldr x10 0x100
            Arm64InstructionFactory.LDR(Arm64RegisterX.X12, 0x200), // ldr x12 0x200
            // Arm64InstructionFactory.BR(Arm64RegisterX.X10),      // br x10
        };
        PrintInstructions(rawInstructions);

        var dataReader = CreateMockDataReader(rawInstructions);
        ulong address = 0;

        // Act
        var result = Arm64DisassemblerHelper.TryReadStubHead(
            dataReader,
            address,
            out ulong parseBase,
            out uint instr0,
            out uint instr1,
            out uint instr2);

        // Assert
        result.Should().BeFalse();
    }
}
#endif