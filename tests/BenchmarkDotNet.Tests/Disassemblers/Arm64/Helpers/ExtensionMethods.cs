using AsmArm64;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Tests.Disassemblers;

internal static class ExtensionMethods
{
    public static Arm64Instruction ToArm64Instruction(this uint rawInstruction)
        => Arm64Instruction.Decode(rawInstruction);

    public static Arm64Instruction[] ToArm64Instructions(this uint[] rawInstructions)
     => rawInstructions.Select(Arm64Instruction.Decode).ToArray();

    public static byte[] ToLittleEndianBytes(this uint[] rawInstructions)
    {
        if (BitConverter.IsLittleEndian)
            return MemoryMarshal.AsBytes(rawInstructions).ToArray();

        byte[] bytes = new byte[rawInstructions.Length * 4];

        for (int i = 0; i < rawInstructions.Length; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(i * 4, 4), rawInstructions[i]);
        }
        return bytes;
    }

    public static byte[] ToLittleEndianBytes(this uint rawInstruction)
        => ToLittleEndianBytes([rawInstruction]);
}
