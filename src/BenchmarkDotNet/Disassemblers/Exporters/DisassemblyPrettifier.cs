using Iced.Intel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace BenchmarkDotNet.Disassemblers.Exporters
{
    internal static class DisassemblyPrettifier
    {
        internal class Element
        {
            internal string TextRepresentation { get; }
            internal Code Source { get; }

            public Element(string textRepresentation, Code source)
            {
                TextRepresentation = textRepresentation;
                Source = source;
            }
        }

        internal class Reference : Element
        {
            internal string Id { get; }

            public Reference(string textRepresentation, string id, Code source) : base(textRepresentation, source) => Id = id;
        }

        internal class Label : Element
        {
            internal string Id { get; }

            public Label(string textRepresentation, string id) : base(textRepresentation, null) => Id = id;
        }

        internal static IReadOnlyList<Element> Prettify(DisassembledMethod method, string labelPrefix)
        {
            var asmInstructions = method.Maps.SelectMany(map => map.Instructions.OfType<Asm>()).ToArray();
            var jumpAndCallAddresses = asmInstructions
                .Where(asm => AsmProvider.TryGetAddress(asm.Instruction, method.PointerSize, out _))
                .Select(asm => AsmProvider.TryGetAddress(asm.Instruction, method.PointerSize, out ulong address) ? address : default)
                .Distinct()
                .ToImmutableHashSet();

            var addressesToLabels = new Dictionary<ulong, string>();
            int currentLabelIndex = 0;
            foreach (var instruction in asmInstructions)
                if (jumpAndCallAddresses.Contains(instruction.StartAddress) && !addressesToLabels.ContainsKey(instruction.StartAddress))
                    addressesToLabels.Add(instruction.StartAddress, $"{labelPrefix}_L{currentLabelIndex++:00}");

            var prettified = new List<Element>();
            var formatter = ClrMdDisassembler.CreateFormatter();
            var formatterOutput = new StringBuilderFormatterOutput();
            foreach (var map in method.Maps)
                foreach (var instruction in map.Instructions)
                {
                    if (!(instruction is Asm asm))
                    {
                        prettified.Add(new Element(instruction.TextRepresentation, instruction));
                        continue;
                    }

                    if (addressesToLabels.TryGetValue(instruction.StartAddress, out string label))
                    {
                        prettified.Add(new Label(label, label));
                    }

                    if (AsmProvider.TryGetAddress(asm.Instruction, method.PointerSize, out ulong jumpAddress))
                    {
                        if (addressesToLabels.TryGetValue(jumpAddress, out string translated)) // jump or a call within same method
                        {
                            formatter.FormatMnemonic(asm.Instruction, formatterOutput);
                            prettified.Add(new Reference($"{formatterOutput.ToStringAndReset().PadRight(ClrMdDisassembler.FirstOperandCharIndex - 1)} {translated}", translated, asm));
                            continue;
                        }

                        if (!string.IsNullOrEmpty(asm.Comment)) // call (Comment contains method name)
                        {
                            formatter.FormatMnemonic(asm.Instruction, formatterOutput);
                            prettified.Add(new Element($"{formatterOutput.ToStringAndReset().PadRight(ClrMdDisassembler.FirstOperandCharIndex - 1)} {asm.Comment}", asm));
                            continue;
                        }
                    }

                    formatter.Format(asm.Instruction, formatterOutput);
                    prettified.Add(new Element(formatterOutput.ToStringAndReset(), asm));
                }

            return prettified;
        }
    }
}