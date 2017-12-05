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

            public Element(string textRepresentation) => TextRepresentation = textRepresentation;
        }

        internal class Reference : Element
        {
            internal string Id { get; }

            public Reference(string textRepresentation, string id) : base(textRepresentation) => Id = id;
        }

        internal class Label : Element
        {
            internal string Id { get; }

            public Label(string textRepresentation, string id) : base(textRepresentation) => Id = id;
        }

        internal static IReadOnlyList<Element> Prettify(DisassembledMethod method, string labelPrefix)
        {
            var parsed = method.Maps.SelectMany(map => map.Instructions.OfType<Asm>().Select(Parse)).ToArray();

            // if somebody is using given address as arugment, the address is "used" and should have it's own label
            var usedAddresses = new HashSet<string>(parsed
                .Select(
                    a => string.IsNullOrEmpty(a.extraArguments)
                        ? a.arguments
                        : a.extraArguments)
                .Where(a => !string.IsNullOrEmpty(a)).Distinct());

            var addressesToLabels = new Dictionary<string, string>();
            var prettified = new List<Element>();

            int currentLabelIndex = 0;
            foreach (var instruction in parsed)
                if (usedAddresses.Contains(instruction.address) && !addressesToLabels.ContainsKey(instruction.address))
                    addressesToLabels.Add(instruction.address, $"{labelPrefix}_L{currentLabelIndex++:00}");

            for (int i = 0; i < parsed.Length; i++)
            {
                var instruction = parsed[i];

                if (addressesToLabels.TryGetValue(instruction.address, out var label))
                    prettified.Add(new Label(label, label));

                var argument = string.IsNullOrEmpty(instruction.extraArguments)
                    ? instruction.arguments
                    : instruction.extraArguments;

                if (addressesToLabels.TryGetValue(argument, out var reference)) // it's sth like 00007ff7`ffbfd320 7cba jl      00007ff7`ffbfd2dc
                    prettified.Add(new Reference($"{PadRight(instruction.instruction)} {reference}", reference));
                else if (!string.IsNullOrEmpty(instruction.extraArguments) && !instruction.extraArguments.StartsWith("("))
                    prettified.Add(new Element($"{PadRight(instruction.instruction)} {instruction.arguments} {WithoutAddress(instruction.extraArguments)}"));
                else if (!string.IsNullOrEmpty(instruction.arguments))
                    prettified.Add(new Element($"{PadRight(instruction.instruction)} {instruction.arguments}"));
                else // sth like "ret"
                    prettified.Add(new Element(instruction.instruction));
            }

            return prettified;
        }

        private static (string address, string byteRepresentation, string instruction, string arguments, string extraArguments) Parse(Asm asm)
        {
            string NextWord(string text, ref int index)
            {
                var buffer = new StringBuilder();

                while (index < text.Length && !char.IsWhiteSpace(text[index]))
                    buffer.Append(text[index++]);

                while (index < text.Length && char.IsWhiteSpace(text[index]))
                    ++index; // ignore next whitespaces

                return buffer.ToString();
            }

            int i = 0;

            // sample input can be:
            // 00007ff7`ffbfd2da 33ff xor     edi,edi
            // 00007ff7`ffbfd2dc 48b9e04be659f87f0000 mov rcx,offset System_Private_CoreLib+0x8f4be0 (00007ff8`59e64be0)
            // 00007ff7`ffbfd2e6 e80570ae5f      call coreclr!MetaDataGetDispenser+0x72810 (00007ff8`5f6e42f0)
            // 00007ff7`ffbfd318 ffc7            inc edi
            // 00007ff7`ffbfd320 7cba jl      00007ff7`ffbfd2dc
            // 
            // so it's always something like
            // address byteRepresentation instruction   arguments [methodName]
            string address = NextWord(asm.TextRepresentation, ref i);
            string byteRepresentation = NextWord(asm.TextRepresentation, ref i);
            string instruction = NextWord(asm.TextRepresentation, ref i);

            // it's sth like 
            // call    System_Private_CoreLib+0x577460 (00007ff8`59ae7460)
            // and we can not get managed method for this address
            if (!string.IsNullOrEmpty(asm.Comment) && asm.Comment != Errors.NotManagedMethod)
                return (address, byteRepresentation, instruction, asm.Comment, null);

            string arguments = NextWord(asm.TextRepresentation, ref i);
            string extraArguments =
                i < asm.TextRepresentation.Length
                    ? asm.TextRepresentation.Substring(startIndex: i).Trim()
                    : null;

            return (address, byteRepresentation, instruction, arguments, extraArguments);
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
            int startOfTheAddress = extraArguments.IndexOf("(");
            if (startOfTheAddress < 0)
                return extraArguments;

            return extraArguments.Substring(0, startOfTheAddress - 1);
        }
    }
}