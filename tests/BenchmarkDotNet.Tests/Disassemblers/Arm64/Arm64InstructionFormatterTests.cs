using AsmArm64;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Disassemblers;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public partial class Arm64InstructionFormatterTests : Arm64DisassemblerTestBase
{
    private static readonly Iced.Intel.FormatterOptions IcedDefaultFormatterOptions = new()
    {
        FirstOperandCharIndex = 10, // Use DisassemblyDiagnoserConfig default config value.
    };

    private static readonly Iced.Intel.FormatterOptions IcedEmptyFormatterOptions = new();

    private static readonly Arm64InstructionFormattingOptions FormattingOptions = Arm64InstructionFormatter.DefaultFormattingOptions;

    public Arm64InstructionFormatterTests(ITestOutputHelper output)
        : base(output)
    {
    }

    private static Arm64Asm[] Decode(
        uint[] rawInstructions,
        ulong baseAddress,
        Dictionary<ulong, string> symbols)
    {
        using var clrRuntime = CreateMockClrRuntime();
        var state = new State(clrRuntime, DummyTargetFrameworkVersion);
        foreach (var symbol in symbols)
        {
            state.AddressToNameMapping.Add(symbol.Key, symbol.Value);
        }

        var bytes = rawInstructions.ToLittleEndianBytes();
        var helper = new Arm64DisassemblerHelper();
        return helper.Decode(bytes, baseAddress, state, depth: 0, DummyCurrentMethod, DisassemblySyntax.Masm);
    }

    private static Arm64Asm ToArm64Asm(
        uint rawInstruction,
        ulong address,
        ulong? referencedAddress = null,
        bool isReferencedAddressIndirect = false)
    {
        if (referencedAddress <= ushort.MaxValue)
            referencedAddress = null;

        return new Arm64Asm
        {
            InstructionPointer = address,
            RawInstruction = rawInstruction,
            ReferencedAddress = referencedAddress,
            IsReferencedAddressIndirect = isReferencedAddressIndirect,
            DisassembleSyntax = DisassembleSyntax.Gas,
        };
    }

    private static string Format(
        Arm64Asm asm,
        Iced.Intel.FormatterOptions? formatterOptions = null,
        bool printInstructionAddresses = false,
        Dictionary<ulong, string>? addressMappings = null)
    {
        formatterOptions ??= IcedEmptyFormatterOptions;
        return Arm64InstructionFormatter.Format(asm, formatterOptions, printInstructionAddresses, pointerSize: 8, addressMappings ?? []);
    }
}
