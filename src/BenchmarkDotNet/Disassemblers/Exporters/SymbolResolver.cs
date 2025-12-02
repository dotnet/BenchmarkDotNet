using Iced.Intel;
using System.Collections.Generic;

#nullable enable

namespace BenchmarkDotNet.Disassemblers.Exporters
{
    internal sealed class SymbolResolver : ISymbolResolver
    {
        private readonly IReadOnlyDictionary<ulong, string> _mappings;

        public SymbolResolver(IReadOnlyDictionary<ulong, string> mappings) => _mappings = mappings;

        public bool TryGetSymbol(in Instruction instruction, int operand, int instructionOperand, ulong address, int addressSize, out SymbolResult symbol)
        {
            if (_mappings.TryGetValue(address, out var text))
            {
                symbol = new SymbolResult(address, text);
                return true;
            }

            symbol = default;
            return false;
        }
    }
}
