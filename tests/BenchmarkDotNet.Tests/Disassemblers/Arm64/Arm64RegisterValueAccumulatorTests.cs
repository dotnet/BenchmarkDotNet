using AwesomeAssertions;
using BenchmarkDotNet.Disassemblers;
using Microsoft.Diagnostics.Runtime.Interfaces;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public partial class Arm64RegisterValueAccumulatorTests : Arm64DisassemblerTestBase
{
    private const ushort MovzOffset = 0x1000;

    private const long ExpectedResultAddress = 0x1234;

    public Arm64RegisterValueAccumulatorTests(ITestOutputHelper output) : base(output)
    {
    }

    private static Arm64RegisterValueAccumulator CreateValueAccumulator(IClrRuntime? clrRuntime = null)
    {
        var accumulator = new Arm64RegisterValueAccumulator();
        accumulator.Init(clrRuntime ?? CreateMockClrRuntime()); // Note: Currently MockClrRuntime is not disposed.
        return accumulator;
    }

    [Fact]
    public void InitialState_ShouldNotHaveValue()
    {
        // Arrange
        var accumulator = CreateValueAccumulator();

        // Assert
        accumulator.HasValue.Should().BeFalse();
        accumulator.Value.Should().Be(0);
    }
}
