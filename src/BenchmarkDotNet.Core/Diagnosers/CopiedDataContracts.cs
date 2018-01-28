using System.Text;
using System.Xml.Serialization;

#pragma warning disable CS3003 // I need ulong
namespace BenchmarkDotNet.Diagnosers
{
    // keep it in sync with src\BenchmarkDotNet.Disassembler.x64\DataContracts.cs!
    public class Code
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

    public class Map
    {
        [XmlArray("Instructions")]
        [XmlArrayItem(nameof(Code), typeof(Code))]
        [XmlArrayItem(nameof(Sharp), typeof(Sharp))]
        [XmlArrayItem(nameof(IL), typeof(IL))]
        [XmlArrayItem(nameof(Asm), typeof(Asm))]
        public Code[] Instructions { get; set; }
    }

    public class DisassembledMethod
    {
        public string Name { get; set; }

        public ulong NativeCode { get; set; }

        public string Problem { get; set; }

        public Map[] Maps { get; set; }

        public DisassembledMethodAnnotation Annotation { get; set; }
    }

    public class DisassemblyResult
    {
        public DisassembledMethod[] Methods { get; set; }
    }

    public class DisassembledMethodAnnotation
    {
        public int TotalBytesOfCode { get; set; }
        public bool IsOptmizedCode { get; set; }
        public bool IsFullyinterruptible { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{new string('=', 39)}Disassembly annotation{new string('=', 39)}");
            sb.AppendLine($"total bytes of code {TotalBytesOfCode}");
            sb.AppendLine((IsOptmizedCode ? "" : "non ") + "optimized code");
            sb.AppendLine(IsFullyinterruptible ? "fully interruptible" : "partially interruptible");
            sb.AppendLine(new string('=', 100));
            return sb.ToString();
        }
        
    public static class Errors
    {
        public const string NotManagedMethod = "not managed method";
    }
}
#pragma warning restore CS3003 // Type is not CLS-compliant