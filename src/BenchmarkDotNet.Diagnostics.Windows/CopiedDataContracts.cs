using System.Text;
using System.Xml.Serialization;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    // keep it in sync with src\BenchmarkDotNet.Disassembler.x64\DataContracts.cs!
    public abstract class Code
    {
        public string TextRepresentation { get; set; }
        public string Comment { get; set; }
    }

    public class Sharp : Code { }

    public class IL : Code { }

    public class Asm : Code
    {
        public ulong InstructionPointer { get; set; }
    }

    public class DisassembledMethod
    {
        public string Name { get; set; }

        public ulong NativeCode { get; set; }

        public string Problem { get; set; }

        [XmlArray("Instructions")]
        [XmlArrayItem(nameof(Sharp), typeof(Sharp))]
        [XmlArrayItem(nameof(IL), typeof(IL))]
        [XmlArrayItem(nameof(Asm), typeof(Asm))]
        public Code[] Instructions { get; set; }
    }

    public class DisassemblyResult
    {
        public DisassembledMethod[] Methods { get; set; }

        public override string ToString()
        {
            var buffer = new StringBuilder(20000);

            foreach (var method in Methods)
            {
                buffer.AppendLine($"{method.Instructions} {method.Name}");

                foreach (var instruction in method.Instructions)
                {
                    buffer.AppendLine(instruction.TextRepresentation);
                }
            }

            return buffer.ToString();
        }
    }
}
