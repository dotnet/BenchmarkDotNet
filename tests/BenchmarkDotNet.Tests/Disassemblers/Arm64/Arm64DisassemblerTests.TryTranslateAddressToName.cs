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
        using var clrRuntime = CreateMockClrRuntime();
        clrRuntime.JitHelperFunctionNames.Add(Address1, DummyTargetMethod.Name);
        State state = new State(clrRuntime, DummyTargetFrameworkVersion);

        // Act
        var helper = new Arm64DisassemblerHelper();
        helper.TryTranslateAddressToName(Address1, isAddressPrecodeMD: false, state, depth: 0, DummyMethodNotUsed);

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
        using var clrRuntime = new MockMemory()
           .AddJitHelperFunctionName(Address1, null)
           .AddMethodByInstructionPointer(Address1, null)
           .AddPointer(Address1, Address2)
           .AddMethodByInstructionPointer(Address2, DummyTargetMethod)
           .ToMockClrRuntime();
        State state = new State(clrRuntime, DummyTargetFrameworkVersion);

        // Act
        var helper = new Arm64DisassemblerHelper();
        helper.TryTranslateAddressToName(Address1, isAddressPrecodeMD: false, state, depth: 0, DummyCurrentMethod);

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
        const ulong NonAlignedAddress = Address1 + 4;

        using var clrRuntime = new MockMemory()
          .AddJitHelperFunctionName(NonAlignedAddress, null)
          .AddMethodByInstructionPointer(NonAlignedAddress, null)
          .AddPointer(NonAlignedAddress, 0) // Skip TryFollowJumpTrampoline
          .AddMethodByHandle(NonAlignedAddress, null)
          .AddTypeByMethodTable(NonAlignedAddress, null)
          .ToMockClrRuntime();
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
        using var clrRuntime = new MockMemory()
         .AddJitHelperFunctionName(Address1, null)
         .AddMethodByInstructionPointer(Address1, DummyCurrentMethod) // Return same method.
         .ToMockClrRuntime();
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
        using var clrRuntime = new MockMemory()
            .AddJitHelperFunctionName(Address1, null)
            .AddMethodByInstructionPointer(Address1, DummyTargetMethod) // Return different method.
            .ToMockClrRuntime();
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
        using var clrRuntime = new MockMemory()
            .AddInstructions(Address1,
            [
                Arm64InstructionFactory.B(0x10000), // b 0x10000
            ])
            .AddJitHelperFunctionName(Address1, null)
            .AddMethodByInstructionPointer(Address1, null)
            .AddMethodByInstructionPointer(Address1 + 0x10000, DummyTargetMethod)
            .ToMockClrRuntime();
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
        var rawInstructions = new uint[] { Arm64InstructionFactory.B(0x1000) };

        using var clrRuntime = new MockMemory()
           .AddInstructions(Address1, rawInstructions)
           .AddInstructions(Address1 + 0x1000, rawInstructions)
           .AddInstructions(Address1 + 0x2000, rawInstructions)
           .AddInstructions(Address1 + 0x3000, rawInstructions)
           .AddInstructions(Address1 + 0x4000, rawInstructions)
           .AddInstructions(Address1 + 0x5000, rawInstructions)
           .AddInstructions(Address1 + 0x6000, rawInstructions)
           .AddInstructions(Address1 + 0x7000, rawInstructions)
           .AddJitHelperFunctionName(Address1, null)
           .AddMethodByInstructionPointer(Address1, null)
           .AddMethodByInstructionPointer(Address1 + 0x1000, null)
           .AddMethodByInstructionPointer(Address1 + 0x2000, null)
           .AddMethodByInstructionPointer(Address1 + 0x3000, null)
           .AddMethodByInstructionPointer(Address1 + 0x4000, null)
           .AddMethodByInstructionPointer(Address1 + 0x5000, null)
           .AddMethodByInstructionPointer(Address1 + 0x6000, null)
           .AddMethodByInstructionPointer(Address1 + 0x7000, null)
           .AddMethodByInstructionPointer(Address1 + 0x8000, DummyTargetMethod)
           .ToMockClrRuntime();
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
        using var clrRuntime = new MockMemory()
           .AddJitHelperFunctionName(Address1, null)
           .AddPointer(Address1, 0)                       // Return 0 to skip GetMethodByInstructionPointer 
           .AddMethodByInstructionPointer(Address1, null) // Return null to test GetMethodByHandle;
           .AddMethodByHandle(Address1, DummyTargetMethod)
           .ToMockClrRuntime();
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
        using var clrRuntime = new MockMemory()
          .AddJitHelperFunctionName(Address1, null)
          .AddPointer(Address1, 0)                       // Return 0 to skip GetMethodByInstructionPointer
          .AddMethodByInstructionPointer(Address1, null) // Return null to test GetMethodByHandle;
          .AddMethodByHandle(Address1, DummyTargetMethod)
          .ToMockClrRuntime();
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
        using var clrRuntime = new MockMemory()
         .AddJitHelperFunctionName(Address1, null)
         .AddMethodByInstructionPointer(Address1, null) // Return null to test GetMethodByHandle;
         .AddPointer(Address1, 0)                       // Return 0 to skip TryFollowJumpTrampoline
         .AddMethodByHandle(Address1, null)             // Returns null to test GetTypeByMethodTable
         .AddTypeByMethodTable(Address1, new MockClrType("DummyType"))
         .ToMockClrRuntime();
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