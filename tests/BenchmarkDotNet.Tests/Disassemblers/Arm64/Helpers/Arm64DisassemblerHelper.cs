using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Disassemblers;
using Microsoft.Diagnostics.Runtime.Interfaces;
using System.Diagnostics;

using Arm64Disassembler = BenchmarkDotNet.Disassemblers.Arm64Disassembler;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

internal class Arm64DisassemblerHelper : Arm64Disassembler
{
    [DebuggerHidden]
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

    [DebuggerHidden]
    public new void TryTranslateAddressToName(ulong address, bool isAddressPrecodeMD, State state, int depth, IClrMethod currentMethod)
    {
        base.TryTranslateAddressToName(address, isAddressPrecodeMD, state, depth, currentMethod);
    }

    [DebuggerHidden]
    public new bool TryFollowJumpTrampoline(State state, ulong address, out ulong target)
    {
        return base.TryFollowJumpTrampoline(state, address, out target);
    }
}
