using Iced.Intel;
using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;

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

                TryTranslateAddressToName(instruction, state, depth, currentMethod);

                yield return new Asm
                {
                    InstructionPointer = instruction.IP,
                    Instruction = instruction
                };
            }
        }
    }
}
