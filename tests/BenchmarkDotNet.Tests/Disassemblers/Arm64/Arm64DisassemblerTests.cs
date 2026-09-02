namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public partial class Arm64DisassemblerTests : Arm64DisassemblerTestBase
{
    // On macos(arm64), available virtual address space is 48-bit (or 52-bit)
    private const ulong DummyBaseAddress = 0x0000_F000_0000_0000UL;
    private const string DummyTargetFramework = "net10.0";

    public Arm64DisassemblerTests(ITestOutputHelper output)
        : base(output)
    {
    }
}
