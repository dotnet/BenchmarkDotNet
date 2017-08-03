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
        // 3. print it to std out
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

        private static List<DisassembledMethod> Disassemble(Settings settings, ClrRuntime runtime, State state)
        {
            var result = new List<DisassembledMethod>();

            var typeWithBenchmark = runtime.Heap.GetTypeByName(settings.TypeName);

            state.Todo.Enqueue(typeWithBenchmark.Methods
                    .Single(method => method.Name == settings.MethodName)); // benchmarks in BenchmarkDotNet are always parameterless, so check by name is enough as of today

            while (state.Todo.Count != 0)
            {
                var method = state.Todo.Dequeue();

                if (!state.HandledMetadataTokens.Add(method.MetadataToken)) // add it now to avoid StackOverflow for recursive methods
                    continue; // already handled

                result.Add(DisassembleMethod(method, state, settings));

                if (!settings.PrintRecursive)
                    break;
            }

            return result;
        }

        static DisassembledMethod DisassembleMethod(ClrMethod method, State state, Settings settings)
        {
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(method.Type.Module.FileName);
            var typeDefinition = assemblyDefinition.MainModule.GetType(ClrmdNameToCecilName(method.Type.Name));
            var methodDefinition = typeDefinition.Methods.Single(m => m.MetadataToken.ToUInt32() == method.MetadataToken);

            ICollection<Instruction> ilInstructions =
                methodDefinition.IsAbstract
                    ? Array.Empty<Instruction>() // abstract methods have no implementation
                    : (ICollection<Instruction>)methodDefinition.Body.Instructions;

            EnqueueAllCalls(state, ilInstructions);

            if (method.NativeCode == ulong.MaxValue)
                return DisassembledMethod.Empty(method.GetFullSignature(), method.NativeCode, "Method got inlined");

            if (method.ILOffsetMap == null)
                return DisassembledMethod.Empty(method.GetFullSignature(), method.NativeCode, "No ILOffsetMap found");

            var instructions = new List<Code>();

            var mapByILOffset = (from map in method.ILOffsetMap
                                 where map.ILOffset >= 0 // prolog is -2, epilog -3
                                 where map.StartAddress <= map.EndAddress
                                 orderby map.ILOffset
                                 select map).ToArray();

            if (mapByILOffset.Length == 0 && settings.PrintIL)
            {
                // The method doesn't have an offset map. Just print the whole thing.
                instructions.AddRange(GetIL(ilInstructions));
            }

            var prolog = method.ILOffsetMap.First();
            if (prolog.ILOffset == -2 && settings.PrintPrologAndEpilog) // -2 is a magic number for prolog
                instructions.AddRange(GetAsm(prolog, state, Array.Empty<Instruction>()));

            for (int i = 0; i < mapByILOffset.Length; ++i)
            {
                var map = mapByILOffset[i];
                var nextMap = i == mapByILOffset.Length - 1
                    ? new ILToNativeMap { ILOffset = int.MaxValue }
                    : mapByILOffset[i + 1];

                var correspondingIL = ilInstructions
                    .Where(instr => instr.Offset >= map.ILOffset && instr.Offset < nextMap.ILOffset).ToArray();

                if (settings.PrintSource)
                    instructions.AddRange(GetSource(method, map));

                if (settings.PrintIL)
                    instructions.AddRange(GetIL(correspondingIL));

                if (settings.PrintAsm)
                    instructions.AddRange(GetAsm(map, state, correspondingIL));
            }

            var epilog = method.ILOffsetMap.Last();
            if (epilog.ILOffset == -3 && settings.PrintPrologAndEpilog) // -3 is a magic number for epilog
                instructions.AddRange(GetAsm(epilog, state, Array.Empty<Instruction>()));

            return new DisassembledMethod
            {
                Instructions = instructions.ToArray(),
                Name = method.GetFullSignature(),
                NativeCode = method.NativeCode
            };
        }

        static IEnumerable<IL> GetIL(ICollection<Instruction> instructions)
            => instructions.Select(instruction => new IL { TextRepresentation = instruction.ToString() });

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
                        TextRepresentation = sourceLine + Environment.NewLine + new string(' ', sourceLocation.ColStart - 1) + new string('^', sourceLocation.ColEnd - sourceLocation.ColStart)
                    };
                }
            }
        }

        static IEnumerable<Asm> GetAsm(ILToNativeMap map, State state, ICollection<Instruction> correspondingIL)
        {
            var disasmBuffer = new StringBuilder(512);
            ulong disasmAddress = map.StartAddress;
            while (true)
            {
                int hr = state.DebugControl.Disassemble(disasmAddress, 0,
                    disasmBuffer, disasmBuffer.Capacity, out uint disasmSize,
                    out ulong nextInstr);
                if (hr != 0)
                    break;

                disasmBuffer.Replace("\n", string.Empty);

                var textRepresentation = disasmBuffer.ToString();

                string calledMethodName = null;

                if (textRepresentation.Contains("call"))
                    calledMethodName = TryEnqueueCalledMethod(textRepresentation, state, correspondingIL);

                yield return new Asm
                {
                    TextRepresentation = disasmBuffer.ToString(),
                    Comment = calledMethodName,
                    InstructionPointer = disasmAddress
                };

                if (nextInstr >= map.EndAddress)
                    break;

                disasmAddress = nextInstr;
            }
        }

        static string ReadSourceLine(string file, int line)
        {
            string[] contents;
            if (!SourceFileCache.TryGetValue(file, out contents))
            {
                contents = File.ReadAllLines(file);
                SourceFileCache.Add(file, contents);
            }

            return line - 1 < contents.Length
                ? contents[line - 1]
                : null; // "nop" can have no corresponding c# code ;)
        }

        static string TryEnqueueCalledMethod(string textRepresentation, State state, ICollection<Instruction> correspondingIL)
        {
            if (!TryGetHexAdress(textRepresentation, out ulong address))
                return null; // call    qword ptr [rax+20h] = indirect calls handled by EnqueueIndirectCalls

            var method = state.Runtime.GetMethodByAddress(address);

            if (method == null) // not managed method
                return "not managed method";

            if (!state.HandledMetadataTokens.Contains(method.MetadataToken))
                state.Todo.Enqueue(method);

            return $"{method.Type.Name}.{method.Name}"; // method.GetFullSignature(); produces detailed, but too long string
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

        static void EnqueueAllCalls(State state, ICollection<Instruction> ilInstructions)
        {
            // let's try to enqueue all method calls that we can find in IL and were not printed yet
            foreach (var callInstruction in ilInstructions.Where(instruction => instruction.Operand is MethodReference))
            {
                MethodReference methodReference = (MethodReference)callInstruction.Operand;
                var typeName = CecilNameToClrmdName(methodReference.DeclaringType.FullName);
                var declaringType = state.Runtime.Heap.GetTypeByName(typeName);

                var calledMethod = declaringType.Methods
                    .SingleOrDefault(m => m.MetadataToken == methodReference.MetadataToken.ToUInt32());

                if (calledMethod == default(ClrMethod))
                {
                    // comparing metadata tokens does not work correctly for some NGENed types like Random.Next
                    // Mono.Cecil reports different metadata token value than ClrMD for the same method 
                    // so the last chance is to try to match them by... name (I don't like it, but I have no better idea for now)
                    var unifiedSignature = UnifyName(methodReference);

                    calledMethod = declaringType
                        .Methods
                        .SingleOrDefault(method => method.GetFullSignature() == unifiedSignature);
                }

                if (calledMethod != null && !state.HandledMetadataTokens.Contains(calledMethod.MetadataToken))
                    state.Todo.Enqueue(calledMethod);
            }
        }

        static string UnifyName(MethodReference method)
        {
            // Cecil returns sth like "System.Int32 System.Random::Next(System.Int32,System.Int32)"
            // Clrmd expects sth like "System.Random.Next(Int32, Int32)
            // this method does not handle generic method/types and nested types

            return $"{method.DeclaringType.Namespace}.{method.DeclaringType.Name}.{method.Name}({string.Join(", ", method.Parameters.Select(param => param.ParameterType.Name))})";
        }

        // nested types contains `/` instead of `+` in the name..
        static string CecilNameToClrmdName(string typeName) => typeName.Replace('/', '+');
        static string ClrmdNameToCecilName(string typeName) => typeName.Replace('+', '/');
    }

    class Settings
    {
        private Settings(int processId, string typeName, string methodName, bool printAsm, bool printIL, bool printSource, bool printPrologAndEpilog, bool printRecursive, string resultsPath)
        {
            ProcessId = processId;
            TypeName = typeName;
            MethodName = methodName;
            PrintAsm = printAsm;
            PrintIL = printIL;
            PrintSource = printSource;
            PrintPrologAndEpilog = printPrologAndEpilog;
            PrintRecursive = printRecursive;
            ResultsPath = resultsPath;
        }

        public int ProcessId { get; }
        public string TypeName { get; }
        public string MethodName { get; }
        public bool PrintAsm { get; }
        public bool PrintIL { get; }
        public bool PrintSource { get; }
        public bool PrintPrologAndEpilog { get; }
        public bool PrintRecursive { get; }
        public string ResultsPath { get; }

        public static Settings FromArgs(string[] args)
            => new Settings(
                processId: int.Parse(args[0]),
                typeName: args[1],
                methodName: args[2],
                printAsm: bool.Parse(args[3]),
                printIL: bool.Parse(args[4]),
                printSource: bool.Parse(args[5]),
                printPrologAndEpilog: bool.Parse(args[6]),
                printRecursive: bool.Parse(args[7]),
                resultsPath: args[8]
            );
    }

    class State
    {
        public State(ClrRuntime runtime, IDebugControl debugControl)
        {
            Runtime = runtime;
            DebugControl = debugControl;
            Todo = new Queue<ClrMethod>();
            HandledMetadataTokens = new HashSet<uint>();
        }

        public ClrRuntime Runtime { get; }
        public IDebugControl DebugControl { get; }
        public Queue<ClrMethod> Todo { get; }
        public HashSet<uint> HandledMetadataTokens { get; }
    }
}
