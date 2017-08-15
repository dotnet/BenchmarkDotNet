using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace BenchmarkDotNet.Disassembler
{
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
        public List<Code> Instructions { get; set; }
    }

    public class DisassembledMethod
    {
        public string Name { get; set; }

        public ulong NativeCode { get; set; }

        public string Problem { get; set; }

        public Map[] Maps { get; set; }

        public static DisassembledMethod Empty(string fullSignature, ulong nativeCode, string problem)
            => new DisassembledMethod
            {
                Name = fullSignature,
                NativeCode = nativeCode,
                Maps = Array.Empty<Map>(),
                Problem = problem
            };
    }

    public class DisassemblyResult
    {
        public DisassembledMethod[] Methods { get; set; }
    }
}