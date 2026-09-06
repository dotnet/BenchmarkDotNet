// TODO: Remove #if directive when migrated to xunit.v3 or migrated to AsmArm64 based implementation.
#if NET
using AsmArm64;
using AwesomeAssertions;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Disassemblers;
using Iced.Intel;
using static AsmArm64.Arm64RegisterW;
using static AsmArm64.Arm64RegisterX;
namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public partial class Arm64InstructionFormatterTests : Arm64DisassemblerTestBase
{
    private static readonly FormatterOptions DefaultFormatterOptions = new()
    {
        FirstOperandCharIndex = 10, // Use DisassemblyDiagnoserConfig default config value.
    };

    private static readonly Arm64InstructionFormattingOptions AsmArm64FormattingOptions = new()
    {
        AliasMode = Arm64InstructionAliasMode.BaseInstruction,
        UseUppercaseText = false,
        UseUppercaseHex = false,
        ImmediateFormat = Arm64NumericFormat.Hexadecimal,
        MemoryOffsetFormat = Arm64NumericFormat.Hexadecimal,
        LabelOffsetFormat = Arm64NumericFormat.Auto,
    };

    public Arm64InstructionFormatterTests(ITestOutputHelper output)
        : base(output)
    {
    }

    [Theory]
    [MemberData(nameof(TestData.TestData01), MemberType = typeof(TestData))]
    public void FormatInstruction(uint rawInstruction, string expected)
    {
        // Arrange
        var asm = rawInstruction.ToArm64Asm(address: 0);

        // Act
        var result = asm.Format(new FormatterOptions());

        // Assert
        result.Should().Be(expected);

        // Verify AsmArm64 compatibility
        var instruction = rawInstruction.ToAsmArm64Instruction();
        var text = instruction.ToString(AsmArm64FormattingOptions);

        text.Should().Be(expected);
    }

    [Fact]
    public void FormatInstruction_IntroDisassembly_SumLocal()
    {
        // Arrange
        var rawInstructions = new[]
        {
            Arm64InstructionFactory.STP(X29, X30, new Arm64ImmediateMemoryAccessor(Arm64RegisterSP.SP, -0x10).Pre),
            Arm64InstructionFactory.MOV(X29, Arm64RegisterSP.SP),
            Arm64InstructionFactory.LDR(X0, new Arm64ImmediateMemoryAccessor(X0, 8)),
            Arm64InstructionFactory.MOV(W1, WZR),
            Arm64InstructionFactory.LDR(W2, new Arm64ImmediateMemoryAccessor(X0, 8)),
            Arm64InstructionFactory.CMP(W2, 0),
            Arm64InstructionFactory.B(Arm64ConditionalKind.LE, 24),
            Arm64InstructionFactory.ADD(X0,X0, 0x10),
            // M00_L00:
            Arm64InstructionFactory.LDR(W3, new Arm64BaseMemoryAccessor(X0), 4),
            Arm64InstructionFactory.ADD(W1,W3,W1),
            Arm64InstructionFactory.SUB(W2,W2, 1),
            Arm64InstructionFactory.CBNZ(W2, -12),
            // M00_L01:
            Arm64InstructionFactory.MOV(W0, W1),
            Arm64InstructionFactory.LDP(X29, X30, new Arm64BaseMemoryAccessor(Arm64RegisterSP.SP), 0x10),
            Arm64InstructionFactory.RET()
        };
        PrintInstructions(rawInstructions);

        const ulong BaseAddress = 0xFF01097000A0;
        var symbols = new Dictionary<ulong, string>
        {
            [0xFF01097000C0] = "M00_L00",
            [0xFF01097000D0] = "M00_L01",
        };
        Arm64Asm[] asms = GetArm64Asms(rawInstructions, BaseAddress, symbols);

        // Act
        List<string> results = new();
        foreach (var asm in asms)
        {
            var result = Arm64InstructionFormatter.Format(asm, DefaultFormatterOptions, printInstructionAddresses: true, pointerSize: 8, symbols);
            results.Add(result);
        }

        // Assert
        results.Should().BeEquivalentTo(
        [
            "FF01097000A0 stp       x29, x30, [sp, #-0x10]!",
            "FF01097000A4 mov       x29, sp",
            "FF01097000A8 ldr       x0, [x0, #8]",
            "FF01097000AC mov       w1, wzr",
            "FF01097000B0 ldr       w2, [x0, #8]",
            "FF01097000B4 cmp       w2, #0",
            "FF01097000B8 b.le      M00_L01",
            "FF01097000BC add       x0, x0, #0x10",
            // M00_L00:
            "FF01097000C0 ldr       w3, [x0], #4",
            "FF01097000C4 add       w1, w3, w1",
            "FF01097000C8 sub       w2, w2, #1",
            "FF01097000CC cbnz      w2, M00_L00",
            // M00_L01:
            "FF01097000D0 mov       w0, w1",
            "FF01097000D4 ldp       x29, x30, [sp], #0x10",
            "FF01097000D8 ret       ",
        ]);
    }

    private static Arm64Asm[] GetArm64Asms(uint[] rawInstructions, ulong baseAddress, Dictionary<ulong, string> symbols)
    {      
        var clrRuntime = CreateMockClrRuntime(0);
        var state = new State(clrRuntime, DummyTargetFrameworkVersion);
        foreach (var symbol in symbols)
        {
            state.AddressToNameMapping.Add(symbol.Key, symbol.Value);
        }

        var bytes = rawInstructions.ToLittleEndianBytes();
        var helper = new Arm64DisassemblerHelper();
        return helper.Decode(bytes, baseAddress, state, depth: 0, DummyCurrentMethod, DisassemblySyntax.Masm);
    }

    public static class TestData
    {
        public static TheoryData<uint, string> TestData01 => new()
        {
            { Arm64InstructionFactory.B(0x8),                     "b #8"}, // Label value that lower than 0x10 use decimal format.
            { Arm64InstructionFactory.B(0x100),                   "b #0x100"},
            { Arm64InstructionFactory.MOVZ(W0, 0x8),              "movz w0, #0x8"},
            { Arm64InstructionFactory.MOVZ(X0, 0x100),            "movz x0, #0x100"},
            { Arm64InstructionFactory.MOVZ(X0, 0xFFF),            "movz x0, #0xfff"},
            { Arm64InstructionFactory.MOVZ(X0, 0x100),            "movz x0, #0x100"},
            { Arm64InstructionFactory.MOVZ(X0, 0x100, amount:16), "movz x0, #0x100, lsl #16"},

            // TODO: `Gee.External.Capstone` seems not support negative label offset and wrong text is outputted. 
            // { Arm64InstructionFactory.B(-8),                      "b #-8"}, // Gee.External.Capstone don't support minus label offset.
        };
    }
}

file static class ExtensionMethods
{
    public static string Format(
        this Arm64Asm asm,
        FormatterOptions options,
        bool printInstructionAddresses = false,
        Dictionary<ulong, string>? addressMappings = null)
    {
        uint pointerSize = 8;
        return Arm64InstructionFormatter.Format(asm, options, printInstructionAddresses, pointerSize, addressMappings ?? []);
    }

    public static Arm64Asm ToArm64Asm(this uint rawInstruction, ulong address, ulong? referencedAddress = null, bool isReferencedAddressIndirect = false)
    {
        if (referencedAddress <= ushort.MaxValue)
            referencedAddress = null;

        return new Arm64Asm
        {
            InstructionPointer = address,
            InstructionLength = 4,
            ReferencedAddress = referencedAddress,
            IsReferencedAddressIndirect = isReferencedAddressIndirect,
            Instruction = rawInstruction.ToCapstoneArm64Instruction(address),
        };
    }
}
#endif
