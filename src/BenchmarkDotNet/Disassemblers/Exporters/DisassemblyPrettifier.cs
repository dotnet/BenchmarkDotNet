using BenchmarkDotNet.Diagnosers;
using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.Disassemblers.Exporters
{
    internal static class DisassemblyPrettifier
    {
        internal class Element
        {
            internal string TextRepresentation { get; }
            internal SourceCode Source { get; }

            public Element(string textRepresentation, SourceCode source)
            {
                TextRepresentation = textRepresentation;
                Source = source;
            }
        }

        internal class Reference : Element
        {
            internal string Id { get; }

            public Reference(string textRepresentation, string id, SourceCode source) : base(textRepresentation, source) => Id = id;
        }

        internal class Label : Element
        {
            internal string Id { get; }

            public Label(string label) : base(label, null) => Id = label;
        }

        internal static IReadOnlyList<Element> Prettify(DisassembledMethod method, DisassemblyResult disassemblyResult, DisassemblyDiagnoserConfig config, string labelPrefix)
        {
            var asmInstructions = method.Maps.SelectMany(map => map.SourceCodes.OfType<Asm>()).ToArray();

            // first of all, we search of referenced addresses (jump|calls)
            var referencedAddresses = new HashSet<ulong>();
            foreach (var asm in asmInstructions)
                if (asm.ReferencedAddress != null)
                {
                    referencedAddresses.Add(asm.ReferencedAddress.Value);
                }

            // for every IP that is referenced, we emit a uinque label
            var addressesToLabels = new Dictionary<ulong, string>();
            int currentLabelIndex = 0;
            foreach (var instruction in asmInstructions)
                if (referencedAddresses.Contains(instruction.InstructionPointer) && !addressesToLabels.ContainsKey(instruction.InstructionPointer))
                    addressesToLabels.Add(instruction.InstructionPointer, $"{labelPrefix}_L{currentLabelIndex++:00}");

            var formatterWithLabelsSymbols = config.GetFormatterWithSymbolSolver(addressesToLabels);
            var formatterWithGlobalSymbols = config.GetFormatterWithSymbolSolver(disassemblyResult.AddressToNameMapping);

            var prettified = new List<Element>();
            foreach (var map in method.Maps)
                foreach (var instruction in map.SourceCodes)
                {
                    if (instruction is Sharp sharp)
                    {
                        prettified.Add(new Element(sharp.Text, sharp));
                    }
                    else if (instruction is MonoCode mono)
                    {
                        prettified.Add(new Element(mono.Text, mono));
                    }
                    else if (instruction is Asm asm)
                    {
                        // this IP is referenced by some jump|call, so we add a label
                        if (addressesToLabels.TryGetValue(asm.InstructionPointer, out string label))
                        {
                            prettified.Add(new Label(label));
                        }

                        if (asm.ReferencedAddress != null)
                        {
                            ulong referencedAddress = asm.ReferencedAddress.Value;
                            // jump or a call within same method
                            if (addressesToLabels.TryGetValue(referencedAddress, out string translated))
                            {
                                prettified.Add(new Reference(CodeFormatter.Format(asm, formatterWithLabelsSymbols, config.PrintInstructionAddresses, disassemblyResult.PointerSize, addressesToLabels), translated, asm));
                                continue;
                            }

                            // call to a known method
                            if (disassemblyResult.AddressToNameMapping.ContainsKey(referencedAddress))
                            {
                                string comment = string.Empty;
                                if (asm.IsReferencedAddressIndirect)
                                {
                                    comment = "; " + disassemblyResult.AddressToNameMapping[referencedAddress];
                                }
                                prettified.Add(new Element(CodeFormatter.Format(asm, formatterWithGlobalSymbols, config.PrintInstructionAddresses, disassemblyResult.PointerSize, disassemblyResult.AddressToNameMapping) + comment, asm));
                                continue;
                            }
                        }

                        prettified.Add(new Element(CodeFormatter.Format(asm, formatterWithGlobalSymbols, config.PrintInstructionAddresses, disassemblyResult.PointerSize, disassemblyResult.AddressToNameMapping), asm));
                    }
                }

            return prettified;
        }
    }
}