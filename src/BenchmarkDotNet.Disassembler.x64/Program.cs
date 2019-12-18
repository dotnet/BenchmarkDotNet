using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interop;
using Microsoft.Diagnostics.RuntimeExt;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Iced.Intel;
using Decoder = Iced.Intel.Decoder;

namespace BenchmarkDotNet.Disassembler
{
    internal static class Program
    {
        static readonly string[] CallSeparator = { "call" };
        static readonly Dictionary<string, string[]> SourceFileCache = new Dictionary<string, string[]>();

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
                var methodsToExport = Disassemble(options);
                
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

        internal static DisassembledMethod[] Disassemble(Settings settings)
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

                var state = new State(runtime);

                var disassembledMethods = Disassemble(settings, runtime, state);

                // we don't want to export the disassembler entry point method which is just an artificial method added to get generic types working
                return disassembledMethods.Length == 1
                    ? disassembledMethods // if there is only one method we want to return it (most probably benchmark got inlined)
                    : disassembledMethods.Where(method => !method.Name.Contains(DisassemblerConstants.DisassemblerEntryMethodName)).ToArray();
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

                if(settings.RecursiveDepth >= method.Depth)
                    result.Add(DisassembleMethod(method, state, settings));
            }

            return result.ToArray();
        }

        private static DisassembledMethod DisassembleMethod(MethodInfo methodInfo, State state, Settings settings)
        {
            var method = methodInfo.Method;

            if (method.NativeCode == ulong.MaxValue || method.ILOffsetMap == null)
            {
                if (method.NativeCode == ulong.MaxValue)
                    if (method.IsAbstract) return CreateEmpty(method, "Abstract method");
                    else if (method.IsVirtual) CreateEmpty(method, "Virtual method");
                    else return CreateEmpty(method, "Method got most probably inlined");

                if (method.ILOffsetMap == null)
                    return CreateEmpty(method, "No ILOffsetMap found");
            }

            var maps = new List<Map>();

            var mapByStartAddress = (from map in method.ILOffsetMap
                                 where map.StartAddress <= map.EndAddress
                                 orderby map.StartAddress // we need to print in the machine code order, not IL! #536
                                 select map).ToArray();

            var sortedUniqueILOffsets = method.ILOffsetMap
                .Where(map => map.ILOffset >= 0 && map.StartAddress <= map.EndAddress)
                .Select(map => map.ILOffset)
                .Distinct() // there can be many maps with the same ILOffset
                .OrderBy(ilOffset => ilOffset)
                .ToArray();

            // maps with negative ILOffset are not always part of the prolog or epilog
            // so we don't exclude all maps with negative ILOffset
            // but only the first ones and the last ones if PrintPrologAndEpilog == false
            bool methodWithoutBody = method.ILOffsetMap.All(map => map.ILOffset < 0); // sth like [NoInlining] void Sample() { }
            int startIndex = settings.PrintPrologAndEpilog || methodWithoutBody
                ? 0 
                : mapByStartAddress.TakeWhile(map => map.ILOffset < 0).Count();
            int endIndex = settings.PrintPrologAndEpilog || methodWithoutBody
                ? mapByStartAddress.Length
                : mapByStartAddress.Length - mapByStartAddress.Reverse().TakeWhile(map => map.ILOffset < 0).Count();

            for (int i = startIndex; i < endIndex; ++i)
            {
                var group = new List<Code>();
                var map = mapByStartAddress[i];

                if (map.ILOffset >= 0)
                {
                    var ilOffsetIndex = Array.IndexOf(sortedUniqueILOffsets, map.ILOffset);
                    var nextILOffsetIndex = ilOffsetIndex + 1;

                    // method.GetILOffset(map.EndAddress); is not enough to get the endILOffset (it returns startILOffset)
                    var nextMapILOffset = nextILOffsetIndex < sortedUniqueILOffsets.Length
                        ? sortedUniqueILOffsets[nextILOffsetIndex]
                        : int.MaxValue;
                }

                if (settings.PrintSource && map.ILOffset >= 0)
                    group.AddRange(GetSource(method, map));

                if (settings.PrintAsm)
                    group.AddRange(GetAsm(map, state, methodInfo.Depth, method));

                maps.Add(new Map { Instructions = group });
            }

            return new DisassembledMethod
            {
                Maps = EliminateDuplicates(maps),
                Name = method.GetFullSignature(),
                NativeCode = method.NativeCode
            };
        }

        private static IEnumerable<Sharp> GetSource(ClrMethod method, ILToNativeMap map)
        {
            var sourceLocation = method.GetSourceLocation(map.ILOffset);
            if (sourceLocation == null)
                yield break;

            for (int line = sourceLocation.LineNumber; line <= sourceLocation.LineNumberEnd; ++line)
            {
                var sourceLine = ReadSourceLine(sourceLocation.FilePath, line);

                if (sourceLine != null)
                {
                    yield return new Sharp
                    {
                        TextRepresentation = sourceLine + Environment.NewLine + GetSmartPrefix(sourceLine, sourceLocation.ColStart - 1) + new string('^', sourceLocation.ColEnd - sourceLocation.ColStart),
                        FilePath = sourceLocation.FilePath,
                        LineNumber = line
                    };
                }
            }
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

        private static IEnumerable<Asm> GetAsm(ILToNativeMap map, State state, int depth, ClrMethod currentMethod)
        {
            int length = (int)(map.EndAddress - map.StartAddress);
            byte[] buffer = new byte[length];

            if (!state.Runtime.DataTarget.ReadProcessMemory(map.StartAddress, buffer, buffer.Length, out int bytesRead))
                yield break;

            var formatter = new NasmFormatter();
            formatter.Options.DigitSeparator = "`";
            formatter.Options.FirstOperandCharIndex = 10;
            var output = new StringBuilderFormatterOutput();
            var decoder = Decoder.Create(IntPtr.Size * 8, new ByteArrayCodeReader(buffer, 0, bytesRead));
            decoder.IP = map.StartAddress;

            while (decoder.IP < map.StartAddress + (ulong)bytesRead)
            {
                decoder.Decode(out var instruction);

                string textRepresentation = GetTextRepresentation(instruction, formatter, output, map, buffer);

                string calledMethodName = textRepresentation.Contains("call")
                    ? TryEnqueueCalledMethod(textRepresentation, state, depth, currentMethod)
                    : null;

                yield return new Asm
                {
                    TextRepresentation = textRepresentation,
                    Comment = calledMethodName,
                    StartAddress = instruction.IP,
                    EndAddress = instruction.IP + (ulong)instruction.ByteLength,
                    SizeInBytes = (uint)instruction.ByteLength
                };
            }
        }

        private static string GetTextRepresentation(Iced.Intel.Instruction instruction, Formatter formatter, StringBuilderFormatterOutput output, ILToNativeMap map, byte[] buffer)
        {
            var formattedOutput = new StringBuilder(100);

            formatter.Format(instruction, output);

            formattedOutput.Append(instruction.IP.ToString("X16"));
            formattedOutput.Append(' ');

            int byteBaseIndex = (int)(instruction.IP - map.StartAddress);
            for (int i = 0; i < instruction.ByteLength; i++)
                formattedOutput.Append(buffer[byteBaseIndex + i].ToString("X2"));
            for (int i = 0; i < 10 - instruction.ByteLength; i++)
                formattedOutput.Append("  ");

            formattedOutput.Append(' ');
            formattedOutput.Append(output.ToStringAndReset());

            return formattedOutput.ToString();
        }

        private static string ReadSourceLine(string file, int line)
        {
            if (!SourceFileCache.TryGetValue(file, out string[] contents))
            {
                if (!File.Exists(file)) // sometimes the symbols report some disk location from MS CI machine like "E:\A\_work\308\s\src\mscorlib\shared\System\Random.cs" for .NET Core 2.0
                    return null;

                contents = File.ReadAllLines(file);
                SourceFileCache.Add(file, contents);
            }

            return line - 1 < contents.Length
                ? contents[line - 1]
                : null; // "nop" can have no corresponding c# code ;)
        }

        private static string TryEnqueueCalledMethod(string textRepresentation, State state, int depth, ClrMethod currentMethod)
        {
            if (!TryGetHexAddress(textRepresentation, out ulong address))
                return null; // call    qword ptr [rax+20h] // needs further research

            var method = state.Runtime.GetMethodByAddress(address);

            if (method == null) // not managed method
                return DisassemblerConstants.NotManagedMethod;

            if (method.NativeCode == currentMethod.NativeCode && method.GetFullSignature() == currentMethod.GetFullSignature())
                return null; // in case of call which is just a jump within the method

            if (!state.HandledMethods.Contains(new MethodId(method.MetadataToken, method.Type.MetadataToken)))
                state.Todo.Enqueue(new MethodInfo(method, depth + 1));

            return method.GetFullSignature();
        }

        private static bool TryGetHexAddress(string textRepresentation, out ulong address)
        {
            // it's always "something call something addr`ess something"
            // 00007ffe`16fb04e4 e897fbffff      call    00007ffe`16fb0080 // static or instance method call
            // 000007fe`979171fb e800261b5e      call    mscorlib_ni+0x499800 (000007fe`f5ac9800) // managed implementation in mscorlib 
            // 000007fe`979f666f 41ff13          call    qword ptr [r11] ds:000007fe`978e0050=000007fe978ed260
            // 00007ffe`16fc0503 e81820615f      call    clr+0x2520 (00007ffe`765d2520) // native implementation in Clr
            var rightPart = textRepresentation
                .Split(CallSeparator, StringSplitOptions.RemoveEmptyEntries).Last() // take the right part
                .Trim() // remove leading whitespaces
                .Replace("`", string.Empty); // remove the magic delimiter

            string addressPart;
            if (rightPart.Contains('(') && rightPart.Contains(')'))
                addressPart = rightPart.Substring(rightPart.LastIndexOf('(') + 1, rightPart.LastIndexOf(')') - rightPart.LastIndexOf('(') - 1);
            else if (rightPart.Contains(':') && rightPart.Contains('='))
                addressPart = rightPart.Substring(rightPart.LastIndexOf(':') + 1, rightPart.LastIndexOf('=') - rightPart.LastIndexOf(':') - 1);
            else
                addressPart = rightPart;

            return ulong.TryParse(addressPart, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out address);
        }

        private static Map[] EliminateDuplicates(List<Map> maps)
        {
            var unique = new HashSet<Code>(CodeComparer.Instance);

            foreach (var map in maps)
                for (int i = map.Instructions.Count - 1; i >= 0; i--)
                    if(!unique.Add(map.Instructions[i]))
                        map.Instructions.RemoveAt(i);

            return maps.Where(map => map.Instructions.Any()).ToArray();
        }

        private static DisassembledMethod CreateEmpty(ClrMethod method, string reason)
            => DisassembledMethod.Empty(method.GetFullSignature(), method.NativeCode, reason);

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

    internal class Settings
    {
        internal Settings(int processId, string typeName, string methodName, bool printAsm,bool printSource, bool printPrologAndEpilog, int recursiveDepth, string resultsPath)
        {
            ProcessId = processId;
            TypeName = typeName;
            MethodName = methodName;
            PrintAsm = printAsm;
            PrintSource = printSource;
            PrintPrologAndEpilog = printPrologAndEpilog;
            RecursiveDepth = methodName == DisassemblerConstants.DisassemblerEntryMethodName && recursiveDepth != int.MaxValue ? recursiveDepth + 1 : recursiveDepth;
            ResultsPath = resultsPath;
        }

        internal int ProcessId { get; }
        internal string TypeName { get; }
        internal string MethodName { get; }
        internal bool PrintAsm { get; }
        internal bool PrintSource { get; }
        internal bool PrintPrologAndEpilog { get; }
        internal int RecursiveDepth { get; }
        internal string ResultsPath { get; }

        internal static Settings FromArgs(string[] args)
            => new Settings(
                processId: int.Parse(args[0]),
                typeName: args[1],
                methodName: args[2],
                printAsm: bool.Parse(args[3]),
                printSource: bool.Parse(args[4]),
                printPrologAndEpilog: bool.Parse(args[5]),
                recursiveDepth: int.Parse(args[6]),
                resultsPath: args[7]
            );
    }

    class State
    {
        internal State(ClrRuntime runtime)
        {
            Runtime = runtime;
            Todo = new Queue<MethodInfo>();
            HandledMethods = new HashSet<MethodId>();
        }

        internal ClrRuntime Runtime { get; }
        internal Queue<MethodInfo> Todo { get; }
        internal HashSet<MethodId> HandledMethods { get; }
    }

    struct MethodInfo // I am not using ValueTuple here (would be perfect) to keep the number of dependencies as low as possible
    {
        internal ClrMethod Method { get; }
        internal int Depth { get; }

        internal MethodInfo(ClrMethod method, int depth)
        {
            Method = method;
            Depth = depth;
        }
    }

    struct MethodId : IEquatable<MethodId>
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
