using AsmArm64;
using AwesomeAssertions;
using BenchmarkDotNet.Disassemblers;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public partial class Arm64DisassemblerTests
{
    /// <summary>
    /// When GetJitHelperFunctionName returns non-empty name.
    /// It's added to AddressToNameMapping. and no further processing is done.
    /// </summary>
    [Fact]
    public void TryTranslateAddressToName_GetJitHelperFunctionName_ReturnsNonEmptyValue()
    {
        var clrRuntime = CreateMockClrRuntime();
        clrRuntime.GetJitHelperFunctionNameFunc = address =>
        {
            return address switch
            {
                Address1 => DummyTargetMethod.Name,
                _ => throw new ArgumentOutOfRangeException($"Unexpected address specified: 0x{address:X2}"),
            };
        };

        const ulong address = Address1;
        State state = new State(clrRuntime, DummyTargetFrameworkVersion);

        // Act
        var helper = new Arm64DisassemblerHelper();
        helper.TryTranslateAddressToName(address, isAddressPrecodeMD: false, state, depth: 0, DummyMethodNotUsed);

        // Assert
        state.AddressToNameMapping.Should().BeEquivalentTo(new Dictionary<ulong, string>
        {
            [Address1] = DummyTargetMethod.Name!,
        });
        state.HandledMethods.Should().BeEmpty();
        state.Todo.Should().BeEmpty();
    }

    /// <summary>
    /// TryTranslateAddressToName try to resolve indirect address.
    /// When following conditions are met.
    ///  1. Failed to get method from specified address.
    ///  2. Specified address is aligned to PointerSize(8).
    /// </summary>
    [Fact]
    public void TryTranslateAddressToName_ResolveIndirectAddress()
    {
        var clrRuntime = CreateMockClrRuntime([], address =>
        {
            return address switch
            {
                Address1 => Address2,
                _ => throw new ArgumentOutOfRangeException($"Unexpected address specified: 0x{address:X2}"),
            };
        });
        clrRuntime.GetJitHelperFunctionNameFunc = address => null;
        clrRuntime.GetMethodByInstructionPointerFunc = address =>
        {
            return address switch
            {
                Address1 => null,
                Address2 => DummyTargetMethod,
                _ => throw new ArgumentOutOfRangeException($"Unexpected address specified: 0x{address:X2}"),
            };
        };

        const ulong address = Address1;
        State state = new State(clrRuntime, DummyTargetFrameworkVersion);

        // Act
        var helper = new Arm64DisassemblerHelper();
        helper.TryTranslateAddressToName(address, isAddressPrecodeMD: false, state, depth: 0, DummyCurrentMethod);

        // Assert
        state.AddressToNameMapping.Should().BeEquivalentTo(new Dictionary<ulong, string>
        {
            [Address1] = DummyTargetMethod.MethodName,
        });
        state.HandledMethods.Should().BeEmpty();
        state.Todo.Should().BeEquivalentTo(
        [
            new MethodInfo(DummyTargetMethod, depth:1),
        ]);
    }

    /// <summary>
    /// When address is not aligned to PointerSize(8).
    /// Skip to resolve indirect address.
    /// Instead, this test verify GetMethodByHandle/GetTypeByMethodTable code path.
    /// </summary>
    [Fact]
    public void TryTranslateAddressToName_NoAlignedAddress()
    {
        const ulong NonAlignedAddress = 0x10004;
        var clrRuntime = CreateMockClrRuntime();
        clrRuntime.GetJitHelperFunctionNameFunc = address => null;
        clrRuntime.GetMethodByInstructionPointerFunc = address =>
        {
            return address switch
            {
                NonAlignedAddress => null,
                _ => throw new ArgumentOutOfRangeException($"Address: 0x{address:X2}"),
            };
        };
        clrRuntime.GetMethodByHandleFunc = handle => null;
        clrRuntime.GetTypeByMethodTableFunc = address => null;

        State state = new State(clrRuntime, DummyTargetFrameworkVersion);

        // Act
        var helper = new Arm64DisassemblerHelper();
        helper.TryTranslateAddressToName(NonAlignedAddress, isAddressPrecodeMD: false, state, depth: 0, DummyMethodNotUsed);

        // Assert
        state.AddressToNameMapping.Should().BeEmpty();
        state.HandledMethods.Should().BeEmpty();
        state.Todo.Should().BeEmpty();
    }

    /// <summary>
    /// Test GetMethodByInstructionPointer behavior.
    /// When resolved method has same address/signature as current method, no further processing is done.
    /// </summary>
    [Fact]
    public void TryTranslateAddressToName_GetMethodByInstructionPointer_ReturnsSameMethod()
    {
        var clrRuntime = CreateMockClrRuntime([]);
        clrRuntime.GetJitHelperFunctionNameFunc = _ => null;
        clrRuntime.GetMethodByInstructionPointerFunc = address =>
        {
            return address switch
            {
                Address1 => DummyCurrentMethod, // Return same method.
                _ => throw new ArgumentOutOfRangeException($"Address: 0x{address:X2}"),
            };
        };

        State state = new State(clrRuntime, DummyTargetFrameworkVersion);

        // Act
        var helper = new Arm64DisassemblerHelper();
        helper.TryTranslateAddressToName(Address1, isAddressPrecodeMD: false, state, depth: 0, DummyCurrentMethod);

        // Assert
        state.AddressToNameMapping.Should().BeEmpty();
        state.HandledMethods.Should().BeEmpty();
        state.Todo.Should().BeEmpty();
    }

    /// <summary>
    /// Test GetMethodByInstructionPointer behavior.
    /// When resolved method has different address/signature as current method.
    /// It's added AddressToNameMapping/Todo.
    /// </summary>
    [Fact]
    public void TryTranslateAddressToName_GetMethodByInstructionPointer_ReturnsDifferentMethod()
    {
        var clrRuntime = CreateMockClrRuntime();
        clrRuntime.GetJitHelperFunctionNameFunc = _ => null;
        clrRuntime.GetMethodByInstructionPointerFunc = address =>
        {
            return address switch
            {
                Address1 => DummyTargetMethod,
                _ => throw new ArgumentOutOfRangeException($"Address: 0x{address:X2}"),
            };
        };
        State state = new State(clrRuntime, DummyTargetFrameworkVersion);

        // Act
        var helper = new Arm64DisassemblerHelper();
        helper.TryTranslateAddressToName(Address1, isAddressPrecodeMD: false, state, 0, DummyCurrentMethod);

        // Assert
        state.AddressToNameMapping.Should().BeEquivalentTo(new Dictionary<ulong, string>
        {
            [Address1] = DummyTargetMethod.MethodName,
        });
        state.HandledMethods.Should().BeEmpty();
        state.Todo.Should().BeEquivalentTo(
        [
            new MethodInfo(DummyTargetMethod, depth:1),
        ]);
    }

    /// <summary>
    /// When GetMethodByInstructionPointer returns null.
    /// Then it try to resolve method with TryFollowJumpTrampoline.
    /// </summary>
    [Fact]
    public void TryTranslateAddressToName_TryFollowJumpTrampoline_B()
    {
        var clrRuntime = CreateMockClrRuntime(
        [
            Arm64InstructionFactory.B(0x10000), // b 0x10000
        ],
        address =>
        {
            return Address1;
        });

        clrRuntime.GetJitHelperFunctionNameFunc = _ => null;
        clrRuntime.GetMethodByInstructionPointerFunc = address =>
        {
            return address switch
            {
                Address1 => null,
                Address1 + 0x10000 => DummyTargetMethod,
                _ => throw new ArgumentOutOfRangeException($"Address: 0x{address:X2}")
            };
        };

        State state = new State(clrRuntime, DummyTargetFrameworkVersion);

        // Act
        var helper = new Arm64DisassemblerHelper();
        helper.TryTranslateAddressToName(Address1, isAddressPrecodeMD: false, state, 0, DummyCurrentMethod);

        // Assert
        state.AddressToNameMapping.Should().BeEquivalentTo(new Dictionary<ulong, string>
        {
            [Address1] = DummyTargetMethod.MethodName,
        });
        state.HandledMethods.Should().BeEmpty();
        state.Todo.Should().BeEquivalentTo(
        [
            new MethodInfo(DummyTargetMethod, depth:1),
        ]);
    }

    /// <summary>
    /// When GetMethodByInstructionPointer returns null.
    /// Then it try to resolve method with TryFollowJumpTrampoline up to 8 hops.
    /// </summary>
    [Fact]
    public void TryTranslateAddressToName_TryFollowJumpTrampoline_MultiHop()
    {
        var clrRuntime = CreateMockClrRuntime(
        [
            Arm64InstructionFactory.B(0x1000), // b 0x1000
        ],
        address =>
        {
            return Address2;
        });

        clrRuntime.GetJitHelperFunctionNameFunc = _ => null;
        clrRuntime.GetMethodByInstructionPointerFunc = address =>
        {
            switch (address)
            {
                case Address1:
                case Address2:
                    return null; // Return null to test JumpTrampoline

                case Address1 + 0x1000:
                case Address1 + 0x2000:
                case Address1 + 0x3000:
                case Address1 + 0x4000:
                case Address1 + 0x5000:
                case Address1 + 0x6000:
                case Address1 + 0x7000:
                    return null;

                case Address1 + 0x8000:
                    return DummyTargetMethod; // Return method when Hop:8

                default:
                    throw new ArgumentOutOfRangeException($"Address: 0x{address:X2}");
            }
        };

        State state = new State(clrRuntime, DummyTargetFrameworkVersion);

        // Act
        var helper = new Arm64DisassemblerHelper();
        helper.TryTranslateAddressToName(Address1, isAddressPrecodeMD: false, state, 0, DummyCurrentMethod);

        // Assert
        state.AddressToNameMapping.Should().BeEquivalentTo(new Dictionary<ulong, string>
        {
            [Address1] = DummyTargetMethod.MethodName,
        });
        state.HandledMethods.Should().BeEmpty();
        state.Todo.Should().BeEquivalentTo(
        [
            new MethodInfo(DummyTargetMethod, depth:1),
        ]);
    }

    /// <summary>
    /// When TryFollowJumpTrampoline failed to resolve method descriptor,
    /// Try to get method discriptor via GetMethodByHandleFunc.
    /// </summary>
    [Fact]
    public void TryTranslateAddressToName_GetMethodByHandle_WithPreCode()
    {
        var clrRuntime = CreateMockClrRuntime([], _ => 0);
        clrRuntime.GetJitHelperFunctionNameFunc = _ => null;
        clrRuntime.GetMethodByInstructionPointerFunc = address =>
        {
            return address switch
            {
                Address1 => null, // Return null to test GetMethodByHandle;
                _ => throw new ArgumentOutOfRangeException($"Address: 0x{address:X2}"),
            };
        };
        clrRuntime.GetMethodByHandleFunc = handle =>
        {
            return DummyTargetMethod;
        };

        State state = new State(clrRuntime, DummyTargetFrameworkVersion);

        // Act
        var helper = new Arm64DisassemblerHelper();
        helper.TryTranslateAddressToName(Address1, isAddressPrecodeMD: true, state, 0, DummyCurrentMethod);

        // Assert
        state.AddressToNameMapping.Should().BeEquivalentTo(new Dictionary<ulong, string>
        {
            [Address1] = $"Precode of {DummyTargetMethod.Signature}",
        });
        state.HandledMethods.Should().BeEmpty();
        state.Todo.Should().BeEquivalentTo(
        [
            new MethodInfo(DummyTargetMethod, depth:1),
        ]);
    }

    /// <summary>
    /// When TryFollowJumpTrampoline failed to resolve method descriptor,
    /// Try to get method discriptor via GetMethodByHandleFunc.
    /// </summary>
    [Fact]
    public void TryTranslateAddressToName_GetMethodByHandle_WithoutPreCode()
    {
        var clrRuntime = CreateMockClrRuntime([], _ => 0);
        clrRuntime.GetJitHelperFunctionNameFunc = _ => "";
        clrRuntime.GetMethodByInstructionPointerFunc = address =>
        {
            return address switch
            {
                Address1 => null, // Return null to test GetMethodByHandle;
                _ => throw new ArgumentOutOfRangeException($"Address: 0x{address:X2}"),
            };
        };
        clrRuntime.GetMethodByHandleFunc = address =>
        {
            return address switch
            {
                Address1 => DummyTargetMethod,
                _ => throw new ArgumentOutOfRangeException($"Address: 0x{address:X2}"),
            };
        };
        State state = new State(clrRuntime, DummyTargetFrameworkVersion);

        // Act
        var helper = new Arm64DisassemblerHelper();
        helper.TryTranslateAddressToName(Address1, isAddressPrecodeMD: false, state, 0, DummyCurrentMethod);

        // Assert
        state.AddressToNameMapping.Should().BeEquivalentTo(new Dictionary<ulong, string>
        {
            [Address1] = $"MD_{DummyTargetMethod.Signature}",
        });
        state.HandledMethods.Should().BeEmpty();
        state.Todo.Should().BeEmpty(); // It's not added when isAddressPrecodeMD:false
    }

    /// <summary>
    /// When GetMethodByHandleFunc failed to resolve method descriptor,
    /// Try to get method discriptor via GetTypeByMethodTable.
    /// </summary>
    [Fact]
    public void TryTranslateAddressToName_GetTypeByMethodTable()
    {
        var clrRuntime = CreateMockClrRuntime([], _ => 0);
        clrRuntime.GetJitHelperFunctionNameFunc = _ => null;
        clrRuntime.GetMethodByInstructionPointerFunc = address =>
        {
            return address switch
            {
                Address1 => null,
                _ => throw new ArgumentOutOfRangeException($"Address: 0x{address:X2}"),
            };
        };
        clrRuntime.GetMethodByHandleFunc = handle => null; // Returns null to test GetTypeByMethodTable
        clrRuntime.GetTypeByMethodTableFunc = address => new MockClrType("DummyType");
        State state = new State(clrRuntime, DummyTargetFrameworkVersion);

        // Act
        var helper = new Arm64DisassemblerHelper();
        helper.TryTranslateAddressToName(Address1, isAddressPrecodeMD: false, state, 0, DummyCurrentMethod);

        // Assert
        state.AddressToNameMapping.Should().BeEquivalentTo(new Dictionary<ulong, string>
        {
            [Address1] = "MT_DummyType",
        });
        state.HandledMethods.Should().BeEmpty();
        state.Todo.Should().BeEmpty();
    }
}