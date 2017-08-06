using System.Text;
using System.Xml.Serialization;

#pragma warning disable CS3003 // I need ulong
namespace BenchmarkDotNet.Diagnosers
{
    // keep it in sync with src\BenchmarkDotNet.Disassembler.x64\DataContracts.cs!
    public abstract class Code
    {
        public string TextRepresentation { get; set; }
        public string Comment { get; set; }
    }

    public class Sharp : Code
    {
        public string FilePath { get; set; }
        public int LineNumber { get; set; }
    }

    public class IL : Code
    {
        public int Offset { get; set; }
    }

    public class Asm : Code
    {
        /// <summary>
        /// The native start offset of this ASM representation
        /// </summary>
        public ulong StartAddress { get; set; }

        /// <summary>
        /// The native end offset of this ASM representation
        /// </summary>
        public ulong EndAddress { get; set; }
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
#pragma warning restore CS3003 // Type is not CLS-compliant