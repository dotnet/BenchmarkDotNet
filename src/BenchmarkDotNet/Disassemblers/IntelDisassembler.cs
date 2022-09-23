using BenchmarkDotNet.Environments;
using Iced.Intel;
using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace BenchmarkDotNet.Disassemblers
{
    internal class IntelDisassembler : ClrMdV2Disassembler
    {
        protected override IEnumerable<Asm> Decode(byte[] code, ulong startAddress, State state, int depth, ClrMethod currentMethod)
        {
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
                        if (state.Runtime.ClrInfo.Version.Major >= 7)
                        {
                            const ulong PageSize = 4096;
                            // Check if the target is a call counting stub and if it is, extract the real target.
                            // The call counting stub starts like this:
                            // mov rax, qword ptr [rip+0xff9]
                            // dec word ptr [rax]
                            byte[] buffer = new byte[10];
                            byte[] callCountingStub = new byte[10] { 0x48, 0x8b, 0x05, 0xf9, 0x0f, 0x00, 0x00, 0x66, 0xff, 0x08 };

                            if (state.Runtime.DataTarget.DataReader.Read(address, buffer) == buffer.Length && buffer.SequenceEqual(callCountingStub))
                            {
                                const ulong TargetMethodAddressSlotOffset = 8;
                                // The call counting stubs are allocated in interleaved code / data pages in memory. The data part of the stub
                                // is at an address one memory page higher than the code.
                                address = state.Runtime.DataTarget.DataReader.ReadPointer(address + PageSize + TargetMethodAddressSlotOffset);
                            }
                            else
                            {
                                buffer = new byte[13];
                                // 00000001`80157720 4c8b15f90f0000  mov     r10,qword ptr [coreclr!__scrt_initialize_onexit_tables+0x20 (00000001`80158720)]
                                // 00000001`80157727 ff25fb0f0000 jmp     qword ptr[coreclr!__scrt_initialize_onexit_tables + 0x28(00000001`80158728)]
                                byte[] stubPrecode = new byte[13] { 0x4c, 0x8b, 0x15, 0xf9, 0x0f, 0x00, 0x00, 0xff, 0x25, 0xfb, 0x0f, 0x00, 0x00 };
                                if (state.Runtime.DataTarget.DataReader.Read(address, buffer) == buffer.Length && buffer.SequenceEqual(stubPrecode))
                                {
                                    //Console.WriteLine("Found StubPrecode");
                                    const ulong MethodDescSlotOffset = 0;
                                    address = state.Runtime.DataTarget.DataReader.ReadPointer(address + PageSize + MethodDescSlotOffset);
                                    isPrestubMD = true;
                                }
                                else
                                {
                                    // 00000001`80157730 ff25fa0f0000    jmp     qword ptr [coreclr!__scrt_initialize_onexit_tables+0x30 (00000001`80158730)]
                                    // 00000001`80157736 4c8b15fb0f0000 mov     r10,qword ptr[coreclr!__scrt_initialize_onexit_tables + 0x38(00000001`80158738)]
                                    // 00000001`8015773d ff25fd0f0000 jmp     qword ptr[coreclr!__scrt_initialize_onexit_tables + 0x40(00000001`80158740)]
                                    buffer = new byte[19];
                                    byte[] fixupPrecode = new byte[19] { 0xff, 0x25, 0xfa, 0x0f, 0x00, 0x00, 0x4c, 0x8b, 0x15, 0xfb, 0x0f, 0x00, 0x00, 0xff, 0x25, 0xfd, 0x0f, 0x00, 0x00 };
                                    if (state.Runtime.DataTarget.DataReader.Read(address, buffer) == buffer.Length && buffer.SequenceEqual(fixupPrecode))
                                    {
                                        //Console.WriteLine("Found FixupPrecode");
                                        const ulong MethodDescSlotOffset = 8;
                                        address = state.Runtime.DataTarget.DataReader.ReadPointer(address + PageSize + MethodDescSlotOffset);
                                        isPrestubMD = true;
                                    }

                                }
                            }
                        }
                    }

                    if (address > ushort.MaxValue)
                    {
                        TryTranslateAddressToName(address, isPrestubMD, state, isIndirect, depth, currentMethod);
                    }
                }

                yield return new Asm
                {
                    InstructionPointer = instruction.IP,
                    InstructionLength = instruction.Length,
                    IntelInstruction = instruction,
                    ReferencedAddress = (address > ushort.MaxValue) ? address : null,
                    IsReferencedAddressIndirect = isIndirect,
                };
            }
        }

        internal static bool TryGetReferencedAddress(Instruction instruction, uint pointerSize, out ulong referencedAddress)
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
    }
}
