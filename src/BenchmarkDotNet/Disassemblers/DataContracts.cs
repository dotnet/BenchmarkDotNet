using Gee.External.Capstone;
using Gee.External.Capstone.Arm64;
using Iced.Intel;
using Microsoft.Diagnostics.Runtime;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;


#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace BenchmarkDotNet.Disassemblers;

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

#if NET6_0_OR_GREATER
// There are way too many properties to serialize them manually.
// Ensure the Instruction's properties are not trimmed.
[method: DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Instruction))]
#endif
public sealed class IntelAsm() : Asm
{
    public Instruction Instruction { get; set; }

    public override string ToString() => Instruction.ToString();
}

public sealed class Arm64Asm : Asm
{
    [JsonInclude]
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal Arm64AsmData Data { get; set; }

    [JsonIgnore]
    public Arm64Instruction? Instruction
    {
        get => Data.Instruction;
        set => Data = Data with { Instruction = value };
    }

    [JsonIgnore]
    internal DisassembleSyntax DisassembleSyntax
    {
        get => Data.DisassembleSyntax;
        set => Data = Data with { DisassembleSyntax = value };
    }

    public override string ToString() => Instruction?.ToString() ?? "";

    // Wrapper class to hold Arm64 instruction and disassemble syntax.
    [JsonConverter(typeof(Arm64AsmDataConverter))]
    internal record struct Arm64AsmData
    {
        internal DisassembleSyntax DisassembleSyntax { get; set; }

        public Arm64Instruction? Instruction { get; set; }
    }

    // Custom JsonConverter for Arm64AsmData.
    internal class Arm64AsmDataConverter : JsonConverter<Arm64AsmData>
    {
        private const string Arm64AddressKey = "arm64Address";
        private const string Arm64BytesKey = "arm64Bytes";
        private const string Arm64SyntaxKey = "arm64Syntax";

        public override Arm64AsmData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            long instructionAddress = default;
            byte[] instructionBytes = [];
            DisassembleSyntax syntax = DisassembleSyntax.Masm;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException();

                string propertyName = reader.GetString()!;
                reader.Read();

                switch (propertyName)
                {
                    case Arm64AddressKey:
                        instructionAddress = reader.GetInt64();
                        break;

                    case Arm64BytesKey:
                        instructionBytes = reader.GetBytesFromBase64();
                        break;

                    case Arm64SyntaxKey:
                        var syntaxValue = reader.GetString()!;
                        syntax = syntaxValue switch
                        {
                            "Intel" => DisassembleSyntax.Intel,
                            "Att" => DisassembleSyntax.Att,
                            "Masm" => DisassembleSyntax.Masm,
                            _ => DisassembleSyntax.Masm,
                        };
                        break;

                    default:
                        throw new NotSupportedException($"Unknown property({propertyName}) found.");
                }
            }

            if (reader.TokenType != JsonTokenType.EndObject)
                throw new JsonException("Invalid JSON");

            using var disassembler = CapstoneDisassembler.CreateArm64Disassembler(Arm64DisassembleMode.Arm);
            disassembler.EnableInstructionDetails = true;
            disassembler.DisassembleSyntax = syntax;
            var instruction = disassembler.Disassemble(instructionBytes, instructionAddress).SingleOrDefault();

            return new Arm64AsmData
            {
                DisassembleSyntax = syntax,
                Instruction = instruction,
            };
        }

        public override void Write(Utf8JsonWriter writer, Arm64AsmData value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(Arm64SyntaxKey, value.DisassembleSyntax.ToString());
            var instruction = value.Instruction;
            if (instruction != null)
            {
                writer.WriteNumber(Arm64AddressKey, instruction.Address);
                writer.WriteBase64String(Arm64BytesKey, instruction.Bytes);
            }
            writer.WriteEndObject();
        }
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

public static class DisassemblerConstants
{
    public const string DisassemblerEntryMethodName = "__ForDisassemblyDiagnoser__";
}

internal sealed class State
{
    internal State(ClrRuntime runtime, string targetFrameworkMoniker)
    {
        Runtime = runtime;
        Todo = new Queue<MethodInfo>();
        HandledMethods = new HashSet<ClrMethod>(new ClrMethodComparer());
        AddressToNameMapping = new Dictionary<ulong, string>();
        RuntimeVersion = ParseVersion(targetFrameworkMoniker);
    }

    internal ClrRuntime Runtime { get; }
    internal Queue<MethodInfo> Todo { get; }
    internal HashSet<ClrMethod> HandledMethods { get; }
    internal Dictionary<ulong, string> AddressToNameMapping { get; }
    internal Version RuntimeVersion { get; }

    internal static Version ParseVersion(string targetFrameworkMoniker)
    {
        int firstDigit = -1, lastDigit = -1;
        for (int i = 0; i < targetFrameworkMoniker.Length; i++)
        {
            if (char.IsDigit(targetFrameworkMoniker[i]))
            {
                if (firstDigit == -1)
                    firstDigit = i;

                lastDigit = i;
            }
            else if (targetFrameworkMoniker[i] == '-')
            {
                break; // it can be platform specific like net7.0-windows8
            }
        }

        string versionToParse = targetFrameworkMoniker.Substring(firstDigit, lastDigit - firstDigit + 1);
        if (!versionToParse.Contains(".")) // Full .NET Framework (net48 etc)
            versionToParse = string.Join(".", versionToParse.ToCharArray());

        return Version.Parse(versionToParse);
    }

    private sealed class ClrMethodComparer : IEqualityComparer<ClrMethod>
    {
        public bool Equals(ClrMethod? x, ClrMethod? y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (x is null || y is null)
                return false;

            return x.NativeCode == y.NativeCode;
        }

        public int GetHashCode(ClrMethod obj) => (int)obj.NativeCode;
    }
}

internal readonly struct MethodInfo // I am not using ValueTuple here (would be perfect) to keep the number of dependencies as low as possible
{
    internal ClrMethod Method { get; }
    internal int Depth { get; }

    internal MethodInfo(ClrMethod method, int depth)
    {
        Method = method;
        Depth = depth;
    }
}