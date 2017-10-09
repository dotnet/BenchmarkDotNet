using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interop;
using Microsoft.Diagnostics.RuntimeExt;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace BenchmarkDotNet.Disassembler
{
    class Program
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
                Handle(options);
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

        static void Handle(Settings settings)
        {
            using (var dataTarget = DataTarget.AttachToProcess(
                settings.ProcessId,
                (uint)TimeSpan.FromMilliseconds(5000).TotalMilliseconds,
                AttachFlag.Invasive))
            {
                var runtime = dataTarget.ClrVersions.Single().CreateRuntime();

                ConfigureSymbols(dataTarget);

                var state = new State(runtime, (IDebugControl)dataTarget.DebuggerInterface);

                var disasembledMethods = Disassemble(settings, runtime, state);

                using (var stream = new FileStream(settings.ResultsPath, FileMode.Append, FileAccess.Write))
                using (var writer = XmlWriter.Create(stream))
                {
                    var serializer = new XmlSerializer(typeof(DisassemblyResult));

                    serializer.Serialize(writer, new DisassemblyResult { Methods = disasembledMethods.ToArray() });
                }
            }
        }

        static void ConfigureSymbols(DataTarget dataTarget)
        {
            // code copied from https://github.com/Microsoft/clrmd/issues/34#issuecomment-161926535
            var symbols = dataTarget.DebuggerInterface as IDebugSymbols;
            symbols?.SetSymbolPath("http://msdl.microsoft.com/download/symbols");
            var control = dataTarget.DebuggerInterface as IDebugControl;
            control?.Execute(DEBUG_OUTCTL.NOT_LOGGED, ".reload", DEBUG_EXECUTE.NOT_LOGGED);
        }

        static List<DisassembledMethod> Disassemble(Settings settings, ClrRuntime runtime, State state)
        {
            var result = new List<DisassembledMethod>();

            var typeWithBenchmark = runtime.Heap.GetTypeByName(settings.TypeName);

            state.Todo.Enqueue(
                new MethodInfo(
                    // benchmarks in BenchmarkDotNet are always parameterless, so check by name is enough as of today
                    typeWithBenchmark.Methods.Single(method => method.IsPublic && method.Name == settings.MethodName && method.GetFullSignature().EndsWith("()")), 
                    0)); 

            while (state.Todo.Count != 0)
            {
                var method = state.Todo.Dequeue();

                if (!state.HandledMetadataTokens.Add(method.Method.MetadataToken)) // add it now to avoid StackOverflow for recursive methods
                    continue; // already handled

                if(settings.RecursiveDepth >= method.Depth)
                    result.Add(DisassembleMethod(method, state, settings));
            }

            return result;
        }

        static DisassembledMethod DisassembleMethod(MethodInfo methodInfo, State state, Settings settings)
        {
            var method = methodInfo.Method;
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(method.Type.Module.FileName);
            if (assemblyDefinition == null)
                return CreateEmpty(method, "Can't read assembly definition");

            var typeDefinition = assemblyDefinition.MainModule.GetTypes().SingleOrDefault(type => type.MetadataToken.ToUInt32() == method.Type.MetadataToken);
            if (typeDefinition == null)
                return CreateEmpty(method, $"Can't find {method.Type.Name} in {assemblyDefinition.Name}");

            var methodDefinition = typeDefinition.Methods.Single(m => m.MetadataToken.ToUInt32() == method.MetadataToken);
            if (methodDefinition == null)
                return CreateEmpty(method, $"Can't find {method.Name} of {typeDefinition.Name}");

            // some methods have no implementation (abstract & CLR magic)
            var ilInstructions = (ICollection<Instruction>)methodDefinition.Body?.Instructions ?? Array.Empty<Instruction>();

            EnqueueAllCalls(state, ilInstructions, methodInfo.Depth); 

            if (method.NativeCode == ulong.MaxValue)
                if(method.IsAbstract) return CreateEmpty(method, "Abstract method");
                else if (method.IsVirtual) CreateEmpty(method, "Virtual method");
                else return CreateEmpty(method, "Method got inlined");

            if (method.ILOffsetMap == null)
                return CreateEmpty(method, "No ILOffsetMap found");

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

            if (mapByStartAddress.Length == 0 && settings.PrintIL)
            {
                // The method doesn't have an offset map. Just print the whole thing.
                maps.Add(CreateMap(GetIL(ilInstructions)));
            }

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

                var correspondingIL = Array.Empty<Instruction>();

                if (map.ILOffset >= 0)
                {
                    var ilOffsetIndex = Array.IndexOf(sortedUniqueILOffsets, map.ILOffset);
                    var nextILOffsetIndex = ilOffsetIndex + 1;

                    // method.GetILOffset(map.EndAddress); is not enough to get the endILOffset (it returns startILOffset)
                    var nextMapILOffset = nextILOffsetIndex < sortedUniqueILOffsets.Length
                        ? sortedUniqueILOffsets[nextILOffsetIndex]
                        : int.MaxValue;

                    correspondingIL = ilInstructions
                        .Where(instr => instr.Offset >= map.ILOffset && instr.Offset < nextMapILOffset)
                        .OrderBy(instr => instr.Offset) // just to make sure the Cecil instructions are also sorted in the right way
                        .ToArray();
                }

                if (settings.PrintSource && map.ILOffset >= 0)
                    group.AddRange(GetSource(method, map));

                if (settings.PrintIL && map.ILOffset >= 0)
                    group.AddRange(GetIL(correspondingIL));

                if (settings.PrintAsm)
                    group.AddRange(GetAsm(map, state, methodInfo.Depth));

                maps.Add(new Map { Instructions = group });
            }

            return new DisassembledMethod
            {
                Maps = EliminateDuplicates(maps),
                Name = method.GetFullSignature(),
                NativeCode = method.NativeCode
            };
        }

        static Map CreateMap(IEnumerable<Code> instructions) => new Map { Instructions = instructions.ToList()  };

        static IEnumerable<IL> GetIL(ICollection<Instruction> instructions)
            => instructions.Select(instruction 
                => new IL
                {
                    TextRepresentation = instruction.ToString(),
                    Offset = instruction.Offset
                });

        static IEnumerable<Sharp> GetSource(ClrMethod method, ILToNativeMap map)
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
                        TextRepresentation = sourceLine + Environment.NewLine + new string(' ', sourceLocation.ColStart - 1) + new string('^', sourceLocation.ColEnd - sourceLocation.ColStart),
                        FilePath = sourceLocation.FilePath,
                        LineNumber = line
                    };
                }
            }
        }

        static IEnumerable<Asm> GetAsm(ILToNativeMap map, State state, int depth)
        {
            var disasmBuffer = new StringBuilder(512);
            ulong disasmAddress = map.StartAddress;
            while (true)
            {
                int hr = state.DebugControl.Disassemble(disasmAddress, 0,
                    disasmBuffer, disasmBuffer.Capacity, out uint disasmSize,
                    out ulong endOffset);
                if (hr != 0)
                    break;

                disasmBuffer.Replace("\n", string.Empty);

                var textRepresentation = disasmBuffer.ToString();

                string calledMethodName = null;

                if (textRepresentation.Contains("call"))
                    calledMethodName = TryEnqueueCalledMethod(textRepresentation, state, depth);

                yield return new Asm
                {
                    TextRepresentation = disasmBuffer.ToString(),
                    Comment = calledMethodName,
                    StartAddress = disasmAddress,
                    EndAddress = endOffset
                };

                if (endOffset >= map.EndAddress)
                    break;

                disasmAddress = endOffset;
            }
        }

        static string ReadSourceLine(string file, int line)
        {
            string[] contents;
            if (!SourceFileCache.TryGetValue(file, out contents))
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

        static string TryEnqueueCalledMethod(string textRepresentation, State state, int depth)
        {
            if (!TryGetHexAdress(textRepresentation, out ulong address))
                return null; // call    qword ptr [rax+20h] // needs further research

            var method = state.Runtime.GetMethodByAddress(address);

            if (method == null) // not managed method
                return "not managed method";

            if (!state.HandledMetadataTokens.Contains(method.MetadataToken))
                state.Todo.Enqueue(new MethodInfo(method, depth + 1));

            return method.GetFullSignature();
        }

        static bool TryGetHexAdress(string textRepresentation, out ulong address)
        {
            // it's always "something call something addr`ess something"
            // 00007ffe`16fb04e4 e897fbffff      call    00007ffe`16fb0080 // static or instance method call
            // 000007fe`979171fb e800261b5e      call    mscorlib_ni+0x499800 (000007fe`f5ac9800) // managed implementation in mscorlib 
            // 000007fe`979f666f 41ff13          call    qword ptr [r11] ds:000007fe`978e0050=000007fe978ed260
            // 00007ffe`16fc0503 e81820615f      call    clr+0x2520 (00007ffe`765d2520) // native implementation in Clr
            var rightPart = textRepresentation
                .Split(CallSeparator, StringSplitOptions.RemoveEmptyEntries).Last() // take the right part
                .Trim() // remove leading whitespaces
                .Replace("`", string.Empty); // remove the magic delimeter

            string addressPart = string.Empty;
            if (rightPart.Contains('(') && rightPart.Contains(')'))
                addressPart = rightPart.Substring(rightPart.LastIndexOf('(') + 1, rightPart.LastIndexOf(')') - rightPart.LastIndexOf('(') - 1);
            else if (rightPart.Contains(':') && rightPart.Contains('='))
                addressPart = rightPart.Substring(rightPart.LastIndexOf(':') + 1, rightPart.LastIndexOf('=') - rightPart.LastIndexOf(':') - 1);
            else
                addressPart = rightPart;

            return ulong.TryParse(addressPart, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out address);
        }

        /// <summary>
        /// for some calls we can not parse the method address from asm, so we just get it from IL
        /// </summary>
        static void EnqueueAllCalls(State state, ICollection<Instruction> ilInstructions, int depth)
        {
            // let's try to enqueue all method calls that we can find in IL and were not printed yet
            foreach (var callInstruction in ilInstructions.Where(instruction => instruction.Operand is MethodReference)) // todo: handle CallSite
            {
                var methodReference = (MethodReference)callInstruction.Operand;

                var declaringType = 
                    methodReference.DeclaringType.IsNested
                        ? state.Runtime.Heap.GetTypeByName(methodReference.DeclaringType.FullName.Replace('/', '+')) // nested types contains `/` instead of `+` in the name..
                        : state.Runtime.Heap.GetTypeByName(methodReference.DeclaringType.FullName);

                if(declaringType == null)
                    continue; // todo: eliminate Cecil vs ClrMD differences in searching by name

                var calledMethod = GetMethod(methodReference, declaringType);
                if (calledMethod != null && !state.HandledMetadataTokens.Contains(calledMethod.MetadataToken))
                    state.Todo.Enqueue(new MethodInfo(calledMethod, depth + 1));
            }
        }

        static ClrMethod GetMethod(MethodReference methodReference, ClrType declaringType)
        {
            var methodsWithSameToken = declaringType.Methods
                .Where(method => method.MetadataToken == methodReference.MetadataToken.ToUInt32()).ToArray();

            if (methodsWithSameToken.Length == 1) // the most common case
                return methodsWithSameToken[0];
            if (methodsWithSameToken.Length > 1 && methodReference.MetadataToken.ToUInt32() != default(UInt32)) 
                // usually one is NGened, the other one is not compiled (looks like a ClrMD bug to me)
                return methodsWithSameToken.Single(method => method.CompilationType != MethodCompilationType.None);

            // comparing metadata tokens does not work correctly for some NGENed types like Random.Next, System.Threading.Monitor & more
            // Mono.Cecil reports different metadata token value than ClrMD for the same method 
            // so the last chance is to try to match them by... name (I don't like it, but I have no better idea for now)
            var unifiedSignature = CecilNameToClrmdName(methodReference);

            return declaringType
                .Methods
                .SingleOrDefault(method => method.GetFullSignature() == unifiedSignature);
        }


        static string CecilNameToClrmdName(MethodReference method)
        {
            // Cecil returns sth like "System.Int32 System.Random::Next(System.Int32,System.Int32)"
            // ClrMD expects sth like "System.Random.Next(Int32, Int32)
            // this method does not handle generic method/types and nested types

            return $"{method.DeclaringType.Namespace}.{method.DeclaringType.Name}.{method.Name}({string.Join(", ", method.Parameters.Select(CecilNameToClrmdName))})";
        }

        // Cecil returns sth like "Boolean&"
        // ClrMD expects sth like "Boolean ByRef
        static string CecilNameToClrmdName(ParameterDefinition param) 
            => param.ParameterType.IsByReference 
                ? param.ParameterType.Name.Replace("&", string.Empty) + " ByRef"
                : param.ParameterType.Name;

        static Map[] EliminateDuplicates(List<Map> maps)
        {
            var unique = new HashSet<Code>(CodeComparer.Instance);

            foreach (var map in maps)
                for (int i = map.Instructions.Count - 1; i >= 0; i--)
                    if(!unique.Add(map.Instructions[i]))
                        map.Instructions.RemoveAt(i);

            return maps.Where(map => map.Instructions.Any()).ToArray();
        }

        static DisassembledMethod CreateEmpty(ClrMethod method, string reason)
            => DisassembledMethod.Empty(method.GetFullSignature(), method.NativeCode, reason);

        class CodeComparer : IEqualityComparer<Code>
        {
            internal static readonly IEqualityComparer<Code> Instance = new CodeComparer();

            public bool Equals(Code x, Code y)
            {
                if (x.GetType() != y.GetType())
                    return false;

                // sometimes ClrMD reports same address range in two different ILToNativeMaps
                // this happens usualy for prolog and the instruction after prolog
                // and for the epilog and last instruction before epilog
                if (x is Asm asmLeft && y is Asm asmRight)
                    return asmLeft.StartAddress == asmRight.StartAddress && asmLeft.EndAddress == asmRight.EndAddress;

                // sometimes some C# code lines are duplicated because the same line is the best match for multiple ILToNativeMaps
                // we don't want to confuse the users, so this must also be removed
                if (x is Sharp sharpLeft && y is Sharp sharpRight)
                    return sharpLeft.TextRepresentation == sharpRight.TextRepresentation
                        && sharpLeft.FilePath == sharpRight.FilePath
                        && sharpLeft.LineNumber == sharpRight.LineNumber;

                if (x is IL ilLeft && y is IL ilRight)
                    return ilLeft.TextRepresentation == ilRight.TextRepresentation
                        && ilLeft.Offset == ilRight.Offset;

                throw new InvalidOperationException("Impossible");
            }

            public int GetHashCode(Code obj) => obj.TextRepresentation.GetHashCode();
        }
    }

    class Settings
    {
        private Settings(int processId, string typeName, string methodName, bool printAsm, bool printIL, bool printSource, bool printPrologAndEpilog, int recursiveDepth, string resultsPath)
        {
            ProcessId = processId;
            TypeName = typeName;
            MethodName = methodName;
            PrintAsm = printAsm;
            PrintIL = printIL;
            PrintSource = printSource;
            PrintPrologAndEpilog = printPrologAndEpilog;
            RecursiveDepth = recursiveDepth;
            ResultsPath = resultsPath;
        }

        internal int ProcessId { get; }
        internal string TypeName { get; }
        internal string MethodName { get; }
        internal bool PrintAsm { get; }
        internal bool PrintIL { get; }
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
                printIL: bool.Parse(args[4]),
                printSource: bool.Parse(args[5]),
                printPrologAndEpilog: bool.Parse(args[6]),
                recursiveDepth: int.Parse(args[7]),
                resultsPath: args[8]
            );
    }

    class State
    {
        internal State(ClrRuntime runtime, IDebugControl debugControl)
        {
            Runtime = runtime;
            DebugControl = debugControl;
            Todo = new Queue<MethodInfo>();
            HandledMetadataTokens = new HashSet<uint>();
        }

        internal ClrRuntime Runtime { get; }
        internal IDebugControl DebugControl { get; }
        internal Queue<MethodInfo> Todo { get; }
        internal HashSet<uint> HandledMetadataTokens { get; }
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
}
