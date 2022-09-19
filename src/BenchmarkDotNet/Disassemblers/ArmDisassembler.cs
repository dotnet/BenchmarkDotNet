using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.Disassemblers
{
    internal class ArmDisassembler : ClrMdV2Disassembler
    {
        protected override IEnumerable<Asm> Decode(byte[] code, ulong startAddress, State state, int depth, ClrMethod currentMethod)
        {
            Console.WriteLine($"Was asked to decode {currentMethod.Signature} from {code.Length} byte array ({string.Join(",", code.Select(b => b.ToString("X")))})");

            yield break;
        }
    }
}
