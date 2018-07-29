using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BenchmarkDotNet.Diagnosers
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
            var parsed = method.Maps.SelectMany(map => map.Instructions.Select(Parse)).ToArray();

            // if somebody is using given address as argument, the address is "used" and should have it's own label
            var usedArguments = new HashSet<string>(parsed
                .Select(
                    a => string.IsNullOrEmpty(a.extraArguments)
                        ? a.arguments
                        : a.extraArguments)
                .Where(arguments => !string.IsNullOrEmpty(arguments)).Distinct());

            var addressesToLabels = new Dictionary<string, string>();
            int currentLabelIndex = 0;
            foreach (var instruction in parsed)
                if (usedArguments.Contains(instruction.address) && !addressesToLabels.ContainsKey(instruction.address))
                    addressesToLabels.Add(instruction.address, $"{labelPrefix}_L{currentLabelIndex++:00}");

            var prettified = new List<Element>();
            foreach (var instruction in parsed)
            {
                if (!(instruction.source is Asm))
                {
                    prettified.Add(new Element(instruction.source.TextRepresentation, instruction.source));
                    continue;
                }

                if (addressesToLabels.TryGetValue(instruction.address, out string label))
                    prettified.Add(new Label(label, label));

                string argument = string.IsNullOrEmpty(instruction.extraArguments)
                    ? instruction.arguments
                    : instruction.extraArguments;
                if (argument == null)
                    throw new NullReferenceException(nameof(instruction.arguments));

                if (addressesToLabels.TryGetValue(argument, out string reference)) // it's sth like 00007ff7`ffbfd320 7cba jl      00007ff7`ffbfd2dc
                    prettified.Add(new Reference($"{PadRight(instruction.instruction)} {reference}", reference, instruction.source));
                else if (!string.IsNullOrEmpty(instruction.extraArguments) && !instruction.extraArguments.StartsWith("("))
                    prettified.Add(new Element($"{PadRight(instruction.instruction)} {instruction.arguments} {WithoutAddress(instruction.extraArguments)}", instruction.source));
                else if (!string.IsNullOrEmpty(instruction.arguments))
                    prettified.Add(new Element($"{PadRight(instruction.instruction)} {instruction.arguments}", instruction.source));
                else // sth like "ret"
                    prettified.Add(new Element(instruction.instruction, instruction.source));
            }

            return prettified;
        }

        private static (Code source, string address, string byteRepresentation, string instruction, string arguments, string extraArguments) Parse(Code code)
        {
            if (!(code is Asm asm))
                return (code, null, null, null, null, null);

            int i = 0;

            // sample input can be:
            // 00007ff7`ffbfd2da 33ff xor     edi,edi
            // 00007ff7`ffbfd2dc 48b9e04be659f87f0000 mov rcx,offset System_Private_CoreLib+0x8f4be0 (00007ff8`59e64be0)
            // 00007ff7`ffbfd2e6 e80570ae5f      call coreclr!MetaDataGetDispenser+0x72810 (00007ff8`5f6e42f0)
            // 00007ff7`ffbfd318 ffc7            inc edi
            // 00007ff7`ffbfd320 7cba jl      00007ff7`ffbfd2dc
            // 
            // so it's always something like
            // address byteRepresentation instruction   [arguments] [extraArguments]
            string address = NextWord(asm.TextRepresentation, ref i);
            string byteRepresentation = NextWord(asm.TextRepresentation, ref i);
            string instruction = NextWord(asm.TextRepresentation, ref i);

            // it's sth like 
            // call    System_Private_CoreLib+0x577460 (00007ff8`59ae7460)
            // and we can not get managed method for this address
            if (!string.IsNullOrEmpty(asm.Comment) && asm.Comment != DisassemblerConstants.NotManagedMethod)
                return (asm, address, byteRepresentation, instruction, asm.Comment, null);

            string arguments = NextWord(asm.TextRepresentation, ref i);
            string extraArguments =
                i < asm.TextRepresentation.Length
                    ? asm.TextRepresentation.Substring(startIndex: i).Trim()
                    : null;

            return (asm, address, byteRepresentation, instruction, arguments, extraArguments);
        }

        private static string NextWord(string text, ref int index)
        {
            var buffer = new StringBuilder();

            while (index < text.Length && !char.IsWhiteSpace(text[index]))
                buffer.Append(text[index++]);

            while (index < text.Length && char.IsWhiteSpace(text[index]))
                ++index; // ignore next whitespaces

            return buffer.ToString();
        }

        private static string PadRight(string instruction)
        {
            const int width = 7;
            var buffer = new StringBuilder(instruction);

            while (buffer.Length < width)
                buffer.Append(' ');

            return buffer.ToString();
        }

        private static string WithoutAddress(string extraArguments)
        {
            int startOfTheAddress = extraArguments.IndexOf("(", StringComparison.Ordinal);
            if (startOfTheAddress < 0)
                return extraArguments;

            return extraArguments.Substring(0, startOfTheAddress - 1);
        }
    }
}