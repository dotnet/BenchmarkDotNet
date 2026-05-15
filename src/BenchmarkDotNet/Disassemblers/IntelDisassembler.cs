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
                            // Check if the target is a known stub
                            // The stubs are allocated in interleaved code / data pages in memory. The data part of the stub
                            // is at an address one memory page higher than the code.
                            byte[] buffer = new byte[10];

                            FlushCachedDataIfNeeded(state.Runtime.DataTarget.DataReader, address, buffer);

                            if (state.Runtime.DataTarget.DataReader.Read(address, buffer) == buffer.Length && buffer.SequenceEqual(data.callCountingStubTemplate))
                            {
                                const ulong TargetMethodAddressSlotOffset = 8;
                                address = state.Runtime.DataTarget.DataReader.ReadPointer(address + data.stubPageSize + TargetMethodAddressSlotOffset);
                            }
                            else
                            {
                                buffer = new byte[13];
                                if (state.Runtime.DataTarget.DataReader.Read(address, buffer) == buffer.Length && buffer.SequenceEqual(data.stubPrecodeTemplate))
                                {
                                    const ulong MethodDescSlotOffset = 0;
                                    address = state.Runtime.DataTarget.DataReader.ReadPointer(address + data.stubPageSize + MethodDescSlotOffset);
                                    isPrestubMD = true;
                                }
                                else
                                {
                                    buffer = new byte[19];
                                    if (state.Runtime.DataTarget.DataReader.Read(address, buffer) == buffer.Length && buffer.SequenceEqual(data.fixupPrecodeTemplate))
                                    {
                                        const ulong MethodDescSlotOffset = 8;
                                        address = state.Runtime.DataTarget.DataReader.ReadPointer(address + data.stubPageSize + MethodDescSlotOffset);
                                        isPrestubMD = true;
                                    }

                                }
                            }
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
        // target is out of rel32 range:
        //   E9 rel32                            — JMP near rel32      (5 bytes)
        //   EB rel8                             — JMP short           (2 bytes)
        //   FF 25 disp32                        — JMP qword [rip+d]   (6 bytes, x64 RIP-relative)
        //   48 B8 imm64 ; FF E0                 — MOV rax,imm64;JMP rax (12 bytes)
        // Writes the resolved target into `target` and returns true if one matches.
        protected override bool TryFollowJumpTrampoline(IDataReader dataReader, ulong address, out ulong target)
        {
            target = 0;
            byte[] buffer = new byte[12];
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

            // FF 25 disp32 — JMP qword ptr [rip+disp32]; the slot at rip+disp32 holds the actual target
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

            return false;
        }
    }
}
