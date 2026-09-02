// TODO: Remove #if directive when migrated to xunit.v3 or migrated to AsmArm64 based implementation.
#if NET
using AsmArm64;
using AwesomeAssertions;
using AwesomeAssertions.Equivalency;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Disassemblers;
using Gee.External.Capstone;
using static AsmArm64.Arm64RegisterX;
using Arm64Instruction = Gee.External.Capstone.Arm64.Arm64Instruction;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public partial class Arm64DisassemblerTests
{
    // Check Arm64Instruction equivalency by ToString output.
    private static EquivalencyOptions<Arm64Asm> ConfigureCustomEquivalency(EquivalencyOptions<Arm64Asm> options)
        => options
            .Using<Arm64Instruction>(ctx => ctx.Subject.ToString().Should().Be(ctx.Expectation.ToString()))
            .When(info => info.Path == "Instruction");

    [Fact]
    public void Decode()
    {
        // Arrange
        var rawInstructions = new[]
        {
            Arm64InstructionFactory.MOVZ(X1, 0x1234),             // movz x0, #0x1234
            Arm64InstructionFactory.MOVK(X1, 0x5678, amount: 16), // movk x0, #0x5678, lsl #16
            Arm64InstructionFactory.BR(X1),                       // br x1
        };
        PrintInstructions(rawInstructions);

        byte[] bytes = rawInstructions.ToLittleEndianBytes();

        var clrRuntime = CreateMockClrRuntime(
            [
                Arm64InstructionFactory.DMB(Arm64BarrierOperationLimitKind.ISHLD), // dmb ishld
                Arm64InstructionFactory.LDR(X10, 0x10000), // ldr x11, #0x10000
                Arm64InstructionFactory.LDR(X12, 0x20000), // ldr x12, #0x20000
                Arm64InstructionFactory.BR(X10),           // br x11
            ],
            address =>
            {
                return Address2; // Called by TryResolvePrecode
            }
        );
        clrRuntime.GetJitHelperFunctionNameFunc = _ => null;
        clrRuntime.GetMethodByInstructionPointerFunc = _ => DummyTargetMethod;

        var state = new State(clrRuntime, DummyTargetFrameworkVersion);

        ulong baseAddress = DummyBaseAddress;
        DisassemblySyntax syntax = DisassemblySyntax.Masm;

        // Act
        var helper = new Arm64DisassemblerHelper();
        var results = helper.Decode(bytes, baseAddress, state, depth: 0, DummyCurrentMethod, syntax);

        // Assert
        results.Length.Should().Be(rawInstructions.Length);

        // movz x1, #0x1000
        results[0].Should().BeEquivalentTo(new Arm64Asm
        {
            InstructionPointer = baseAddress,
            Instruction = rawInstructions[0].ToCapstoneArm64Instruction(baseAddress, 0),
            InstructionLength = 4,
            DisassembleSyntax = DisassembleSyntax.Masm,
            ReferencedAddress = null,
            IsReferencedAddressIndirect = false,
        }, ConfigureCustomEquivalency);

        // movk x1, #0x1
        results[1].Should().BeEquivalentTo(new Arm64Asm
        {
            InstructionPointer = baseAddress + 4,
            Instruction = rawInstructions[1].ToCapstoneArm64Instruction(baseAddress, 1),
            InstructionLength = 4,
            DisassembleSyntax = DisassembleSyntax.Masm,
            ReferencedAddress = null,
            IsReferencedAddressIndirect = false,
        }, ConfigureCustomEquivalency);

        // br x1
        results[2].Should().BeEquivalentTo(new Arm64Asm
        {
            InstructionPointer = baseAddress + 8,
            Instruction = rawInstructions[2].ToCapstoneArm64Instruction(baseAddress, 2),
            InstructionLength = 4,
            DisassembleSyntax = DisassembleSyntax.Masm,
            ReferencedAddress = Address2,
            IsReferencedAddressIndirect = true,
        }, ConfigureCustomEquivalency);

        state.HandledMethods.Should().BeEmpty(); // HandledMethods is added when disassembled. It's not set on decode timing.

        state.AddressToNameMapping.Should().BeEquivalentTo(new Dictionary<ulong, string>()
        {
            [Address2] = DummyTargetMethod.MethodName,
        });
    }
}
#endif
