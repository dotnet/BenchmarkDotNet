using Gee.External.Capstone;
using Gee.External.Capstone.Arm64;
using Iced.Intel;
using Microsoft.Diagnostics.Runtime;
using SimpleJson;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Serialization;

namespace BenchmarkDotNet.Disassemblers;

public abstract class SourceCode
{
    // Closed hierarchy.
    internal SourceCode() { }

    public ulong InstructionPointer { get; set; }

    internal JsonObject Serialize()
    {
        var json = new JsonObject { ["$type"] = GetType().Name };
        Serialize(json);
        return json;
    }

    private protected virtual void Serialize(JsonObject json)
    {
        json[nameof(InstructionPointer)] = InstructionPointer.ToString();
    }

    internal virtual void Deserialize(JsonObject json)
    {
        InstructionPointer = ulong.Parse((string) json[nameof(InstructionPointer)]);
    }
}

public sealed class Sharp : SourceCode
{
    public string Text { get; set; }
    public string FilePath { get; set; }
    public int LineNumber { get; set; }

    private protected override void Serialize(JsonObject json)
    {
        base.Serialize(json);

        json[nameof(Text)] = Text;
        json[nameof(FilePath)] = FilePath;
        json[nameof(LineNumber)] = LineNumber;
    }

    internal override void Deserialize(JsonObject json)
    {
        base.Deserialize(json);

        Text = (string) json[nameof(Text)];
        FilePath = (string) json[nameof(FilePath)];
        LineNumber = Convert.ToInt32(json[nameof(LineNumber)]);
    }
}

public abstract class Asm : SourceCode
{
    // Closed hierarchy.
    internal Asm() { }

    public int InstructionLength { get; set; }
    public ulong? ReferencedAddress { get; set; }
    public bool IsReferencedAddressIndirect { get; set; }

    private protected override void Serialize(JsonObject json)
    {
        base.Serialize(json);
        json[nameof(InstructionLength)] = InstructionLength;
        if (ReferencedAddress.HasValue)
        {
            json[nameof(ReferencedAddress)] = ReferencedAddress.ToString();
        }
        json[nameof(IsReferencedAddressIndirect)] = IsReferencedAddressIndirect;
    }

    internal override void Deserialize(JsonObject json)
    {
        base.Deserialize(json);

        InstructionLength = Convert.ToInt32(json[nameof(InstructionLength)]);
        if (json.TryGetValue(nameof(ReferencedAddress), out var ra))
        {
            ReferencedAddress = ulong.Parse((string) ra);
        }
        IsReferencedAddressIndirect = (bool) json[nameof(IsReferencedAddressIndirect)];
    }
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

    private protected override void Serialize(JsonObject json)
    {
        base.Serialize(json);

        var instructionJson = new JsonObject();
        foreach (var property in typeof(Instruction).GetProperties())
        {
            if (property.GetSetMethod() is not null && property.GetGetMethod() is not null)
            {
                instructionJson[property.Name] = property.GetValue(Instruction) switch
                {
                    ulong l => l.ToString(),
                    long l => l.ToString(),
                    Enum e => e.ToString(),
                    var propertyValue => propertyValue
                };
            }
        }
        json[nameof(Instruction)] = instructionJson;
    }

    internal override void Deserialize(JsonObject json)
    {
        base.Deserialize(json);

        object instruction = new Instruction();
        foreach (var kvp in (JsonObject) json[nameof(Instruction)])
        {
            object value = kvp.Value;
            var property = typeof(Instruction).GetProperty(kvp.Key);
            var propertyType = property.PropertyType;
            if (propertyType == typeof(ulong))
            {
                value = ulong.Parse((string) value);
            }
            else if (propertyType == typeof(long))
            {
                value = long.Parse((string) value);
            }
            else if (typeof(Enum).IsAssignableFrom(propertyType))
            {
                value = Enum.Parse(propertyType, (string) value);
            }
            else if (propertyType.IsPrimitive)
            {
                value = Convert.ChangeType(value, propertyType);
            }
            property.SetValue(instruction, value);
        }
        Instruction = (Instruction) instruction;
    }
}

public sealed class Arm64Asm : Asm
{
    private const string AddressKey = "Arm64Address";
    private const string BytesKey = "Arm64Bytes";
    private const string SyntaxKey = "Arm64Syntax";

    public Arm64Instruction Instruction { get; set; }
    internal DisassembleSyntax DisassembleSyntax { get; set; }

    public override string ToString() => Instruction.ToString();

    private protected override void Serialize(JsonObject json)
    {
        base.Serialize(json);

        // We only need the address, bytes, and syntax to reconstruct the instruction.
        if (Instruction?.Bytes?.Length > 0)
        {
            json[AddressKey] = Instruction.Address.ToString();
            json[BytesKey] = Convert.ToBase64String(Instruction.Bytes);
            json[SyntaxKey] = (int) DisassembleSyntax;
        }
    }

    internal override void Deserialize(JsonObject json)
    {
        base.Deserialize(json);

        if (json.TryGetValue(BytesKey, out var bytes64))
        {
            // Use the Capstone disassembler to recreate the instruction from the bytes.
            using var disassembler = CapstoneDisassembler.CreateArm64Disassembler(Arm64DisassembleMode.Arm);
            disassembler.EnableInstructionDetails = true;
            disassembler.DisassembleSyntax = (DisassembleSyntax) Convert.ToInt32(json[SyntaxKey]);
            byte[] bytes = Convert.FromBase64String((string) bytes64);
            Instruction = disassembler.Disassemble(bytes, long.Parse((string) json[AddressKey])).Single();
        }
    }
}

public sealed class MonoCode : SourceCode
{
    public string Text { get; set; }

    private protected override void Serialize(JsonObject json)
    {
        base.Serialize(json);

        json[nameof(Text)] = Text;
    }

    internal override void Deserialize(JsonObject json)
    {
        base.Deserialize(json);

        Text = (string) json[nameof(Text)];
    }
}

public sealed class Map
{
    [XmlArray("Instructions")]
    [XmlArrayItem(nameof(SourceCode), typeof(SourceCode))]
    [XmlArrayItem(nameof(Sharp), typeof(Sharp))]
    [XmlArrayItem(nameof(IntelAsm), typeof(IntelAsm))]
    public SourceCode[] SourceCodes { get; set; }

    internal JsonObject Serialize()
    {
        var sourceCodes = new JsonArray(SourceCodes.Length);
        foreach (var sourceCode in SourceCodes)
        {
            sourceCodes.Add(sourceCode.Serialize());
        }
        return new JsonObject
        {
            [nameof(SourceCodes)] = sourceCodes,
        };
    }

    internal void Deserialize(JsonObject json)
    {
        var sourceCodes = (JsonArray) json[nameof(SourceCodes)];
        SourceCodes = new SourceCode[sourceCodes.Count];
        for (int i = 0; i < sourceCodes.Count; i++)
        {
            var sourceJson = (JsonObject) sourceCodes[i];
            SourceCodes[i] = sourceJson["$type"] switch
            {
                nameof(Sharp) => new Sharp(),
                nameof(IntelAsm) => new IntelAsm(),
                nameof(Arm64Asm) => new Arm64Asm(),
                nameof(MonoCode) => new MonoCode(),
                var unhandledType => throw new NotSupportedException($"Unexpected type: {unhandledType}")
            };
            SourceCodes[i].Deserialize(sourceJson);
        }
    }
}

public sealed class DisassembledMethod
{
    public string Name { get; set; }

    public ulong NativeCode { get; set; }

    public string Problem { get; set; }

    public Map[] Maps { get; set; } = [];

    public string CommandLine { get; set; }

    public static DisassembledMethod Empty(string fullSignature, ulong nativeCode, string problem)
        => new()
        {
            Name = fullSignature,
            NativeCode = nativeCode,
            Problem = problem
        };

    internal JsonObject Serialize()
    {
        var maps = new JsonArray(Maps.Length);
        foreach (var map in Maps)
        {
            maps.Add(map.Serialize());
        }
        return new JsonObject
        {
            [nameof(Name)] = Name,
            [nameof(NativeCode)] = NativeCode.ToString(),
            [nameof(Problem)] = Problem,
            [nameof(Maps)] = maps,
            [nameof(CommandLine)] = CommandLine
        };
    }

    internal void Deserialize(JsonObject json)
    {
        Name = (string) json[nameof(Name)];
        NativeCode = ulong.Parse((string) json[nameof(NativeCode)]);
        Problem = (string) json[nameof(Problem)];

        var maps = (JsonArray) json[nameof(Maps)];
        Maps = new Map[maps.Count];
        for (int i = 0; i < maps.Count; i++)
        {
            Maps[i] = new Map();
            Maps[i].Deserialize((JsonObject) maps[i]);
        }

        CommandLine = (string) json[nameof(CommandLine)];
    }
}

public sealed class DisassemblyResult
{
    public DisassembledMethod[] Methods { get; set; } = [];
    public string[] Errors { get; set; } = [];
    public MutablePair[] SerializedAddressToNameMapping { get; set; } = [];
    public uint PointerSize { get; set; }

    [XmlIgnore] // XmlSerializer does not support dictionaries ;)
    public Dictionary<ulong, string> AddressToNameMapping
        =>  _addressToNameMapping ??= SerializedAddressToNameMapping.ToDictionary(x => x.Key, x => x.Value);

    [XmlIgnore]
    private Dictionary<ulong, string> _addressToNameMapping;

    // KeyValuePair is not serializable, because it has read-only properties
    // so we need to define our own...
    [Serializable]
    [XmlType(TypeName = "Workaround")]
    public struct MutablePair
    {
        public ulong Key { get; set; }
        public string Value { get; set; }
    }

    internal JsonObject Serialize()
    {
        var methods = new JsonArray(Methods.Length);
        foreach (var method in Methods)
        {
            methods.Add(method.Serialize());
        }
        var errors = new JsonArray(Errors.Length);
        foreach (var error in Errors)
        {
            errors.Add(error);
        }
        var addressToNameMapping = new JsonObject();
        foreach (var kvp in SerializedAddressToNameMapping)
        {
            addressToNameMapping[kvp.Key.ToString()] = kvp.Value;
        }
        return new JsonObject
        {
            [nameof(Methods)] = methods,
            [nameof(Errors)] = errors,
            [nameof(AddressToNameMapping)] = addressToNameMapping,
            [nameof(PointerSize)] = PointerSize.ToString()
        };
    }

    internal void Deserialize(JsonObject json)
    {
        var methods = (JsonArray) json[nameof(Methods)];
        Methods = new DisassembledMethod[methods.Count];
        for (int i = 0; i < methods.Count; i++)
        {
            Methods[i] = new DisassembledMethod();
            Methods[i].Deserialize((JsonObject) methods[i]);
        }

        var errors = (JsonArray) json[nameof(Errors)];
        Errors = new string[errors.Count];
        for (int i = 0; i < errors.Count; i++)
        {
            Errors[i] = (string) errors[i];
        }

        var addressToNameMapping = (JsonObject) json[nameof(AddressToNameMapping)];
        SerializedAddressToNameMapping = new MutablePair[addressToNameMapping.Count];
        int addressIndex = 0;
        foreach (var kvp in addressToNameMapping)
        {
            SerializedAddressToNameMapping[addressIndex].Key = ulong.Parse(kvp.Key);
            SerializedAddressToNameMapping[addressIndex].Value = (string) kvp.Value;
            ++addressIndex;
        }

        PointerSize = uint.Parse((string) json[nameof(PointerSize)]);
    }
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
    internal string TargetFrameworkMoniker { get; }
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
        public bool Equals(ClrMethod x, ClrMethod y) => x.NativeCode == y.NativeCode;

        public int GetHashCode(ClrMethod obj) => (int) obj.NativeCode;
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