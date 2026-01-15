using BenchmarkDotNet.Disassemblers;
using System.Text.Json.Serialization;

namespace BenchmarkDotNet.Serialization;

[JsonSerializable(typeof(ClrMdArgs))]
[JsonSerializable(typeof(Sharp))]
[JsonSerializable(typeof(MonoCode))]
[JsonSerializable(typeof(IntelAsm))]
[JsonSerializable(typeof(Arm64Asm))]
[JsonSerializable(typeof(Map))]
[JsonSerializable(typeof(DisassembledMethod))]
[JsonSerializable(typeof(DisassemblyResult))]
internal partial class BdnJsonSerializerContext : JsonSerializerContext
{
}
