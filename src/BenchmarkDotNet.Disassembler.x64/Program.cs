using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interop;
using Microsoft.Diagnostics.RuntimeExt;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Iced.Intel;
using Decoder = Iced.Intel.Decoder;

namespace BenchmarkDotNet.Disassembler
{
    internal static class Program
    {
        // the goals of the existence of this process: 
        // 1. attach to benchmarked process
        // 2. disassemble the code
        // 3. save it to xml file
        // 4. detach & shut down
        //
        // requirements: must not have any dependencies to BenchmarkDotNet itself, KISS
        static void Main(string[] args)
        {
            var options = Settings.FromArgs(args);

            if (Process.GetProcessById(options.ProcessId).HasExited) // possible when benchmark has already finished
                throw new Exception($"The process {options.ProcessId} has already exited"); // if we don't throw here the Clrmd will fail with some mysterious HRESULT: 0xd000010a ;)

            try
            {
                var methodsToExport = ClrMdDisassembler.AttachAndDisassemble(options);
                
                SaveToFile(methodsToExport, options.ResultsPath);
            }
            catch (OutOfMemoryException) // thrown by clrmd when pdb is missing or in invalid format
            {
                Console.WriteLine("\\ ---------------------------");
                Console.WriteLine("Failed to read source code location!");
                Console.WriteLine("Please make sure that the project, which defines benchmarks contains following settings:");
                Console.WriteLine("\t <DebugType>pdbonly</DebugType>");
                Console.WriteLine("\t <DebugSymbols>true</DebugSymbols>");
                Console.WriteLine("\\ ---------------------------");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to disassemble with following exception:");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private static void SaveToFile(DisassembledMethod[] disassembledMethods, string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write))
            using (var writer = XmlWriter.Create(stream))
            {
                var serializer = new XmlSerializer(typeof(DisassemblyResult));

                serializer.Serialize(writer, new DisassemblyResult { Methods = disassembledMethods });
            }
        }
    }

    internal static class ClrMdDisassembler
    {
        internal static DisassembledMethod[] AttachAndDisassemble(Settings settings)
        {
            using (var dataTarget = DataTarget.AttachToProcess(
                settings.ProcessId,
                (uint)TimeSpan.FromMilliseconds(5000).TotalMilliseconds,
                AttachFlag.Passive))
            {
                var runtime = dataTarget.ClrVersions.Single().CreateRuntime();

                // Per https://github.com/microsoft/clrmd/issues/303
                dataTarget.DataReader.Flush();

                ConfigureSymbols(dataTarget);

                var formatter = new NasmFormatter();
                formatter.Options.DigitSeparator = "`";
                formatter.Options.FirstOperandCharIndex = 10;

                var state = new State(runtime, formatter);

                var disassembledMethods = Disassemble(settings, runtime, state);

                // we don't want to export the disassembler entry point method which is just an artificial method added to get generic types working
                return disassembledMethods.Length == 1
                    ? disassembledMethods // if there is only one method we want to return it (most probably benchmark got inlined)
                    : disassembledMethods.Where(method => !method.Name.Contains(DisassemblerConstants.DisassemblerEntryMethodName)).ToArray();
            }
        }

        private static void ConfigureSymbols(DataTarget dataTarget)
        {
            // code copied from https://github.com/Microsoft/clrmd/issues/34#issuecomment-161926535
            var symbols = dataTarget.DebuggerInterface as IDebugSymbols;
            symbols?.SetSymbolPath("http://msdl.microsoft.com/download/symbols");
            var control = dataTarget.DebuggerInterface as IDebugControl;
            control?.Execute(DEBUG_OUTCTL.NOT_LOGGED, ".reload", DEBUG_EXECUTE.NOT_LOGGED);
        }

        private static DisassembledMethod[] Disassemble(Settings settings, ClrRuntime runtime, State state)
        {
            var result = new List<DisassembledMethod>();

            var typeWithBenchmark = runtime.Heap.GetTypeByName(settings.TypeName);

            state.Todo.Enqueue(
                new MethodInfo(
                    // the Disassembler Entry Method is always parameterless, so check by name is enough
                    typeWithBenchmark.Methods.Single(method => method.IsPublic && method.Name == settings.MethodName),
                    0));

            while (state.Todo.Count != 0)
            {
                var method = state.Todo.Dequeue();

                if (!state.HandledMethods.Add(new MethodId(method.Method.MetadataToken, method.Method.Type.MetadataToken))) // add it now to avoid StackOverflow for recursive methods
                    continue; // already handled

                if (settings.MaxDepth >= method.Depth)
                    result.Add(DisassembleMethod(method, state, settings));
            }

            return result.ToArray();
        }

        private static DisassembledMethod DisassembleMethod(MethodInfo methodInfo, State state, Settings settings)
        {
            var method = methodInfo.Method;

            if (method.CompilationType == MethodCompilationType.None)
                return CreateEmpty(method, "Native method");

            if (method.NativeCode == ulong.MaxValue)
                if (method.IsAbstract) return CreateEmpty(method, "Abstract method");
                else if (method.IsVirtual) CreateEmpty(method, "Virtual method");
                else return CreateEmpty(method, "Method got most probably inlined");

            if ((method.ILOffsetMap is null || method.ILOffsetMap.Length == 0) && (method.HotColdInfo is null || method.HotColdInfo.HotStart == 0))
                return CreateEmpty(method, $"No valid {nameof(method.ILOffsetMap)} and {nameof(method.HotColdInfo)}");

            var codes = new List<Code>();
            if (settings.PrintSource && !(method.ILOffsetMap is null))
            {
                // for getting C# code we always use the original ILOffsetMap
                foreach (var map in method.ILOffsetMap.Where(map => map.StartAddress < map.EndAddress && map.ILOffset >= 0).OrderBy(map => map.StartAddress))
                    codes.AddRange(SourceCodeProvider.GetSource(method, map));
            }

            if (settings.PrintAsm)
            {
                // for getting ASM we try to use data from HotColdInfo if available (better for decoding)
                foreach (var map in GetCompleteNativeMap(method))
                    codes.AddRange(AsmProvider.GetAsm(map.StartAddress, (uint)(map.EndAddress - map.StartAddress), state, methodInfo.Depth, method));
            }

            List<Map> maps = settings.PrintAsm && settings.PrintSource
                ? codes.GroupBy(code => code.StartAddress).OrderBy(group => group.Key).Select(group => new Map() { Instructions = group.ToList() }).ToList()
                : new List<Map>() { new Map() { Instructions = codes } };

            return new DisassembledMethod
            {
                Maps = EliminateDuplicates(maps),
                Name = method.GetFullSignature(),
                NativeCode = method.NativeCode
            };
        }

        private static ILToNativeMap[] GetCompleteNativeMap(ClrMethod method)
        {
            // it's better to use one single map rather than few small ones
            // it's simply easier to get next instruction when decoding ;)
            var hotColdInfo = method.HotColdInfo;
            if (!(hotColdInfo is null && hotColdInfo.HotSize > 0 && hotColdInfo.HotStart > 0))
            {
                return hotColdInfo.ColdSize <= 0
                    ? new[] { new ILToNativeMap() { StartAddress = hotColdInfo.HotStart, EndAddress = hotColdInfo.HotStart + hotColdInfo.HotSize, ILOffset = -1 } }
                    : new[]
                      {
                            new ILToNativeMap() { StartAddress = hotColdInfo.HotStart, EndAddress = hotColdInfo.HotStart + hotColdInfo.HotSize, ILOffset = -1 },
                            new ILToNativeMap() { StartAddress = hotColdInfo.ColdStart, EndAddress = hotColdInfo.ColdStart + hotColdInfo.ColdSize, ILOffset = -1 }
                      };
            }

            return method.ILOffsetMap
                    .Where(map => map.StartAddress < map.EndAddress) // some maps have 0 length?
                    .OrderBy(map => map.StartAddress) // we need to print in the machine code order, not IL! #536
                    .ToArray();
        }

        private static DisassembledMethod CreateEmpty(ClrMethod method, string reason)
            => DisassembledMethod.Empty(method.GetFullSignature(), method.NativeCode, reason);

        private static Map[] EliminateDuplicates(List<Map> maps)
        {
            var unique = new HashSet<Code>(CodeComparer.Instance);

            foreach (var map in maps)
                for (int i = map.Instructions.Count - 1; i >= 0; i--)
                    if (!unique.Add(map.Instructions[i]))
                        map.Instructions.RemoveAt(i);

            return maps.Where(map => map.Instructions.Any()).ToArray();
        }

        private class CodeComparer : IEqualityComparer<Code>
        {
            internal static readonly IEqualityComparer<Code> Instance = new CodeComparer();

            public bool Equals(Code x, Code y)
            {
                // sometimes ClrMD reports same address range in two different ILToNativeMaps
                // this happens usually for prolog and the instruction after prolog
                // and for the epilog and last instruction before epilog
                if (x is Asm asmLeft && y is Asm asmRight)
                    return asmLeft.StartAddress == asmRight.StartAddress && asmLeft.EndAddress == asmRight.EndAddress;

                // sometimes some C# code lines are duplicated because the same line is the best match for multiple ILToNativeMaps
                // we don't want to confuse the users, so this must also be removed
                if (x is Sharp sharpLeft && y is Sharp sharpRight)
                    return sharpLeft.TextRepresentation == sharpRight.TextRepresentation
                        && sharpLeft.FilePath == sharpRight.FilePath
                        && sharpLeft.LineNumber == sharpRight.LineNumber;

                return false; // different types!
            }

            public int GetHashCode(Code obj) => obj.TextRepresentation.GetHashCode();
        }
    }

    internal static class AsmProvider
    {
        internal static IEnumerable<Asm> GetAsm(ulong startAddress, uint size, State state, int depth, ClrMethod currentMethod)
        {
            byte[] code = new byte[size];
            if (!state.Runtime.DataTarget.ReadProcessMemory(startAddress, code, code.Length, out int bytesRead) || bytesRead == 0)
                yield break;

            var decoder = Decoder.Create(IntPtr.Size * 8, new ByteArrayCodeReader(code, 0, bytesRead));
            decoder.IP = startAddress;

            while (decoder.IP < startAddress + (ulong)bytesRead)
            {
                decoder.Decode(out var instruction);

                string textRepresentation = GetTextRepresentation(instruction, state, startAddress, code);

                TryTranslateAddressToName(instruction, state, depth, currentMethod, out string name);

                yield return new Asm
                {
                    TextRepresentation = textRepresentation,
                    Comment = name,
                    StartAddress = instruction.IP,
                    EndAddress = instruction.IP + (ulong)instruction.ByteLength,
                    SizeInBytes = (uint)instruction.ByteLength
                };
            }
        }

        private static string GetTextRepresentation(Instruction instruction, State state, ulong startAddress, byte[] code)
        {
            var output = new StringBuilderFormatterOutput();

            output.Write(instruction.IP.ToString("X16"), FormatterOutputTextKind.Text);
            output.Write(" ", FormatterOutputTextKind.Text);

            int byteBaseIndex = (int)(instruction.IP - startAddress);
            for (int i = 0; i < instruction.ByteLength; i++)
                output.Write(code[byteBaseIndex + i].ToString("X2"), FormatterOutputTextKind.Text);
            for (int i = 0; i < 10 - instruction.ByteLength; i++)
                output.Write("  ", FormatterOutputTextKind.Text);

            output.Write(" ", FormatterOutputTextKind.Text);
            state.Formatter.Format(instruction, output);

            return output.ToString();
        }

        private static void TryTranslateAddressToName(Instruction instruction, State state, int depth, ClrMethod currentMethod, out string calledMethodName)
        {
            calledMethodName = default;
            ClrRuntime runtime = state.Runtime;

            if (!TryGetAddress(instruction, runtime, out ulong address) || address <= ushort.MaxValue)
                return;

            calledMethodName = runtime.GetJitHelperFunctionName(address);
            if (!string.IsNullOrEmpty(calledMethodName))
                return;

            calledMethodName = runtime.GetMethodTableName(address);
            if (!string.IsNullOrEmpty(calledMethodName))
            {
                calledMethodName = $"MT_{calledMethodName}";
                return;
            }

            var method = runtime.GetMethodByAddress(address);
            if (method is null)
                return;

            if (method.NativeCode == currentMethod.NativeCode && method.GetFullSignature() == currentMethod.GetFullSignature())
                return; // in case of a call which is just a jump within the method or a recursive call

            if (!state.HandledMethods.Contains(new MethodId(method.MetadataToken, method.Type.MetadataToken)))
                state.Todo.Enqueue(new MethodInfo(method, depth + 1));

            calledMethodName = method.GetFullSignature();
            if (!calledMethodName.StartsWith(method.Type.Name, StringComparison.Ordinal))
                calledMethodName = $"{method.Type.Name}.{method.GetFullSignature()}";
        }

        private static bool TryGetAddress(Instruction instruction, ClrRuntime runtime, out ulong address)
        {
            for (int i = 0; i < instruction.OpCount; i++)
            {
                switch (instruction.GetOpKind(i))
                {
                    case OpKind.NearBranch16:
                    case OpKind.NearBranch32:
                    case OpKind.NearBranch64:
                        address = instruction.NearBranchTarget;
                        return true;
                    case OpKind.Immediate16:
                    case OpKind.Immediate8to16:
                    case OpKind.Immediate8to32:
                    case OpKind.Immediate8to64:
                    case OpKind.Immediate32to64:
                    case OpKind.Immediate32 when runtime.PointerSize == 4:
                    case OpKind.Immediate64:
                        address = instruction.GetImmediate(i);
                        return true;
                    case OpKind.Memory64:
                        address = instruction.MemoryAddress64;
                        return true;
                    case OpKind.Memory when instruction.IsIPRelativeMemoryOperand:
                        address = instruction.IPRelativeMemoryAddress;
                        return true;
                    case OpKind.Memory:
                        address = instruction.MemoryDisplacement;
                        return true;
                }
            }

            address = default;
            return false;
        }
    }

    internal static class SourceCodeProvider
    {
        private static readonly Dictionary<string, string[]> SourceFileCache = new Dictionary<string, string[]>();

        internal static IEnumerable<Sharp> GetSource(ClrMethod method, ILToNativeMap map)
        {
            var sourceLocation = method.GetSourceLocation(map.ILOffset);
            if (sourceLocation == null)
                yield break;

            for (int line = sourceLocation.LineNumber; line <= sourceLocation.LineNumberEnd; ++line)
            {
                var sourceLine = ReadSourceLine(sourceLocation.FilePath, line);
                if (sourceLine == null)
                    continue;

                var text = sourceLine + Environment.NewLine 
                    + GetSmartPrefix(sourceLine, sourceLocation.ColStart - 1) 
                    + new string('^', sourceLocation.ColEnd - sourceLocation.ColStart);

                yield return new Sharp
                {
                    TextRepresentation = text,
                    StartAddress = map.StartAddress,
                    FilePath = sourceLocation.FilePath,
                    LineNumber = line
                };
            }
        }

        private static string ReadSourceLine(string file, int line)
        {
            if (!SourceFileCache.TryGetValue(file, out string[] contents))
            {
                // sometimes the symbols report some disk location from MS CI machine like "E:\A\_work\308\s\src\mscorlib\shared\System\Random.cs" for .NET Core 2.0
                if (!File.Exists(file)) 
                    return null;

                contents = File.ReadAllLines(file);
                SourceFileCache.Add(file, contents);
            }

            return line - 1 < contents.Length
                ? contents[line - 1]
                : null; // "nop" can have no corresponding c# code ;)
        }

        private static string GetSmartPrefix(string sourceLine, int length)
        {
            if (length <= 0)
                return string.Empty;

            var prefix = new char[length];
            for (int i = 0; i < length; i++)
            {
                char sourceChar = i < sourceLine.Length ? sourceLine[i] : ' ';
                prefix[i] = sourceChar == '\t' ? sourceChar : ' ';
            }
            return new string(prefix);
        }
    }

    internal class Settings
    {
        internal Settings(int processId, string typeName, string methodName, bool printAsm, bool printSource, int recursiveDepth, string resultsPath)
        {
            ProcessId = processId;
            TypeName = typeName;
            MethodName = methodName;
            PrintAsm = printAsm;
            PrintSource = printSource;
            MaxDepth = methodName == DisassemblerConstants.DisassemblerEntryMethodName && recursiveDepth != int.MaxValue ? recursiveDepth + 1 : recursiveDepth;
            ResultsPath = resultsPath;
        }

        internal int ProcessId { get; }
        internal string TypeName { get; }
        internal string MethodName { get; }
        internal bool PrintAsm { get; }
        internal bool PrintSource { get; }
        internal int MaxDepth { get; }
        internal string ResultsPath { get; }

        internal static Settings FromArgs(string[] args)
            => new Settings(
                processId: int.Parse(args[0]),
                typeName: args[1],
                methodName: args[2],
                printAsm: bool.Parse(args[3]),
                printSource: bool.Parse(args[4]),
                recursiveDepth: int.Parse(args[5]),
                resultsPath: args[6]
            );
    }

    internal class State
    {
        internal State(ClrRuntime runtime, Formatter formatter)
        {
            Runtime = runtime;
            Formatter = formatter;
            Todo = new Queue<MethodInfo>();
            HandledMethods = new HashSet<MethodId>();
        }

        internal ClrRuntime Runtime { get; }
        internal Queue<MethodInfo> Todo { get; }
        internal HashSet<MethodId> HandledMethods { get; }
        internal Formatter Formatter { get; }
    }

    readonly struct MethodInfo // I am not using ValueTuple here (would be perfect) to keep the number of dependencies as low as possible
    {
        internal ClrMethod Method { get; }
        internal int Depth { get; }

        internal MethodInfo(ClrMethod method, int depth)
        {
            Method = method;
            Depth = depth;
        }
    }

    readonly struct MethodId : IEquatable<MethodId>
    {
        internal uint MethodMetadataTokenId { get; }
        internal uint TypeMetadataTokenId { get; }

        public MethodId(uint methodMetadataTokenId, uint typeMetadataTokenId) : this()
        {
            MethodMetadataTokenId = methodMetadataTokenId;
            TypeMetadataTokenId = typeMetadataTokenId;
        }

        public bool Equals(MethodId other) => MethodMetadataTokenId == other.MethodMetadataTokenId && TypeMetadataTokenId == other.TypeMetadataTokenId;
        public override bool Equals(object other) => other is MethodId methodId && Equals(methodId);
        public override int GetHashCode() => (int)(MethodMetadataTokenId ^ TypeMetadataTokenId);
    }
}
