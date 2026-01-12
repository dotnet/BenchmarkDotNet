using BenchmarkDotNet.Serialization;
using System.Linq;
using System.Text.Json.Serialization;

#nullable enable

namespace BenchmarkDotNet.Disassemblers
{
    internal struct ClrMdArgs(int processId, string typeName, string methodName, bool printSource, int maxDepth, string syntax, string tfm, string[] filters, string resultsPath = "")
    {
        [JsonIgnore]
        internal int ProcessId = processId;

        [JsonIgnore]
        internal string TypeName = typeName ?? "";

        [JsonInclude]
        internal string MethodName = methodName;

        [JsonInclude]
        internal bool PrintSource = printSource;

        [JsonInclude]
        internal int MaxDepth = methodName == DisassemblerConstants.DisassemblerEntryMethodName && maxDepth != int.MaxValue ? maxDepth + 1 : maxDepth;

        [JsonInclude]
        internal string[] Filters = filters;

        [JsonInclude]
        internal string Syntax = syntax;

        [JsonInclude]
        internal string TargetFrameworkMoniker = tfm;

        [JsonInclude]
        internal string ResultsPath = resultsPath;

        internal static ClrMdArgs FromArgs(string[] args)
            => new(
                processId: int.Parse(args[0]),
                typeName: args[1],
                methodName: args[2],
                printSource: bool.Parse(args[3]),
                maxDepth: int.Parse(args[4]),
                resultsPath: args[5],
                syntax: args[6],
                tfm: args[7],
                filters: [.. args.Skip(8)]
            );
    }
}