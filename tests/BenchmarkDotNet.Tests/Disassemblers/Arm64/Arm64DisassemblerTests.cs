namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public partial class Arm64DisassemblerTests : Arm64DisassemblerTestBase
{
    // Use 4GB as base address. it's minimum valid address on macos(arm64) 
    internal const ulong Address1 = 0x1_0000_0000;
    internal const ulong Address2 = 0x2_0000_0000;
    internal const ulong ExpectedResultAddress = 0x3_0000_0000;

    public Arm64DisassemblerTests(ITestOutputHelper output)
        : base(output)
    {
    }
}
