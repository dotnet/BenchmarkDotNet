using AsmArm64;
using BenchmarkDotNet.Disassemblers;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public abstract class Arm64DisassemblerTestBase
{
    // On macos(arm64), available virtual address space is 48-bit (or 52-bit) and minimum address must be 4GB (0x1_0000_0000)
    internal const ulong DummyBaseAddress = 0x0000_F000_0000_0000UL;

    internal static readonly Version DummyTargetFrameworkVersion = new Version(10, 0);
    internal static readonly MockClrMethod DummyMethodNotUsed = new(null, 0, null, null);
    internal static readonly MockClrMethod DummyCurrentMethod = new("DummyCurrentMethod", DummyBaseAddress + 0x10000, "DummyCurrentMethodSignature", new MockClrType("DummyCurrentMethodType"));
    internal static readonly MockClrMethod DummyTargetMethod = new("DummyTargetMethod", DummyBaseAddress + 0x20000, "DummyTargetMethodSignature", new MockClrType("DummyTargetMethodType"));

    protected readonly ITestOutputHelper Output;

    public Arm64DisassemblerTestBase(ITestOutputHelper output)
    {
        Output = output;
    }

    protected void PrintInstructions(IEnumerable<Arm64Instruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            var instructionText = Arm64InstructionFormatter.Format(instruction);
            Output.WriteLine(instructionText);
        }
    }

    protected void PrintInstructions(uint[] rawInstructions)
        => PrintInstructions(rawInstructions.Select(Arm64Instruction.Decode));

    /// <summary>
    /// Create MockClrRuntime with MockDataReader that returns following data.
    ///   Read: Return 0 length as written bytes.
    ///   ReadPointer: Return address 0 data to skip TryResolvePrecode/TryFollowJumpTrampoline.
    /// </summary>
    protected static MockClrRuntime CreateMockClrRuntime(bool throwExceptionMode = true)
    {
        var dataReader = new MockDataReader();
        return new MockClrRuntime(new MockDataTarget(dataReader)) { ThrowExceptionMode = throwExceptionMode };
    }
}
