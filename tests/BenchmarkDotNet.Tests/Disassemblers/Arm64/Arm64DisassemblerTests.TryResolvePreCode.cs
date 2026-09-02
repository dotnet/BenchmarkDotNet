#if NET8_0_OR_GREATER
using AsmArm64;
using AwesomeAssertions;
using static AsmArm64.Arm64RegisterX;
using static AsmArm64.Arm64RegisterW;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public partial class Arm64DisassemblerTests
{
    [Fact]
    public void TryResolvePrecode_StubPrecode()
    {
        // Arrange
        const int JumpAddress = 0x10000;
        const int MdOffset = 0x20000; // This value is not used for test.

        var rawInstructions = new[]
        {
            Arm64InstructionFactory.DMB(Arm64BarrierOperationLimitKind.ISHLD), // dmb ishld
            Arm64InstructionFactory.LDR(X10, JumpAddress), // ldr x11, #0x10000
            Arm64InstructionFactory.LDR(X12, MdOffset),    // ldr x12, #0x20000
            Arm64InstructionFactory.BR(X10),               // br x11
        };
        PrintInstructions(rawInstructions);

        var dataReader = CreateMockDataReader(rawInstructions, getPointer: (ulong address) =>
        {
            // Validate address value
            var parseBase = DummyBaseAddress + 4; // Skip `dmb ishld` instruction
            ulong mdSlot = parseBase + 4 + MdOffset;
            address.Should().Be(mdSlot);

            // Return resolved address (It must be greater than or equals to 65536)
            return 0x30000;
        });

        ulong address = DummyBaseAddress;

        // Act
        var result = Arm64DisassemblerHelper.TryResolvePrecode(dataReader, ref address, out var isPrestubMd);

        // Assert
        result.Should().BeTrue();
        address.Should().Be(0x30000);
        isPrestubMd.Should().BeTrue();
    }

    [Fact]
    public void TryResolvePrecode_FixupPrecode()
    {
        // Arrange
        var rawInstructions = new uint[]
        {
            Arm64InstructionFactory.DMB(Arm64BarrierOperationLimitKind.ISHLD), // dmb ishld
            Arm64InstructionFactory.LDR(X11, 0x10000), // ldr x11, #0x10000
            Arm64InstructionFactory.BR(X11),           // br x11
            Arm64InstructionFactory.LDR(X12, 0x20000), // ldr x12, #0x20000
        };

        PrintInstructions(rawInstructions);

        var dataReader = CreateMockDataReader(rawInstructions, getPointer: (ulong address) =>
        {
            const int mdLdrOffset = 12;
            address.Should().Be(DummyBaseAddress + mdLdrOffset + 0x20000);
            return 0x10000; // Must be greater than or equals to 65536
        });

        ulong address = DummyBaseAddress;

        // Act
        var result = Arm64DisassemblerHelper.TryResolvePrecode(dataReader, ref address, out var isPrestubMd);

        // Assert
        result.Should().BeTrue();
        address.Should().Be(0x10000);
        isPrestubMd.Should().BeTrue();
    }

    [Fact]
    public void TryResolvePrecode_FixupPrecodeCode_Fixup()
    {
        // Arrange
        const int MdOffset = 0x10000;
        const int JumpAddress = 0x20000; // This value is not used.
        var rawInstructions = new uint[]
        {
            Arm64InstructionFactory.DMB(Arm64BarrierOperationLimitKind.ISHLD), // dmb ishld
            Arm64InstructionFactory.LDR(X12, MdOffset),    // ldr x12, #0x10000
            Arm64InstructionFactory.LDR(X11, JumpAddress), // ldr x11, #0x20000
            Arm64InstructionFactory.BR(X11),               // br x11
        };
        PrintInstructions(rawInstructions);

        var dataReader = CreateMockDataReader(rawInstructions, getPointer: (ulong address) =>
        {
            // Validate address value
            ulong parseBase = DummyBaseAddress + 4; // Skip `dmb ishld` instruction
            ulong mdAddress = unchecked(parseBase + MdOffset);
            address.Should().Be(mdAddress);

            // Return resolved address (It must be greater than or equals to 65536)
            return 0x30000;
        });

        // Act
        ulong address = DummyBaseAddress;
        var result = Arm64DisassemblerHelper.TryResolvePrecode(dataReader, ref address, out var isPrestubMd);

        // Assert
        result.Should().BeTrue();
        address.Should().Be(0x30000);
        isPrestubMd.Should().BeTrue();
    }

    [Fact]
    public void TryResolvePrecode_CallCountingStub()
    {
        // Arrange
        const int RemainingCallCount = 0x10000;
        var rawInstructions = new uint[]
        {
            Arm64InstructionFactory.DMB(Arm64BarrierOperationLimitKind.ISHLD),          // dmb ishld
            Arm64InstructionFactory.LDR(X9, RemainingCallCount),                        // ldr x9, #0x10000
            Arm64InstructionFactory.LDRH(W10, new Arm64ImmediateMemoryAccessor(X9, 0)), // ldrh w10, [x9]
            Arm64InstructionFactory.SUBS(W10, W10, 1),                                  // subs w10, w10, #1
        };
        PrintInstructions(rawInstructions);

        var dataReader = CreateMockDataReader(rawInstructions, getPointer: (ulong address) =>
        {
            // Validate address value
            ulong parseBase = DummyBaseAddress + 4; // Skip `dmb ishld` instruction
            ulong countSlot = unchecked(parseBase + RemainingCallCount);
            address.Should().Be(countSlot + 8);

            // Return resolved address (It must be greater than or equals to 65536)
            return 0x20000;
        });

        // Act
        ulong address = DummyBaseAddress;
        var result = Arm64DisassemblerHelper.TryResolvePrecode(dataReader, ref address, out var isPrestubMd);

        // Assert
        result.Should().BeTrue();
        address.Should().Be(0x20000);
        isPrestubMd.Should().BeFalse();
    }
}
#endif