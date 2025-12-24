using System;
using System.Linq;
using SimpleJson;

#nullable enable

namespace BenchmarkDotNet.Disassemblers
{
    internal struct ClrMdArgs(int processId, string typeName, string methodName, bool printSource, int maxDepth, string syntax, string tfm, string[] filters, string resultsPath = "")
    {
        internal int ProcessId = processId;
        internal string TypeName = typeName;
        internal string MethodName = methodName;
        internal bool PrintSource = printSource;
        internal int MaxDepth = methodName == DisassemblerConstants.DisassemblerEntryMethodName && maxDepth != int.MaxValue ? maxDepth + 1 : maxDepth;
        internal string[] Filters = filters;
        internal string Syntax = syntax;
        internal string TargetFrameworkMoniker = tfm;
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

        internal readonly string Serialize()
        {
            SimpleJsonSerializer.CurrentJsonSerializerStrategy.Indent = false;
            var jsonObject = new JsonObject()
            {
                [nameof(MethodName)] = MethodName,
                [nameof(PrintSource)] = PrintSource,
                [nameof(MaxDepth)] = MaxDepth,
                [nameof(Syntax)] = Syntax,
                [nameof(TargetFrameworkMoniker)] = TargetFrameworkMoniker,
                [nameof(ResultsPath)] = ResultsPath,
            };
            var filters = new JsonArray(Filters.Length);
            foreach (var filter in Filters)
            {
                filters.Add(filter);
            }
            jsonObject[nameof(Filters)] = filters;
            return jsonObject.ToString();
        }

        internal void Deserialize(string? json)
        {
            var jsonObject = SimpleJsonSerializer.DeserializeObject<JsonObject>(json);
            if (jsonObject == null)
                return;

            MethodName = (string)jsonObject[nameof(MethodName)];
            PrintSource = (bool)jsonObject[nameof(PrintSource)];
            MaxDepth = Convert.ToInt32(jsonObject[nameof(MaxDepth)]);
            Syntax = (string) jsonObject[nameof(Syntax)];
            TargetFrameworkMoniker = (string) jsonObject[nameof(TargetFrameworkMoniker)];
            ResultsPath = (string) jsonObject[nameof(ResultsPath)];
            var filters = (JsonArray) jsonObject[nameof(Filters)];
            Filters = new string[filters.Count];
            for (int i = 0; i < filters.Count; ++i)
            {
                Filters[i] = (string) filters[i];
            }
        }
    }
}