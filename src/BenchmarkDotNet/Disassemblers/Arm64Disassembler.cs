using Gee.External.Capstone;
using Gee.External.Capstone.Arm64;
using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.Disassemblers
{
    internal class Arm64Disassembler : ClrMdV2Disassembler<Arm64Instruction>
    {
        protected override IEnumerable<Asm> Decode(byte[] code, ulong startAddress, State state, int depth, ClrMethod currentMethod)
        {
            Console.WriteLine($"Was asked to decode {currentMethod.Signature} from {code.Length} byte array ({string.Join(",", code.Select(b => b.ToString("X")))})");

            const Arm64DisassembleMode disassembleMode = Arm64DisassembleMode.Arm;
            using (CapstoneArm64Disassembler disassembler = CapstoneDisassembler.CreateArm64Disassembler(disassembleMode))
            {
                // Enables disassemble details, which are disabled by default, to provide more detailed information on
                // disassembled binary code.
                disassembler.EnableInstructionDetails = true;
                disassembler.DisassembleSyntax = DisassembleSyntax.Intel;

                Arm64Instruction[] instructions = disassembler.Disassemble(code, (long)startAddress);
                foreach (Arm64Instruction instruction in instructions)
                {
                    yield return new Asm()
                    {
                        InstructionPointer = (ulong)instruction.Address,
                        InstructionLength = instruction.Bytes.Length,
                        Arm64Instruction = instruction
                    };
                }
            }
        }

        protected override bool TryGetReferencedAddressT(Arm64Instruction instruction, uint pointerSize, out ulong referencedAddress)
            => TryGetReferencedAddress(instruction, pointerSize, out referencedAddress);

        internal static bool TryGetReferencedAddress(Arm64Instruction instruction, uint pointerSize, out ulong referencedAddress)
        {
            if (instruction.Details.BelongsToGroup(Arm64InstructionGroupId.ARM64_GRP_BRANCH_RELATIVE))
            {
                // One of the operands is the address
                for (int i = 0; i < instruction.Details.Operands.Length; i++)
                {
                    if (instruction.Details.Operands[i].Type == Arm64OperandType.Immediate)
                    {
                        referencedAddress = (ulong)instruction.Details.Operands[i].Immediate;
                        return true;
                    }
                }
            }
            referencedAddress = 0;
            return false;
        }
    }
}
