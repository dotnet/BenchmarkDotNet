using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Disassemblers;
using Microsoft.Diagnostics.Runtime.Interfaces;

#if NET8_0_OR_GREATER
using Microsoft.Diagnostics.Runtime;
using System.Runtime.CompilerServices;
using Arm64Instruction = Gee.External.Capstone.Arm64.Arm64Instruction;
#endif

using Arm64Disassembler = BenchmarkDotNet.Disassemblers.Arm64Disassembler;


namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

internal class Arm64DisassemblerHelper : Arm64Disassembler
{
    public new Arm64Asm[] Decode(
        byte[] code,
        ulong startAddress,
        State state,
        int depth,
        IClrMethod currentMethod,
        DisassemblySyntax syntax)
    {
        var results = base.Decode(code, startAddress, state, depth, currentMethod, syntax);
        return results.Cast<Arm64Asm>().ToArray();
    }

    public new void TryTranslateAddressToName(ulong address, bool isAddressPrecodeMD, State state, int depth, IClrMethod currentMethod)
    {
        base.TryTranslateAddressToName(address, isAddressPrecodeMD, state, depth, currentMethod);
    }

    public new bool TryFollowJumpTrampoline(State state, ulong address, out ulong target)
    {
        return base.TryFollowJumpTrampoline(state, address, out target);
    }

    // Following methods require UnsafeAccessor to invoke private methods.
#if NET8_0_OR_GREATER
    public static bool TryResolvePrecode(IDataReader reader, ref ulong address, out bool isPrestubMD)
    {
        var disassembler = new Arm64Disassembler();
        return TryResolvePrecode(disassembler, reader, ref address, out isPrestubMD);
    }

    public static bool TryReadStubHead(IDataReader reader, ulong address, out ulong parseBase, out uint instr0, out uint instr1, out uint instr2)
    {
        var disassembler = new Arm64Disassembler();
        return TryReadStubHead(disassembler, reader, address, out parseBase, out instr0, out instr1, out instr2);
    }

    public static bool IsLdrLiteral64(uint instr, out int rt, out int offsetBytes)
    {
        var disassembler = new Arm64Disassembler();
        return IsLdrLiteral64(disassembler, instr, out rt, out offsetBytes);
    }

    public static bool TryGetReferencedAddress(Arm64Instruction instruction, Arm64RegisterValueAccumulator accumulator, uint pointerSize, out ulong referencedAddress, out bool isReferencedAddressIndirect)
    {
        var disassembler = new Arm64Disassembler();
        return TryGetReferencedAddress(disassembler, instruction, accumulator, pointerSize, out referencedAddress, out isReferencedAddressIndirect);
    }

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = nameof(TryResolvePrecode))]
    private static extern bool TryResolvePrecode(
        Arm64Disassembler disassembler,
        IDataReader reader,
        ref ulong address,
        out bool isPrestubMD);

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = nameof(TryReadStubHead))]
    private static extern bool TryReadStubHead(
        Arm64Disassembler disassembler,
        IDataReader reader,
        ulong address,
        out ulong parseBase,
        out uint instr0,
        out uint instr1,
        out uint instr2);

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = nameof(IsLdrLiteral64))]
    private static extern bool IsLdrLiteral64(
        Arm64Disassembler disassembler,
        uint instr,
        out int rt,
        out int offsetBytes);

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = nameof(TryGetReferencedAddress))]
    private static extern bool TryGetReferencedAddress(
        Arm64Disassembler
        disassembler,
        Arm64Instruction instruction,
        Arm64RegisterValueAccumulator accumulator,
        uint pointerSize,
        out ulong referencedAddress,
        out bool isReferencedAddressIndirect);
#endif
}
