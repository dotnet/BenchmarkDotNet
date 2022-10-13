using Iced.Intel;
using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

#pragma warning disable CS3001 // Argument type 'ulong' is not CLS-compliant
#pragma warning disable CS3003 // Type is not CLS-compliant
#pragma warning disable CS1591 // XML comments for public types...
namespace BenchmarkDotNet.Disassemblers
{
    public abstract class SourceCode
    {
        public ulong InstructionPointer { get; set; }
    }

    public class Sharp : SourceCode
    {
        public string Text { get; set; }
        public string FilePath { get; set; }
        public int LineNumber { get; set; }
    }

    public class Asm : SourceCode
    {
        public int InstructionLength { get; set; }
        public ulong? ReferencedAddress { get; set; }
        public bool IsReferencedAddressIndirect { get; set; }

        public Instruction? IntelInstruction { get; set; }
#if !CLRMDV1
        public Gee.External.Capstone.Arm64.Arm64Instruction Arm64Instruction { get; set; }
#endif
    }

    public class MonoCode : SourceCode
    {
        public string Text { get; set; }
    }

    public class Map
    {
        [XmlArray("Instructions")]
        [XmlArrayItem(nameof(SourceCode), typeof(SourceCode))]
        [XmlArrayItem(nameof(Sharp), typeof(Sharp))]
        [XmlArrayItem(nameof(Asm), typeof(Asm))]
        public SourceCode[] SourceCodes { get; set; }
    }

    public class DisassembledMethod
    {
        public string Name { get; set; }

        public ulong NativeCode { get; set; }

        public string Problem { get; set; }

        public Map[] Maps { get; set; }

        public string CommandLine { get; set; }

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
        public string[] Errors { get; set; }
        public MutablePair[] SerializedAddressToNameMapping { get; set; }
        public uint PointerSize { get; set; }

        [XmlIgnore] // XmlSerializer does not support dictionaries ;)
        public Dictionary<ulong, string> AddressToNameMapping
            => _addressToNameMapping ?? (_addressToNameMapping = SerializedAddressToNameMapping.ToDictionary(x => x.Key, x => x.Value));

        [XmlIgnore]
        private Dictionary<ulong, string> _addressToNameMapping;

        public DisassemblyResult()
        {
            Methods = Array.Empty<DisassembledMethod>();
            Errors = Array.Empty<string>();
        }

        // KeyValuePair is not serializable, because it has read-only properties
        // so we need to define our own...
        [Serializable]
        [XmlType(TypeName = "Workaround")]
        public struct MutablePair
        {
            public ulong Key { get; set; }
            public string Value { get; set; }
        }
    }

    public static class DisassemblerConstants
    {
        public const string DisassemblerEntryMethodName = "__ForDisassemblyDiagnoser__";
    }

    internal class Settings
    {
        internal Settings(int processId, string typeName, string methodName, bool printSource, int maxDepth, string resultsPath, string syntax, string tfm, string[] filters)
        {
            ProcessId = processId;
            TypeName = typeName;
            MethodName = methodName;
            PrintSource = printSource;
            MaxDepth = methodName == DisassemblerConstants.DisassemblerEntryMethodName && maxDepth != int.MaxValue ? maxDepth + 1 : maxDepth;
            ResultsPath = resultsPath;
            Syntax = syntax;
            TargetFrameworkMoniker = tfm;
            Filters = filters;
        }

        internal int ProcessId { get; }
        internal string TypeName { get; }
        internal string MethodName { get; }
        internal bool PrintSource { get; }
        internal int MaxDepth { get; }
        internal string[] Filters;
        internal string Syntax { get; }
        internal string TargetFrameworkMoniker { get; }
        internal string ResultsPath { get; }

        internal static Settings FromArgs(string[] args)
            => new Settings(
                processId: int.Parse(args[0]),
                typeName: args[1],
                methodName: args[2],
                printSource: bool.Parse(args[3]),
                maxDepth: int.Parse(args[4]),
                resultsPath: args[5],
                syntax: args[6],
                tfm: args[7],
                filters: args.Skip(8).ToArray()
            );
    }

    internal class State
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
}
#pragma warning restore CS1591 // XML comments for public types...
#pragma warning restore CS3003 // Type is not CLS-compliant
#pragma warning restore CS3001 // Argument type 'ulong' is not CLS-compliant