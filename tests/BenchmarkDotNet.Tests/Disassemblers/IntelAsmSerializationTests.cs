using AwesomeAssertions;
using BenchmarkDotNet.Disassemblers;
using BenchmarkDotNet.Serialization;
using Iced.Intel;

namespace BenchmarkDotNet.Tests.Disassemblers;

public class IntelAsmSerializationTests
{
    private const ulong StartAddress = 0x7FFB_3566_0000;

    // The in-process disassembler serializes its result back to the host, so every
    // address-carrying part of an instruction has to survive the JSON round-trip.
    private static readonly byte[] Code =
    [
        0x0F, 0x84, 0x00, 0x01, 0x00, 0x00,             // je near rel32
        0xC4, 0xE2, 0x7D, 0x18, 0x05, 0x40, 0x00, 0x00, 0x00, // vbroadcastss ymm0,[rip+40h]
        0xC5, 0xFC, 0x11, 0x44, 0x02, 0xE0,             // vmovups [rdx+rax-20h],ymm0
        0x48, 0xB8, 0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11, // mov rax,1122334455667788h
        0xEB, 0xE2,                                     // jmp short (back to start)
        0x06,                                           // invalid in 64-bit mode
    ];

    [Fact]
    public void IntelAsmSurvivesJsonRoundTrip()
    {
        var original = Decode(Code, StartAddress);
        var result = new DisassemblyResult
        {
            Methods = [new DisassembledMethod { Name = "M", Maps = [new Map { SourceCodes = original.ToArray<SourceCode>() }] }],
            PointerSize = 8,
        };

        var roundTripped = BdnJsonSerializer.Deserialize<DisassemblyResult>(BdnJsonSerializer.Serialize(result))!
            .Methods.Single().Maps.Single().SourceCodes.Cast<IntelAsm>().ToArray();

        roundTripped.Should().HaveCount(original.Length);
        for (int i = 0; i < original.Length; i++)
        {
            var expected = original[i].Instruction;
            var actual = roundTripped[i].Instruction;

            actual.IP.Should().Be(expected.IP);
            actual.NextIP.Should().Be(expected.NextIP);
            actual.Length.Should().Be(expected.Length);
            actual.Code.Should().Be(expected.Code);
            Format(actual).Should().Be(Format(expected));
            Instruction.EqualsAllBits(actual, expected).Should().BeTrue($"instruction {i} ({Format(expected)}) should round-trip exactly");
        }
    }

    private static IntelAsm[] Decode(byte[] code, ulong ip)
    {
        var decoder = Iced.Intel.Decoder.Create(64, new ByteArrayCodeReader(code));
        decoder.IP = ip;
        var instructions = new List<IntelAsm>();
        while (decoder.IP < ip + (ulong) code.Length)
        {
            decoder.Decode(out var instruction);
            instructions.Add(new IntelAsm { InstructionPointer = instruction.IP, InstructionLength = instruction.Length, Instruction = instruction });
        }
        return instructions.ToArray();
    }

    private static string Format(Instruction instruction)
    {
        var output = new StringOutput();
        new MasmFormatter().Format(instruction, output);
        return output.ToString();
    }
}
