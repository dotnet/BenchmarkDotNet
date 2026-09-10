#if NET8_0_OR_GREATER
using Argon;
using AsmArm64;
using AwesomeAssertions;
using static AsmArm64.Arm64RegisterW;
using static AsmArm64.Arm64RegisterX;

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
            Arm64InstructionFactory.LDR(X10, JumpAddress), // ldr x10, #0x10000
            Arm64InstructionFactory.LDR(X12, MdOffset),    // ldr x12, #0x20000
            Arm64InstructionFactory.BR(X10),               // br x10
        };
        PrintInstructions(rawInstructions);

        const ulong BaseAddress = DummyBaseAddress;
        const ulong MdSlotAddress = DummyBaseAddress + DmbIshldOffset + 4 + MdOffset;
        using var clrRuntime = new MockMemory()
           .AddInstructions(BaseAddress, rawInstructions)
           .AddPointer(MdSlotAddress, ExpectedResultAddress)
           .ToMockClrRuntime();
        var dataReader = clrRuntime.DataTarget.DataReader;

        // Act
        ulong address = BaseAddress;
        var result = Arm64DisassemblerHelper.TryResolvePrecode(dataReader, ref address, out var isPrestubMd);

        // Assert
        result.Should().BeTrue();
        address.Should().Be(ExpectedResultAddress);
        isPrestubMd.Should().BeTrue();
    }

    [Fact]
    public void TryResolvePrecode_FixupPrecode()
    {
        const int JumpAddress = 0x10000; // This value is not used.
        const int MdOffset = 0x20000;

        // Arrange
        var rawInstructions = new uint[]
        {
            Arm64InstructionFactory.DMB(Arm64BarrierOperationLimitKind.ISHLD), // dmb ishld
            Arm64InstructionFactory.LDR(X11, JumpAddress), // ldr x11, #0x10000
            Arm64InstructionFactory.BR(X11),               // br x11
            Arm64InstructionFactory.LDR(X12, MdOffset),    // ldr x12, #0x20000
        };
        PrintInstructions(rawInstructions);

        const ulong BaseAddress = DummyBaseAddress;
        const ulong MdSlotAddress = DummyBaseAddress + DmbIshldOffset + 8 + MdOffset;
        using var clrRuntime = new MockMemory()
           .AddInstructions(BaseAddress, rawInstructions)
           .AddPointer(MdSlotAddress, ExpectedResultAddress)
           .ToMockClrRuntime();
        var dataReader = clrRuntime.DataTarget.DataReader;

        // Act
        ulong address = BaseAddress;
        var result = Arm64DisassemblerHelper.TryResolvePrecode(dataReader, ref address, out var isPrestubMd);

        // Assert
        result.Should().BeTrue();
        address.Should().Be(ExpectedResultAddress);
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

        const ulong BaseAddress = DummyBaseAddress;
        const ulong MdSlotAddress = DummyBaseAddress + DmbIshldOffset + 0x10000;
        using var clrRuntime = new MockMemory()
           .AddInstructions(BaseAddress, rawInstructions)
           .AddPointer(MdSlotAddress, ExpectedResultAddress)
           .ToMockClrRuntime();
        var dataReader = clrRuntime.DataTarget.DataReader;

        // Act
        ulong address = BaseAddress;
        var result = Arm64DisassemblerHelper.TryResolvePrecode(dataReader, ref address, out var isPrestubMd);

        // Assert
        result.Should().BeTrue();
        address.Should().Be(ExpectedResultAddress);
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

        const ulong BaseAddress = DummyBaseAddress;
        const ulong CountSlotAddress = DummyBaseAddress + DmbIshldOffset + RemainingCallCount + 8;
        using var clrRuntime = new MockMemory()
           .AddInstructions(BaseAddress, rawInstructions)
           .AddPointer(CountSlotAddress, ExpectedResultAddress)
           .ToMockClrRuntime();
        var dataReader = clrRuntime.DataTarget.DataReader;

        // Act
        ulong address = BaseAddress;
        var result = Arm64DisassemblerHelper.TryResolvePrecode(dataReader, ref address, out var isPrestubMd);

        // Assert
        result.Should().BeTrue();
        address.Should().Be(ExpectedResultAddress);
        isPrestubMd.Should().BeFalse();
    }

    [Fact]
    public void TryResolvePrecode_NoStubHead_ShouldBeFalse()
    {
        // Arrange
        var rawInstructions = new[]
        {
            Arm64InstructionFactory.DMB(Arm64BarrierOperationLimitKind.ISHLD), // dmb ishld
            Arm64InstructionFactory.NOP(),
        };
        PrintInstructions(rawInstructions);

        const ulong BaseAddress = DummyBaseAddress;
        using var clrRuntime = new MockMemory()
           .AddInstructions(BaseAddress, rawInstructions)
           .ToMockClrRuntime();
        var dataReader = clrRuntime.DataTarget.DataReader;

        // Act
        ulong address = BaseAddress;
        var result = Arm64DisassemblerHelper.TryResolvePrecode(dataReader, ref address, out var isPrestubMd);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TryResolvePrecode_NoMatch_ShouldBeFalse()
    {
        // Arrange
        var rawInstructions = new[]
        {
            Arm64InstructionFactory.DMB(Arm64BarrierOperationLimitKind.ISHLD), // dmb ishld
            Arm64InstructionFactory.NOP(),
            Arm64InstructionFactory.NOP(),
            Arm64InstructionFactory.NOP(),
        };
        PrintInstructions(rawInstructions);

        const ulong BaseAddress = DummyBaseAddress;
        using var clrRuntime = new MockMemory()
           .AddInstructions(BaseAddress, rawInstructions)
           .ToMockClrRuntime();
        var dataReader = clrRuntime.DataTarget.DataReader;

        // Act
        ulong address = BaseAddress;
        var result = Arm64DisassemblerHelper.TryResolvePrecode(dataReader, ref address, out var isPrestubMd);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TryResolvePrecode_StubPrecode_FailedToRead_ShouldBeFalse()
    {
        const int JumpAddress = 0x10000; // This value is not used.
        const int MdOffset = 0x20000;

        // Arrange
        var rawInstructions = new uint[]
        {
            Arm64InstructionFactory.DMB(Arm64BarrierOperationLimitKind.ISHLD), // dmb ishld
            Arm64InstructionFactory.LDR(X10, JumpAddress), // ldr x10, #0x10000
            Arm64InstructionFactory.LDR(X12, MdOffset),    // ldr x12, #0x20000
            Arm64InstructionFactory.BR(X10),               // br x10
        };
        PrintInstructions(rawInstructions);

        const ulong BaseAddress = DummyBaseAddress;
        const ulong MdSlotAddress = DummyBaseAddress + DmbIshldOffset + 4 + MdOffset;
        using var clrRuntime = new MockMemory()
           .AddInstructions(BaseAddress, rawInstructions)
           .AddFalsePointer(MdSlotAddress)
           .ToMockClrRuntime();
        var dataReader = clrRuntime.DataTarget.DataReader;

        // Act
        ulong address = BaseAddress;
        var result = Arm64DisassemblerHelper.TryResolvePrecode(dataReader, ref address, out var isPrestubMd);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TryResolvePrecode_FixupPrecode_FailedToRead_ShouldBeFalse()
    {
        const int JumpAddress = 0x10000; // This value is not used.
        const int MdOffset = 0x20000;

        // Arrange
        var rawInstructions = new uint[]
        {
            Arm64InstructionFactory.DMB(Arm64BarrierOperationLimitKind.ISHLD), // dmb ishld
            Arm64InstructionFactory.LDR(X11, JumpAddress), // ldr x11, #0x10000
            Arm64InstructionFactory.BR(X11),               // br x11
            Arm64InstructionFactory.LDR(X12, MdOffset),    // ldr x12, #0x20000
        };
        PrintInstructions(rawInstructions);

        const ulong BaseAddress = DummyBaseAddress;
        const ulong MdSlotAddress = DummyBaseAddress + DmbIshldOffset + 8 + MdOffset;
        using var clrRuntime = new MockMemory()
           .AddInstructions(BaseAddress, rawInstructions)
           .AddFalsePointer(MdSlotAddress)
           .ToMockClrRuntime();
        var dataReader = clrRuntime.DataTarget.DataReader;

        // Act
        ulong address = BaseAddress;
        var result = Arm64DisassemblerHelper.TryResolvePrecode(dataReader, ref address, out var isPrestubMd);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TryResolvePrecode_FixupPrecodeCode_Fixup_FailedToRead_ShouldBeFalse()
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

        const ulong BaseAddress = DummyBaseAddress;
        const ulong MdSlotAddress = DummyBaseAddress + DmbIshldOffset + 0x10000;
        using var clrRuntime = new MockMemory()
           .AddInstructions(BaseAddress, rawInstructions)
           .AddFalsePointer(MdSlotAddress)
           .ToMockClrRuntime();
        var dataReader = clrRuntime.DataTarget.DataReader;

        // Act
        ulong address = BaseAddress;
        var result = Arm64DisassemblerHelper.TryResolvePrecode(dataReader, ref address, out var isPrestubMd);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TryResolvePrecode_CallCountingStub_FailedToRead_ShouldBeFalse()
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

        const ulong BaseAddress = DummyBaseAddress;
        const ulong CountSlotAddress = DummyBaseAddress + DmbIshldOffset + RemainingCallCount + 8;
        using var clrRuntime = new MockMemory()
           .AddInstructions(BaseAddress, rawInstructions)
           .AddFalsePointer(CountSlotAddress)
           .ToMockClrRuntime();
        var dataReader = clrRuntime.DataTarget.DataReader;

        // Act
        ulong address = BaseAddress;
        var result = Arm64DisassemblerHelper.TryResolvePrecode(dataReader, ref address, out var isPrestubMd);

        // Assert
        result.Should().BeFalse();
    }
}
#endif