// TODO: Remove #if directive when migrated to xunit.v3 or migrated to AsmArm64 based implementation.
#if NET
using AsmArm64;
using AwesomeAssertions;
using BenchmarkDotNet.Disassemblers;
using static AsmArm64.Arm64RegisterX;
using static AsmArm64.Arm64RegisterW;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public partial class Arm64DisassemblerTests
{
    [Fact]
    public void TryFollowJumpTrampoline_B()
    {
        // Arrange
        var rawInstructions = new[]
        {
            Arm64InstructionFactory.B(0x10000), // b 0x10000
        };
        PrintInstructions(rawInstructions);

        var clrRuntime = CreateMockClrRuntime(rawInstructions);
        var state = new State(clrRuntime, DummyTargetFrameworkVersion);
        ulong baseAddress = DummyBaseAddress;

        // Act
        var helper = new Arm64DisassemblerHelper();
        var result = helper.TryFollowJumpTrampoline(state, baseAddress, out var target);

        // Assert
        result.Should().BeTrue();
        target.Should().Be(baseAddress + 0x10000);
    }

    [Fact]
    public void TryFollowJumpTrampoline_B_MinusOffset()
    {
        // Arrange
        var rawInstructions = new[]
        {
            Arm64InstructionFactory.B(-0x10000), // b 0x10000
        };

        PrintInstructions(rawInstructions);

        var clrRuntime = CreateMockClrRuntime(rawInstructions);
        var state = new State(clrRuntime, DummyTargetFrameworkVersion);
        ulong baseAddress = DummyBaseAddress;

        // Act
        var helper = new Arm64DisassemblerHelper();
        var result = helper.TryFollowJumpTrampoline(state, baseAddress, out var target);

        // Assert
        result.Should().BeTrue();
        target.Should().Be(baseAddress - 0x10000);
    }

    [Fact]
    public void TryFollowJumpTrampoline_StubPrecode()
    {
        // Arrange
        var rawInstructions = new[]
        {
            Arm64InstructionFactory.DMB(Arm64BarrierOperationLimitKind.ISHLD), // dmb ishld
            Arm64InstructionFactory.LDR(X10, 0x10000), // ldr x11, #0x10000
            Arm64InstructionFactory.LDR(X12, 0x20000), // ldr x12, #0x20000
            Arm64InstructionFactory.BR(X10),           // br x11
        };
        PrintInstructions(rawInstructions);

        var clrRuntime = CreateMockClrRuntime(rawInstructions, address =>
        {
            return ExpectedResultAddress;
        });
        var state = new State(clrRuntime, DummyTargetFrameworkVersion);
        ulong baseAddress = DummyBaseAddress;

        // Act
        var helper = new Arm64DisassemblerHelper();
        var result = helper.TryFollowJumpTrampoline(state, baseAddress, out var target);

        // Assert
        result.Should().BeTrue();
        target.Should().Be(ExpectedResultAddress);
    }

    [Fact]
    public void TryFollowJumpTrampoline_FixupPrecode()
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

        var clrRuntime = CreateMockClrRuntime(rawInstructions, address =>
        {
            return ExpectedResultAddress;
        });
        var state = new State(clrRuntime, DummyTargetFrameworkVersion);
        ulong baseAddress = DummyBaseAddress;

        // Act
        var helper = new Arm64DisassemblerHelper();
        var result = helper.TryFollowJumpTrampoline(state, baseAddress, out var target);

        // Assert
        result.Should().BeTrue();
        target.Should().Be(ExpectedResultAddress);
    }

    [Fact]
    public void TryFollowJumpTrampoline_CallCountingStub()
    {
        // Arrange
        var rawInstructions = new uint[]
        {
            Arm64InstructionFactory.DMB(Arm64BarrierOperationLimitKind.ISHLD),          // dmb ishld
            Arm64InstructionFactory.LDR(X9, 0x10000),                                   // ldr x11, #0x10000
            Arm64InstructionFactory.LDRH(W10, new Arm64ImmediateMemoryAccessor(X9, 0)), // ldrh w10, [x9]
            Arm64InstructionFactory.SUBS(W10, W10, 1),                                  // subs w10, w10, #1
        };
        PrintInstructions(rawInstructions);

        var clrRuntime = CreateMockClrRuntime(rawInstructions, address =>
        {
            return ExpectedResultAddress;
        });
        var state = new State(clrRuntime, DummyTargetFrameworkVersion);
        ulong baseAddress = DummyBaseAddress;

        // Act
        var helper = new Arm64DisassemblerHelper();
        var result = helper.TryFollowJumpTrampoline(state, baseAddress, out var target);

        // Assert
        result.Should().BeTrue();
        target.Should().Be(ExpectedResultAddress);
    }

    // TryFollowJumTrampoline seems not support FixupPrecode with pre-backpatch form.
    [Fact]
    public void TryFollowJumpTrampoline_FixupPrecodeCode_Fixup_ShouldReturnFalse()
    {
        // Arrange
        var rawInstructions = new uint[]
        {
            Arm64InstructionFactory.DMB(Arm64BarrierOperationLimitKind.ISHLD), // dmb ishld
            Arm64InstructionFactory.LDR(X12, 0x10000), // ldr x12, #0x10000
            Arm64InstructionFactory.LDR(X11, 0x20000), // ldr x11, #0x20000
            Arm64InstructionFactory.BR(X11),           // br x11
        };
        PrintInstructions(rawInstructions);

        var clrRuntime = CreateMockClrRuntime(rawInstructions, address =>
        {
            return ExpectedResultAddress;
        });
        var state = new State(clrRuntime, DummyTargetFrameworkVersion);
        ulong baseAddress = DummyBaseAddress;

        // Act
        var helper = new Arm64DisassemblerHelper();
        var result = helper.TryFollowJumpTrampoline(state, baseAddress, out var target);

        // Assert
        result.Should().BeFalse();
    }


    [Fact]
    public void TryFollowJumpTrampoline_NonStubHead_ShouldReturnFalse()
    {
        // Arrange
        var rawInstructions = new[]
        {
            // Arm64InstructionFactory.DMB(Arm64BarrierOperationLimitKind.ISHLD),  // dmb ishld
            Arm64InstructionFactory.BL(0x10000), // bl 0x10000
        };
        PrintInstructions(rawInstructions);

        var clrRuntime = CreateMockClrRuntime(rawInstructions, address =>
        {
            return ExpectedResultAddress;
        });
        var state = new State(clrRuntime, DummyTargetFrameworkVersion);
        ulong baseAddress = DummyBaseAddress;

        // Act
        var helper = new Arm64DisassemblerHelper();
        var result = helper.TryFollowJumpTrampoline(state, baseAddress, out var target);

        // Assert
        result.Should().BeFalse();
    }
}
#endif
