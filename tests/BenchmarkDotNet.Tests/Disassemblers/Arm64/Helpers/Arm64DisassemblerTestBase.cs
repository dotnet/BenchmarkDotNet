using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interfaces;
using System.Buffers.Binary;

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

    protected void PrintInstructions(Gee.External.Capstone.Arm64.Arm64Instruction[] instructions)
    {
        foreach (var instruction in instructions)
        {
            Output.WriteLine(instruction.ToString());
        }
    }

    protected void PrintInstructions(uint[] rawInstructions)
    {
        foreach (var rawInstruction in rawInstructions)
        {
            var instruction = rawInstruction.ToCapstoneArm64Instruction();
            Output.WriteLine(instruction.ToString());
        }
    }

    /// <summary>
    /// Create MockClrRuntime with MockDataReader that returns following data.
    ///   Read: Throw InvalidOperationException.
    ///   ReadPointer: Return address 0 data to skip TryResolvePrecode/TryFollowJumpTrampoline.
    /// </summary>
    protected static MockClrRuntime CreateMockClrRuntime()
    {
        var dataReader = new MockDataReader();
        return new MockClrRuntime(new MockDataTarget(dataReader));
    }
}

file static class ExtensionMethods
{
    public static ReadBytesDelegate ToGetReadBytesDelegate(this uint[] rawInstructions)
    {
        return (address, buffer) =>
        {
            var instructionCountToWrite = Math.Min(rawInstructions.Length, buffer.Length / 4);

            for (int i = 0; i < instructionCountToWrite; i++)
            {
                uint rawInstruction = rawInstructions[i];
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(i * 4), rawInstruction);
            }

            return instructionCountToWrite * 4;
        };
    }

    public static TryReadPointerDelegate ToTryReadPointerDelegate(this Func<ulong, ulong> getPointer)
    {
        return (ulong address, out ulong value) =>
        {
            value = getPointer(address);
            return true;
        };
    }
}
