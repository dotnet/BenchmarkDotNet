using AsmArm64;
using Iced.Intel;
using Microsoft.Diagnostics.Runtime.Interfaces;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace BenchmarkDotNet.Disassemblers;

// Based on Capstone definition..
public enum DisassembleSyntax
{
    Default = Masm,

    // x64/x86
    Att = 1,
    Intel = 2,
    Masm = 3,

    // Following enum definitions are added for arm64 platform
    Gas = 11,  // (Default) GNU assembler inspired style
    Llvm = 12, // LLVM inspired style
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(Sharp), typeDiscriminator: nameof(Sharp))]
[JsonDerivedType(typeof(IntelAsm), typeDiscriminator: nameof(IntelAsm))]
[JsonDerivedType(typeof(Arm64Asm), typeDiscriminator: nameof(Arm64Asm))]
[JsonDerivedType(typeof(MonoCode), typeDiscriminator: nameof(MonoCode))]
public abstract class SourceCode
{
    // Closed hierarchy.
    internal SourceCode() { }

    public ulong InstructionPointer { get; set; }
}

public sealed class Sharp : SourceCode
{
    public required string Text { get; set; }
    public required string FilePath { get; set; }
    public int LineNumber { get; set; }
}

public abstract class Asm : SourceCode
{
    // Closed hierarchy.
    internal Asm() { }

    public int InstructionLength { get; set; }
    public ulong? ReferencedAddress { get; set; }
    public bool IsReferencedAddressIndirect { get; set; }
}

public sealed class IntelAsm : Asm
{
    [JsonConverter(typeof(IntelInstructionConverter))]
    public Instruction Instruction { get; set; }

    public override string ToString() => Instruction.ToString();

    // Instruction can't be round-tripped through its public properties: several of them alias the same
    // private field at different widths (IP16/IP32/IP, NearBranch16/32/64, MemoryDisplacement32/64, ...),
    // so whichever setter runs last truncates the value. Encode it to machine code instead and decode it back.
    internal sealed class IntelInstructionConverter : JsonConverter<Instruction>
    {
        private const string CodeSizeKey = "codeSize";
        private const string AddressKey = "address";
        private const string LengthKey = "length";
        private const string BytesKey = "bytes";

        public override Instruction Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            CodeSize codeSize = CodeSize.Unknown;
            ulong address = 0;
            int length = 0;
            byte[] bytes = [];

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException();

                string propertyName = reader.GetString()!;
                reader.Read();

                switch (propertyName)
                {
                    case CodeSizeKey:
                        codeSize = Enum.Parse<CodeSize>(reader.GetString()!);
                        break;
                    case AddressKey:
                        address = reader.GetUInt64();
                        break;
                    case LengthKey:
                        length = reader.GetInt32();
                        break;
                    case BytesKey:
                        bytes = reader.GetBytesFromBase64();
                        break;
                    default:
                        throw new NotSupportedException($"Unknown property({propertyName}) found.");
                }
            }

            if (reader.TokenType != JsonTokenType.EndObject)
                throw new JsonException("Invalid JSON");

            // No bytes means the instruction was not encodable (e.g. the decoder hit invalid bytes), so only its location is kept.
            Instruction instruction = default;
            if (bytes.Length > 0)
            {
                var decoder = Iced.Intel.Decoder.Create(GetBitness(codeSize), new ByteArrayCodeReader(bytes));
                decoder.IP = address;
                decoder.Decode(out instruction);
            }

            // Restored as written, because an instruction that was created rather than decoded has no length or code size.
            instruction.CodeSize = codeSize;
            instruction.Length = length;
            instruction.NextIP = address + (ulong) length;
            return instruction;
        }

        public override void Write(Utf8JsonWriter writer, Instruction value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(CodeSizeKey, value.CodeSize.ToString());
            writer.WriteNumber(AddressKey, value.IP);
            writer.WriteNumber(LengthKey, value.Length);

            var codeWriter = new ByteListCodeWriter();
            if (value.Code != Iced.Intel.Code.INVALID && Encoder.Create(GetBitness(value.CodeSize), codeWriter).TryEncode(value, value.IP, out _, out _))
            {
                writer.WriteBase64String(BytesKey, codeWriter.Bytes.ToArray());
            }
            writer.WriteEndObject();
        }

        private static int GetBitness(CodeSize codeSize) => codeSize switch
        {
            CodeSize.Code16 => 16,
            CodeSize.Code32 => 32,
            _ => 64,
        };

        private sealed class ByteListCodeWriter : CodeWriter
        {
            internal List<byte> Bytes { get; } = [];

            public override void WriteByte(byte value) => Bytes.Add(value);
        }
    }
}

public sealed class Arm64Asm : Asm
{
    public Arm64Asm()
    {
        InstructionLength = 4;
    }

    public required uint RawInstruction { get; init; }

    public required DisassembleSyntax DisassembleSyntax { get; init; }

    private Arm64Instruction? _instruction;

    [JsonIgnore]
    public Arm64Instruction Instruction
    {
        get
        {
            _instruction ??= Arm64Instruction.Decode(RawInstruction);
            return _instruction.Value;
        }
    }

    public override string ToString()
    {
        var options = Arm64InstructionFormatter.GetFormattingOptions(Instruction);
        return Instruction.ToString(options);
    }
}

public sealed class MonoCode : SourceCode
{
    public string Text { get; set; } = "";
}

public sealed class Map
{
    public SourceCode[] SourceCodes { get; set; } = [];
}

public sealed class DisassembledMethod
{
    public string Name { get; set; } = "";

    public ulong NativeCode { get; set; }

    public string Problem { get; set; } = "";

    public Map[] Maps { get; set; } = [];

    public string CommandLine { get; set; } = "";

    public static DisassembledMethod Empty(string fullSignature, ulong nativeCode, string problem)
        => new()
        {
            Name = fullSignature,
            NativeCode = nativeCode,
            Problem = problem
        };
}

public sealed class DisassemblyResult
{
    public DisassembledMethod[] Methods { get; set; } = [];
    public string[] Errors { get; set; } = [];

    public uint PointerSize { get; set; }

    public Dictionary<ulong, string> AddressToNameMapping { get; set; } = [];
}

internal sealed class State
{
    internal State(IClrRuntime runtime, Version runtimeVersion)
    {
        Runtime = runtime;
        Todo = new Queue<MethodInfo>();
        HandledMethods = new HashSet<IClrMethod>(new IClrMethodComparer());
        AddressToNameMapping = [];
        RuntimeVersion = runtimeVersion;
    }

    internal IClrRuntime Runtime { get; }
    internal Queue<MethodInfo> Todo { get; }
    internal HashSet<IClrMethod> HandledMethods { get; }
    internal Dictionary<ulong, string> AddressToNameMapping { get; }
    internal Version RuntimeVersion { get; }

    private sealed class IClrMethodComparer : IEqualityComparer<IClrMethod>
    {
        public bool Equals(IClrMethod? x, IClrMethod? y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (x is null || y is null)
                return false;

            return x.NativeCode == y.NativeCode;
        }

        public int GetHashCode(IClrMethod obj) => (int) obj.NativeCode;
    }
}

internal readonly struct MethodInfo // I am not using ValueTuple here (would be perfect) to keep the number of dependencies as low as possible
{
    internal IClrMethod Method { get; }
    internal int Depth { get; }

    internal MethodInfo(IClrMethod method, int depth)
    {
        Method = method;
        Depth = depth;
    }
}
