using BenchmarkDotNet.Diagnosers;
using Iced.Intel;
using Microsoft.Diagnostics.Runtime;

namespace BenchmarkDotNet.Disassemblers
{
    internal class IntelDisassembler : ClrMdDisassembler
    {
        internal sealed class RuntimeSpecificData
        {
            // See dotnet/runtime src/coreclr/vm/amd64/thunktemplates.asm/.S for the stub code
            // mov    rax,QWORD PTR [rip + DATA_SLOT(CallCountingStub, RemainingCallCountCell)]
            // dec    WORD PTR [rax]
            // je     LOCAL_LABEL(CountReachedZero)
            // jmp    QWORD PTR [rip + DATA_SLOT(CallCountingStub, TargetForMethod)]
            // LOCAL_LABEL(CountReachedZero):
            // jmp    QWORD PTR [rip + DATA_SLOT(CallCountingStub, TargetForThresholdReached)]
            internal readonly byte[] callCountingStubTemplate = [0x48, 0x8b, 0x05, 0xf9, 0x0f, 0x00, 0x00, 0x66, 0xff, 0x08];
            // mov    r10, [rip + DATA_SLOT(StubPrecode, MethodDesc)]
            // jmp    [rip + DATA_SLOT(StubPrecode, Target)]
            internal readonly byte[] stubPrecodeTemplate = [0x4c, 0x8b, 0x15, 0xf9, 0x0f, 0x00, 0x00, 0xff, 0x25, 0xfb, 0x0f, 0x00, 0x00];
            // jmp    [rip + DATA_SLOT(FixupPrecode, Target)]
            // mov    r10, [rip + DATA_SLOT(FixupPrecode, MethodDesc)]
            // jmp    [rip + DATA_SLOT(FixupPrecode, PrecodeFixupThunk)]
            internal readonly byte[] fixupPrecodeTemplate = [0xff, 0x25, 0xfa, 0x0f, 0x00, 0x00, 0x4c, 0x8b, 0x15, 0xfb, 0x0f, 0x00, 0x00, 0xff, 0x25, 0xfd, 0x0f, 0x00, 0x00];
            internal readonly ulong stubPageSize;

            internal RuntimeSpecificData(State state)
            {
                stubPageSize = (ulong)Environment.SystemPageSize;
                if (state.RuntimeVersion.Major >= 8)
                {
                    // In .NET 8, the stub page size was changed to 16kB
                    stubPageSize = 16384;
                    // Update the templates so that the offsets are correct
                    callCountingStubTemplate[4] = 0x3f;
                    stubPrecodeTemplate[4] = 0x3f;
                    stubPrecodeTemplate[10] = 0x3f;
                    fixupPrecodeTemplate[3] = 0x3f;
                    fixupPrecodeTemplate[10] = 0x3f;
                    fixupPrecodeTemplate[16] = 0x3f;
                }
            }
        }

        private static readonly Dictionary<Version, RuntimeSpecificData> runtimeSpecificData = [];

        protected override IEnumerable<Asm> Decode(byte[] code, ulong startAddress, State state, int depth, ClrMethod currentMethod, DisassemblySyntax syntax)
        {
            if (!runtimeSpecificData.TryGetValue(state.RuntimeVersion, out var data))
            {
                runtimeSpecificData.Add(state.RuntimeVersion, data = new RuntimeSpecificData(state));
            }

            var reader = new ByteArrayCodeReader(code);
            var decoder = Decoder.Create(state.Runtime.DataTarget.DataReader.PointerSize * 8, reader);
            decoder.IP = startAddress;

            while (reader.CanReadByte)
            {
                decoder.Decode(out var instruction);

                bool isIndirect = instruction.IsCallFarIndirect || instruction.IsCallNearIndirect || instruction.IsJmpFarIndirect || instruction.IsJmpNearIndirect;
                bool isPrestubMD = false;

                ulong address = 0;
                if (TryGetReferencedAddress(instruction, (uint)state.Runtime.DataTarget.DataReader.PointerSize, out address))
                {
                    if (isIndirect)
                    {
                        address = state.Runtime.DataTarget.DataReader.ReadPointer(address);
                        if (state.RuntimeVersion.Major >= 7)
                        {
                            FlushCachedDataIfNeeded(state.Runtime.DataTarget.DataReader, address, new byte[1]);
                            TryResolvePrecode(state.Runtime.DataTarget.DataReader, ref address, out isPrestubMD);
                        }
                    }
                    TryTranslateAddressToName(address, isPrestubMD, state, depth, currentMethod);
                }

                yield return new IntelAsm
                {
                    InstructionPointer = instruction.IP,
                    InstructionLength = instruction.Length,
                    Instruction = instruction,
                    ReferencedAddress = (address > ushort.MaxValue) ? address : null,
                    IsReferencedAddressIndirect = isIndirect,
                };
            }
        }

        // Resolve a precode/stub address (the body of one of the runtime's interleaved code-page
        // thunks) to either the underlying MethodDesc handle (so GetMethodByHandle can find the
        // method whose JITted body should be disassembled — even when the precode's own Target slot
        // still points at PreStub/PrecodeFixupThunk because the call site has never been backpatched)
        // or, for call-counting stubs that don't carry a MethodDesc, the TargetForMethod slot.
        // Returns true when the bytes at `address` match a known precode shape and `address` was
        // rewritten to the resolved slot value; `isPrestubMD` is set when the resolved value is a
        // MethodDesc handle (used downstream to dispatch to GetMethodByHandle).
        //
        // The runtime's data-section layout shifts between patch versions (slot offsets vary), so
        // we match on the fixed OPCODE bytes only and extract the actual RIP-relative displacements
        // straight from the encoded instructions — that makes us resilient to data-page layout
        // changes without hard-coding StubPrecodeData offsets per runtime version.
        private static bool TryResolvePrecode(IDataReader reader, ref ulong address, out bool isPrestubMD)
        {
            isPrestubMD = false;
            byte[] buffer = new byte[19];
            int read = reader.Read(address, buffer);
            if (read < 13)
                return false;

            // FixupPrecode (19 bytes when read available):
            //   FF 25 [disp32]      JMP qword [rip+disp]   -> Target slot
            //   4C 8B 15 [disp32]   MOV r10, [rip+disp]    -> MethodDesc slot
            //   FF 25 [disp32]      JMP qword [rip+disp]   -> PrecodeFixupThunk slot
            // Resolve to MethodDesc so we can recover the ClrMethod even if the call site has
            // not been backpatched and Target still points at PrecodeFixupThunk.
            if (read >= 19
                && buffer[0] == 0xFF && buffer[1] == 0x25
                && buffer[6] == 0x4C && buffer[7] == 0x8B && buffer[8] == 0x15
                && buffer[13] == 0xFF && buffer[14] == 0x25)
            {
                int dispMD = BitConverter.ToInt32(buffer, 9);
                ulong mdSlot = unchecked(address + 13 + (ulong)(long)dispMD);
                if (reader.ReadPointer(mdSlot, out ulong md) && IsValidAddress(md))
                {
                    address = md;
                    isPrestubMD = true;
                    return true;
                }
                return false;
            }

            // StubPrecode (13 bytes):
            //   4C 8B 15 [disp32]   MOV r10, [rip+disp]    -> MethodDesc slot
            //   FF 25 [disp32]      JMP qword [rip+disp]   -> Target slot (usually PreStub)
            if (buffer[0] == 0x4C && buffer[1] == 0x8B && buffer[2] == 0x15
                && buffer[7] == 0xFF && buffer[8] == 0x25)
            {
                int dispMD = BitConverter.ToInt32(buffer, 3);
                ulong mdSlot = unchecked(address + 7 + (ulong)(long)dispMD);
                if (reader.ReadPointer(mdSlot, out ulong md) && IsValidAddress(md))
                {
                    address = md;
                    isPrestubMD = true;
                    return true;
                }
                return false;
            }

            // CallCountingStub (matches first ~10 bytes):
            //   48 8B 05 [disp32]   MOV rax, [rip+disp]    -> RemainingCallCount slot
            //   66 FF 08            DEC word ptr [rax]
            //   (later instructions JMP to TargetForMethod, which lives 8 bytes after
            //    RemainingCallCount in the data section)
            // No MethodDesc slot here, so we have to read TargetForMethod and rely on
            // GetMethodByInstructionPointer to identify the live tier-1 code.
            if (read >= 10
                && buffer[0] == 0x48 && buffer[1] == 0x8B && buffer[2] == 0x05
                && buffer[7] == 0x66 && buffer[8] == 0xFF && buffer[9] == 0x08)
            {
                int dispCount = BitConverter.ToInt32(buffer, 3);
                ulong countSlot = unchecked(address + 7 + (ulong)(long)dispCount);
                if (reader.ReadPointer(countSlot + 8, out ulong target) && IsValidAddress(target))
                {
                    address = target;
                    return true;
                }
                return false;
            }

            return false;
        }

        private static bool TryGetReferencedAddress(Instruction instruction, uint pointerSize, out ulong referencedAddress)
        {
            for (int i = 0; i < instruction.OpCount; i++)
            {
                switch (instruction.GetOpKind(i))
                {
                    case OpKind.NearBranch16:
                    case OpKind.NearBranch32:
                    case OpKind.NearBranch64:
                        referencedAddress = instruction.NearBranchTarget;
                        return referencedAddress > ushort.MaxValue;
                    case OpKind.Immediate16:
                    case OpKind.Immediate8to16:
                    case OpKind.Immediate8to32:
                    case OpKind.Immediate8to64:
                    case OpKind.Immediate32to64:
                    case OpKind.Immediate32 when pointerSize == 4:
                    case OpKind.Immediate64:
                        referencedAddress = instruction.GetImmediate(i);
                        return referencedAddress > ushort.MaxValue;
                    case OpKind.Memory when instruction.IsIPRelativeMemoryOperand:
                        referencedAddress = instruction.IPRelativeMemoryAddress;
                        return referencedAddress > ushort.MaxValue;
                    case OpKind.Memory:
                        referencedAddress = instruction.MemoryDisplacement64;
                        return referencedAddress > ushort.MaxValue;
                }
            }

            referencedAddress = default;
            return false;
        }

        // Recognise the common x86/x64 JMP trampoline shapes the CLR JIT emits when a call's real
        // target is out of rel32 range, and the precode/stub shapes the runtime emits as the stable
        // entry point for tiered methods (so a direct `call rel32` landing on the precode still
        // resolves to the underlying method):
        //   E9 rel32                            — JMP near rel32      (5 bytes)
        //   EB rel8                             — JMP short           (2 bytes)
        //   FF 25 disp32                        — JMP qword [rip+d]   (6 bytes, RIP-relative) — also matches FixupPrecode
        //   48 B8 imm64 ; FF E0                 — MOV rax,imm64;JMP rax (12 bytes)
        //   CallCountingStub  (template match)  — reads TargetForMethod slot at data offset 8
        //   StubPrecode       (template match)  — reads Target slot at data offset 8
        // Writes the resolved target into `target` and returns true if one matches.
        protected override bool TryFollowJumpTrampoline(State state, ulong address, out ulong target)
        {
            target = 0;
            IDataReader dataReader = state.Runtime.DataTarget.DataReader;
            byte[] buffer = new byte[19];
            int read = dataReader.Read(address, buffer);
            if (read < 2)
                return false;

            // E9 rel32 — JMP near rel32 (target = next-instr + sign_extended(rel32))
            if (read >= 5 && buffer[0] == 0xE9)
            {
                int rel = buffer[1] | (buffer[2] << 8) | (buffer[3] << 16) | (buffer[4] << 24);
                target = unchecked(address + 5 + (ulong)(long)rel);
                return IsValidAddress(target);
            }

            // EB rel8 — JMP short (target = next-instr + sign_extended(rel8))
            if (buffer[0] == 0xEB)
            {
                sbyte rel = (sbyte)buffer[1];
                target = unchecked(address + 2 + (ulong)(long)rel);
                return IsValidAddress(target);
            }

            // FF 25 disp32 — JMP qword ptr [rip+disp32]; the slot at rip+disp32 holds the actual
            // target. This also matches FixupPrecode (Target slot lives at data offset 0).
            if (read >= 6 && buffer[0] == 0xFF && buffer[1] == 0x25)
            {
                int disp = buffer[2] | (buffer[3] << 8) | (buffer[4] << 16) | (buffer[5] << 24);
                ulong slot = unchecked(address + 6 + (ulong)(long)disp);
                if (dataReader.ReadPointer(slot, out ulong slotTarget) && IsValidAddress(slotTarget))
                {
                    target = slotTarget;
                    return true;
                }
                return false;
            }

            // 48 B8 imm64 ; FF E0 — MOV rax, imm64; JMP rax
            if (read >= 12 && buffer[0] == 0x48 && buffer[1] == 0xB8 && buffer[10] == 0xFF && buffer[11] == 0xE0)
            {
                target = (ulong)buffer[2]
                    | ((ulong)buffer[3] << 8)
                    | ((ulong)buffer[4] << 16)
                    | ((ulong)buffer[5] << 24)
                    | ((ulong)buffer[6] << 32)
                    | ((ulong)buffer[7] << 40)
                    | ((ulong)buffer[8] << 48)
                    | ((ulong)buffer[9] << 56);
                return IsValidAddress(target);
            }

            // CallCountingStub / StubPrecode — both start with a MOV from a RIP-relative slot, so
            // they aren't picked up by the JMP patterns above. Match the byte template and read the
            // Target slot from the data page that sits one stub-page above the code page. Only the
            // runtime-7+ thunks use this fixed layout; older runtimes never get here.
            if (state.RuntimeVersion.Major >= 7
                && runtimeSpecificData.TryGetValue(state.RuntimeVersion, out var data))
            {
                // CallCountingStub: 10-byte template, TargetForMethod at data offset 8
                if (read >= 10 && new ReadOnlySpan<byte>(buffer, 0, 10).SequenceEqual(data.callCountingStubTemplate))
                {
                    const ulong TargetForMethodOffset = 8;
                    if (dataReader.ReadPointer(address + data.stubPageSize + TargetForMethodOffset, out target) && IsValidAddress(target))
                        return true;
                    target = 0;
                    return false;
                }

                // StubPrecode: 13-byte template, Target at data offset 8 (MethodDesc at 0)
                if (read >= 13 && new ReadOnlySpan<byte>(buffer, 0, 13).SequenceEqual(data.stubPrecodeTemplate))
                {
                    const ulong TargetOffset = 8;
                    if (dataReader.ReadPointer(address + data.stubPageSize + TargetOffset, out target) && IsValidAddress(target))
                        return true;
                    target = 0;
                    return false;
                }
            }

            return false;
        }
    }
}
