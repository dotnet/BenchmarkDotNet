using AsmArm64;
using BenchmarkDotNet.Diagnosers;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interfaces;
using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Disassemblers
{
    internal class Arm64Disassembler : ClrMdDisassembler
    {
        protected override IEnumerable<Asm> Decode(byte[] code, ulong startAddress, State state, int depth, IClrMethod currentMethod, DisassemblySyntax syntax)
        {
            Arm64RegisterValueAccumulator accumulator = new();
            accumulator.Init(state.Runtime);

            ReadOnlySpan<uint> rawInstructions = MemoryMarshal.Cast<byte, uint>(code);

            var results = new List<Arm64Asm>();

            for (int i = 0; i < rawInstructions.Length; ++i)
            {
                var rawInstruction = rawInstructions[i];
                var instruction = Arm64Instruction.Decode(rawInstruction);
                var instructionAddress = startAddress + (4 * (ulong) i);

                if (TryGetReferencedAddress(instruction, instructionAddress, accumulator, (uint) state.Runtime.DataTarget.DataReader.PointerSize, out var referencedAddress, out var isIndirect))
                {
                    bool isPrestubMD = false;
                    if (isIndirect && state.RuntimeVersion.Major >= 7)
                    {
                        FlushCachedDataIfNeeded(state.Runtime.DataTarget.DataReader, referencedAddress, new byte[1]);
                        TryResolvePrecode(state.Runtime.DataTarget.DataReader, ref referencedAddress, out isPrestubMD);
                    }
                    TryTranslateAddressToName(referencedAddress, isPrestubMD, state, depth, currentMethod);
                }

                accumulator.Feed(instruction, instructionAddress);

                results.Add(new Arm64Asm()
                {
                    InstructionPointer = instructionAddress,
                    RawInstruction = rawInstruction,
                    DisassembleSyntax = DisassembleSyntax.Gas,
                    ReferencedAddress = IsValidAddress(referencedAddress) ? referencedAddress : null,
                    IsReferencedAddressIndirect = isIndirect,
                });
            }

            return results;
        }

        // Counterpart of IntelDisassembler.TryResolvePrecode: recognise the AArch64 precode/stub
        // shapes by matching the fixed opcode bits and reading slot displacements out of the
        // encoded LDR-literal instructions. Resolves to the MethodDesc handle when one is present
        // (so GetMethodByHandle can recover the live ClrMethod even if the call site is still
        // pointing at PreStub), and to the TargetForMethod slot for call-counting stubs.
        //
        // See dotnet/runtime src/coreclr/vm/arm64/thunktemplates.asm/.S for the canonical stub
        // shapes. The register numbers (x10/x12 for StubPrecode, x11/x12 for FixupPrecode, x9 for
        // CallCountingStub) are part of the runtime's stub ABI and stay fixed across versions; the
        // data-section layout is also stable. What can change between versions is the offset
        // between the code page and its data section, so we extract the LDR-literal displacements
        // straight from the bytes instead of consulting a runtime-version-specific page-size table.
        internal static bool TryResolvePrecode(IDataReader reader, ref ulong address, out bool isPrestubMD)
        {
            isPrestubMD = false;
            if (!TryReadStubHead(reader, address, out ulong parseBase, out uint instr0, out uint instr1, out uint instr2))
                return false;

            // StubPrecode: LDR x10, Target ; LDR x12, MethodDesc ; BR x10
            if (IsLdrLiteral64(instr0, out int rt0, out int _) && rt0 == 10
                && IsLdrLiteral64(instr1, out int rt1, out int off1) && rt1 == 12
                && instr2 == 0xD61F0140u)
            {
                ulong mdSlot = unchecked(parseBase + 4 + (ulong) (long) off1);
                if (reader.ReadPointer(mdSlot, out ulong md) && IsValidAddress(md))
                {
                    address = md;
                    isPrestubMD = true;
                    return true;
                }
                return false;
            }

            // FixupPrecode: LDR x11, Target ; BR x11 ; LDR x12, MethodDesc
            if (IsLdrLiteral64(instr0, out int rtA, out int _) && rtA == 11
                && instr1 == 0xD61F0160u
                && IsLdrLiteral64(instr2, out int rtB, out int off2) && rtB == 12)
            {
                ulong mdSlot = unchecked(parseBase + 8 + (ulong) (long) off2);
                if (reader.ReadPointer(mdSlot, out ulong md) && IsValidAddress(md))
                {
                    address = md;
                    isPrestubMD = true;
                    return true;
                }
                return false;
            }

            // FixupPrecodeCode_Fixup: LDR x12, MethodDesc ; LDR x11, PrecodeFixupThunk ; BR x11
            // This is the pre-backpatch shape — the call site has never been routed through the
            // method's JIT'd entry point yet, so x11 still loads the fixup thunk instead of Target.
            // Resolve via the MethodDesc slot loaded into x12 (instr0).
            if (IsLdrLiteral64(instr0, out int rtF0, out int offF0) && rtF0 == 12
                && IsLdrLiteral64(instr1, out int rtF1, out int _) && rtF1 == 11
                && instr2 == 0xD61F0160u)
            {
                ulong mdSlot = unchecked(parseBase + (ulong) (long) offF0);
                if (reader.ReadPointer(mdSlot, out ulong md) && IsValidAddress(md))
                {
                    address = md;
                    isPrestubMD = true;
                    return true;
                }
                return false;
            }

            // CallCountingStub: LDR x9, RemainingCallCount ; LDRH w10, [x9] ; SUBS w10, w10, #1
            // No MethodDesc to recover here; read TargetForMethod, which lives 8 bytes after
            // RemainingCallCount in the data section.
            if (IsLdrLiteral64(instr0, out int rtCount, out int offCount) && rtCount == 9
                && instr1 == 0x7940012Au
                && instr2 == 0x7100054Au)
            {
                ulong countSlot = unchecked(parseBase + (ulong) (long) offCount);
                if (reader.ReadPointer(countSlot + 8, out ulong target) && IsValidAddress(target))
                {
                    address = target;
                    return true;
                }
                return false;
            }

            return false;
        }

        // .NET 10 prefixes some AArch64 precode/stub shapes with `DMB ISHLD` for concurrent
        // stub-patching safety. Detect and skip the barrier so the existing 3-instruction pattern
        // match still works, and return the effective PC of the first real stub instruction so
        // LDR-literal offsets are calculated relative to it.
        // Encoding: DMB ISHLD = 0xD50339BF.
        private const uint DmbIshLdInstr = 0xD50339BFu;

        internal static bool TryReadStubHead(IDataReader reader, ulong address, out ulong parseBase, out uint instr0, out uint instr1, out uint instr2)
        {
            parseBase = address;
            instr0 = instr1 = instr2 = 0;

            // IDataReader.Read may return partial data.
            // Call it repeatedly until the buffer is filled or no more data is available.
            Span<byte> buffer = stackalloc byte[16];
            int totalBytesRead = 0;
            while (totalBytesRead < buffer.Length)
            {
                int bytesRead = reader.Read(address + (ulong) totalBytesRead, buffer.Slice(totalBytesRead));
                if (bytesRead <= 0)
                    break;
                totalBytesRead += bytesRead;
            }

            if (totalBytesRead < 12)
                return false;

            int offset = 0;
            uint first = ReadInstr(buffer, 0);
            if (first == DmbIshLdInstr)
            {
                if (totalBytesRead < 16)
                    return false;
                offset = 4;
                parseBase = address + 4;
            }

            instr0 = ReadInstr(buffer, offset + 0);
            instr1 = ReadInstr(buffer, offset + 4);
            instr2 = ReadInstr(buffer, offset + 8);
            return true;
        }

        private static uint ReadInstr(ReadOnlySpan<byte> buffer, int offset)
            => (uint) buffer[offset]
             | ((uint) buffer[offset + 1] << 8)
             | ((uint) buffer[offset + 2] << 16)
             | ((uint) buffer[offset + 3] << 24);

        // LDR (literal), 64-bit form. Encoding: bits[31:24]=0x58, bits[23:5]=imm19 (signed,
        // word-scaled offset relative to the LDR's own PC), bits[4:0]=Xt. Returns the destination
        // register and the byte-scaled offset from the LDR instruction's address to the loaded slot.
        internal static bool IsLdrLiteral64(uint instr, out int rt, out int offsetBytes)
        {
            rt = 0;
            offsetBytes = 0;
            if ((instr & 0xFF000000u) != 0x58000000u)
                return false;
            rt = (int) (instr & 0x1Fu);
            int imm19 = (int) ((instr >> 5) & 0x7FFFFu);
            // Sign-extend 19-bit imm to 32-bit.
            if ((imm19 & 0x40000) != 0)
                imm19 |= unchecked((int) 0xFFF80000u);
            offsetBytes = imm19 * 4;
            return true;
        }

        internal static bool TryGetReferencedAddress(
            Arm64Instruction instruction,
            ulong instructionAddress,
            Arm64RegisterValueAccumulator accumulator,
            uint pointerSize,
            out ulong referencedAddress,
            out bool isReferencedAddressIndirect)
        {

            switch (instruction.Mnemonic)
            {
                // TODO: Add support for BRAA/BRAB/BRAAZ/BRABZ/BLRAA/BLRAB/BLRAAZ/BLRABZ instructions. Currently .NET JIT don't emit these instructions.
                // See: https://github.com/dotnet/runtime/blob/v11.0.0-rc.3.26522.148/src/coreclr/jit/instrsarm64.h
                case Arm64Mnemonic.BR:
                case Arm64Mnemonic.BLR:
                    if (instruction.TryGetOperand(0, out Arm64RegisterOperand register) && register.Value == accumulator.TrackedRegister)
                    {
                        if (accumulator.HasValue)
                        {
                            // Branch via register where we have extracted the value of the register by parsing the disassembly.
                            // The accumulator holds an absolute address (MOVZ/MOVK constant or ADRP resolved to a page address).
                            referencedAddress = accumulator.Value;
                            isReferencedAddressIndirect = true;
                            return true;
                        }
                    }
                    break;
            }

            if (instruction.BelongsToGroup(Arm64InstructionGroupId.ARM64_GRP_BRANCH_RELATIVE))
            {
                if (instruction.Flags.HasFlag(Arm64InstructionFlags.HasLabel))
                {
                    // One of the label operands is the address
                    for (int i = 0; i < instruction.Operands.Count; i++)
                    {
                        if (!instruction.TryGetOperand(i, out Arm64LabelOperand labelOperand))
                            continue;

                        referencedAddress = instruction.GetTargetAddress(instructionAddress, labelOperand.Offset);
                        isReferencedAddressIndirect = false;
                        return true;
                    }
                }
            }

            referencedAddress = 0;
            isReferencedAddressIndirect = false;
            return false;
        }

        // Recognise the AArch64 jump trampoline shape the CLR JIT emits when a call's real target
        // is out of rel26 range (±128 MB), plus the precode/stub shapes the runtime emits as the
        // stable entry point for tiered methods (so a direct `BL imm26` landing on the precode
        // still resolves to the underlying method):
        //   B imm26   (bits[31:26] = 0b000101)   — target = address + sign_extended(imm26) * 4
        //   CallCountingStub  (opcode match)     — reads TargetForMethod slot
        //   StubPrecode       (opcode match)     — reads Target slot (the LDR that BR consumes)
        //   FixupPrecode      (opcode match)     — reads Target slot (the LDR that BR consumes)
        // Slot displacements are extracted from the LDR-literal instructions themselves, so the
        // stub recognition doesn't depend on the runtime's code-to-data offset. Writes the resolved
        // target into `target` and returns true if one matches.
        protected override bool TryFollowJumpTrampoline(State state, ulong address, out ulong target)
        {
            target = 0;
            IDataReader dataReader = state.Runtime.DataTarget.DataReader;

            // B imm26 shape is 4 bytes and cannot be prefixed with DMB (the barrier is only
            // emitted for precode/stub shapes, not for JIT-emitted jump trampolines).
            byte[] head = new byte[4];
            if (dataReader.Read(address, head) < 4)
                return false;
            uint firstInstr = ReadInstr(head, 0);
            if ((firstInstr >> 26) == 0x5)
            {
                uint imm26 = firstInstr & 0x03FFFFFFu;
                // Sign-extend the 26-bit immediate to 32 bits, then multiply by 4 (instructions are 4-byte aligned).
                int offset = (int) (imm26 & 0x02000000u) != 0
                    ? unchecked((int) (imm26 | 0xFC000000u)) << 2
                    : (int) imm26 << 2;
                target = unchecked(address + (ulong) (long) offset);
                return IsValidAddress(target);
            }

            if (!TryReadStubHead(dataReader, address, out ulong parseBase, out uint instr0, out uint instr1, out uint instr2))
                return false;

            // StubPrecode: LDR x10, Target ; LDR x12, MethodDesc ; BR x10. Follow the first LDR.
            if (IsLdrLiteral64(instr0, out int rt0, out int off0) && rt0 == 10
                && IsLdrLiteral64(instr1, out int rt1, out int _) && rt1 == 12
                && instr2 == 0xD61F0140u)
            {
                ulong targetSlot = unchecked(parseBase + (ulong) (long) off0);
                if (dataReader.ReadPointer(targetSlot, out target) && IsValidAddress(target))
                    return true;
                target = 0;
                return false;
            }

            // FixupPrecode: LDR x11, Target ; BR x11 ; LDR x12, MethodDesc. Follow the first LDR.
            if (IsLdrLiteral64(instr0, out int rtA, out int offA) && rtA == 11
                && instr1 == 0xD61F0160u
                && IsLdrLiteral64(instr2, out int rtB, out int _) && rtB == 12)
            {
                ulong targetSlot = unchecked(parseBase + (ulong) (long) offA);
                if (dataReader.ReadPointer(targetSlot, out target) && IsValidAddress(target))
                    return true;
                target = 0;
                return false;
            }

            // CallCountingStub: LDR x9, RemainingCallCount ; LDRH w10, [x9] ; SUBS w10, w10, #1.
            // TargetForMethod lives 8 bytes after RemainingCallCount in the data section.
            if (IsLdrLiteral64(instr0, out int rtCount, out int offCount) && rtCount == 9
                && instr1 == 0x7940012Au
                && instr2 == 0x7100054Au)
            {
                ulong countSlot = unchecked(parseBase + (ulong) (long) offCount);
                if (dataReader.ReadPointer(countSlot + 8, out target) && IsValidAddress(target))
                    return true;
                target = 0;
                return false;
            }

            return false;
        }
    }
}
