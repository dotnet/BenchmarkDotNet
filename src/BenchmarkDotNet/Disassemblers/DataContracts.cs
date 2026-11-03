using AsmArm64;
using Iced.Intel;
using Microsoft.Diagnostics.Runtime.Interfaces;
using System.Diagnostics;
using System.Text.Json.Serialization;


#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

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
