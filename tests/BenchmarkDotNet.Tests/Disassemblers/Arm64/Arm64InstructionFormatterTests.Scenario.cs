using AsmArm64;
using AwesomeAssertions;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Disassemblers;
using static AsmArm64.Arm64RegisterW;
using static AsmArm64.Arm64RegisterX;
using static AsmArm64.Arm64RegisterSP;
using static AsmArm64.Arm64InstructionFactory;
using AwesomeAssertions.Execution;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public partial class Arm64InstructionFormatterTests : Arm64DisassemblerTestBase
{
    [Fact]
    public void FormatInstruction_IntroDisassembly_SumLocal()
    {
        // Arrange
        var rawInstructions = new[]
        {
            STP(X29, X30, new Arm64ImmediateMemoryAccessor(SP, -0x10).Pre),  // stp x29, x30, [sp, #-0x10]!
            MOV(X29, SP),                                                    // mov x29, sp
            LDR(X0, new Arm64ImmediateMemoryAccessor(X0, 8)),                // ldr x0, [x0, #8]
            MOV(W1, WZR),                                                    // mov w1, wzr
            LDR(W2, new Arm64ImmediateMemoryAccessor(X0, 8)),                // ldr w2, [x0, #8]
            CMP(W2, 0),                                                      // cmp w2, #0
            B(Arm64ConditionalKind.LE, 0x18),                                // b.le #0x18
            ADD(X0,X0, 0x10),                                                // add x0, x0, #0x10
            // M00_L00:
            LDR(W3, new Arm64BaseMemoryAccessor(X0), 4),                     // ldr w3, [x0], #4
            ADD(W1,W3,W1),                                                   // add w1, w3, w1
            SUB(W2,W2, 1),                                                   // sub w2, w2, #1
            CBNZ(W2, -0x0C),                                                 // cbnz w2, #-0xC
            // M00_L01:
            MOV(W0, W1),                                                     // mov w0, w1
            LDP(X29, X30, new Arm64BaseMemoryAccessor(SP), 0x10),            // ldp x29, x30, [sp], #0x10
            RET(),
        };
        PrintInstructions(rawInstructions);

        const ulong BaseAddress = 0xFF01097000A0;
        var symbols = new Dictionary<ulong, string>
        {
            [0xFF01097000C0] = "M00_L00",
            [0xFF01097000D0] = "M00_L01",
        };
        Arm64Asm[] asms = Decode(rawInstructions, BaseAddress, symbols);

        // Act
        List<string> results = new();
        foreach (var asm in asms)
        {
            var result = Arm64InstructionFormatter.Format(asm, IcedDefaultFormatterOptions, printInstructionAddresses: true, pointerSize: 8, symbols);
            results.Add(result);
        }

        // Assert
        using (var scope = new AssertionScope())
        {
            scope.FormattingOptions.StringPrintLength = 100;
            results[00].Should().Be("FF01097000A0 stp       x29, x30, [sp, #-0x10]!");
            results[01].Should().Be("FF01097000A4 mov       x29, sp");
            results[02].Should().Be("FF01097000A8 ldr       x0, [x0, #8]");
            results[03].Should().Be("FF01097000AC mov       w1, wzr");
            results[04].Should().Be("FF01097000B0 ldr       w2, [x0, #8]");
            results[05].Should().Be("FF01097000B4 cmp       w2, #0");
            results[06].Should().Be("FF01097000B8 b.le      M00_L01");
            results[07].Should().Be("FF01097000BC add       x0, x0, #0x10");
            // M00_L00:
            results[08].Should().Be("FF01097000C0 ldr       w3, [x0], #4");
            results[09].Should().Be("FF01097000C4 add       w1, w3, w1");
            results[10].Should().Be("FF01097000C8 sub       w2, w2, #1");
            results[11].Should().Be("FF01097000CC cbnz      w2, M00_L00");
            // M00_L01:
            results[12].Should().Be("FF01097000D0 mov       w0, w1");
            results[13].Should().Be("FF01097000D4 ldp       x29, x30, [sp], #0x10");
            results[14].Should().Be("FF01097000D8 ret");
        }
    }
}
