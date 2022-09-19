using Iced.Intel;
using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;

namespace BenchmarkDotNet.Disassemblers
{
    internal class IntelDisassembler : ClrMdV2Disassembler<Instruction>
    {
        protected override IEnumerable<Asm> Decode(byte[] code, ulong startAddress, State state, int depth, ClrMethod currentMethod)
        {
            var reader = new ByteArrayCodeReader(code);
            var decoder = Decoder.Create(state.Runtime.DataTarget.DataReader.PointerSize * 8, reader);
            decoder.IP = startAddress;

            while (reader.CanReadByte)
            {
                decoder.Decode(out var instruction);

                TryTranslateAddressToName(instruction, state, depth, currentMethod);

                yield return new Asm
                {
                    InstructionPointer = instruction.IP,
                    InstructionLength = instruction.Length,
                    IntelInstruction = instruction
                };
            }
        }

        protected override bool TryGetReferencedAddressT(Instruction instruction, uint pointerSize, out ulong referencedAddress)
            => TryGetReferencedAddress(instruction, pointerSize, out referencedAddress);

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
