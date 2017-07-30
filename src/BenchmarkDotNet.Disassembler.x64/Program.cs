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

                var state = new State(runtime, (IDebugControl)dataTarget.DebuggerInterface);

                var typeWithBenchmark = runtime.Heap.GetTypeByName(settings.TypeName);

                state.Todo.Enqueue(typeWithBenchmark.Methods
                    .Single(method => method.Name == settings.MethodName)); // benchmarks in BenchmarkDotNet are always parameterless, so check by name is enough as of today

                while (state.Todo.Count != 0)
                {
                    var method = state.Todo.Dequeue();

                    if (!state.HandledMetadataTokens.Add(method.MetadataToken)) // add it now to avoid StackOverflow for recursive methods
                        continue; // already handled

                    DisassembleMethod(method, state, settings);

                    if (!settings.PrintRecursive)
                        return;
                }
            }
        }

        static void DisassembleMethod(ClrMethod method, State state, Settings settings)
        {
            if (method.NativeCode == ulong.MaxValue) // no implementation or not compiled yet (not sure)
                return;

            Console.WriteLine($"Disassembly for {method.GetFullSignature()} ({method.NativeCode:X})"); // :X is for hex

            var module = method.Type.Module;
            string fileName = module.FileName;

            var assemblyDefinition = AssemblyDefinition.ReadAssembly(fileName);
            var typeDefinition = assemblyDefinition.MainModule.GetType(method.Type.Name);
            var methodDefinition = typeDefinition.Methods.Single(m => m.MetadataToken.ToUInt32() == method.MetadataToken);
            ICollection<Instruction> ilInstructions = methodDefinition.Body?.Instructions;

            if (method.ILOffsetMap == null)
                return;

            var mapByILOffset = (from map in method.ILOffsetMap
                                 where map.ILOffset >= 0 // prolog is -2, epilog -3
                                 where map.StartAddress <= map.EndAddress
                                 orderby map.ILOffset
                                 select map).ToArray();

            if (mapByILOffset.Length == 0 && settings.PrintIL)
            {
                // The method doesn't have an offset map. Just print the whole thing.
                PrintIL(ilInstructions);
            }

            var prolog = method.ILOffsetMap.First();
            if (prolog.ILOffset == -2 && settings.PrintPrologAndEpilog) // -2 is a magic number for prolog
                PrintAsm(prolog, state, Array.Empty<Instruction>());

            for (int i = 0; i < mapByILOffset.Length; ++i)
            {
                var map = mapByILOffset[i];
                var nextMap = i == mapByILOffset.Length - 1
                    ? new ILToNativeMap { ILOffset = int.MaxValue }
                    : mapByILOffset[i + 1];

                var correspondingIL = ilInstructions
                    .Where(instr => instr.Offset >= map.ILOffset && instr.Offset < nextMap.ILOffset).ToArray();

                if (settings.PrintSource)
                    PrintSource(method, map);

                if (settings.PrintIL)
                    PrintIL(correspondingIL);

                if (settings.PrintAsm)
                    PrintAsm(map, state, correspondingIL);
            }

            var epilog = method.ILOffsetMap.Last();
            if (epilog.ILOffset == -3 && settings.PrintPrologAndEpilog) // -3 is a magic number for epilog
                PrintAsm(epilog, state, Array.Empty<Instruction>());

            Console.WriteLine();
        }

        static void PrintSource(ClrMethod method, ILToNativeMap map)
        {
            var sourceLocation = method.GetSourceLocation(map.ILOffset);
            if (sourceLocation == null)
                return;

            for (int line = sourceLocation.LineNumber; line <= sourceLocation.LineNumberEnd; ++line)
            {
                var sourceLine = ReadSourceLine(sourceLocation.FilePath, line);

                if (sourceLine != null)
                {
                    Console.WriteLine(sourceLine);
                    Console.WriteLine(new string(' ', sourceLocation.ColStart - 1) + new string('^', sourceLocation.ColEnd - sourceLocation.ColStart));
                }
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

        static void PrintIL(ICollection<Instruction> instructions)
        {
            foreach (var instr in instructions)
            {
                Console.WriteLine(instr.ToString());
            }
        }

        static void PrintAsm(ILToNativeMap map, State state, ICollection<Instruction> correspondingIL)
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

                var textRepresentation = disasmBuffer.ToString();

                if (textRepresentation.Contains("call"))
                {
                    var calledMethodName = TryEnqueueCalledMethod(textRepresentation, state, correspondingIL);

                    disasmBuffer.Insert(
                        (int)disasmSize - 2, // it always end with "\n"
                        $"\t; {calledMethodName}");
                }

                Console.Write(disasmBuffer.ToString());

                if (nextInstr >= map.EndAddress)
                    break;
                disasmAddress = nextInstr;
            }
        }

        static string TryEnqueueCalledMethod(string textRepresentation, State state, ICollection<Instruction> correspondingIL)
        {
            if (TryGetHexAdress(textRepresentation, out ulong address))
            {
                var method = state.Runtime.GetMethodByAddress(address);

                if (method == null) // not managed method
                    return "?? not managed method";

                if (!state.HandledMetadataTokens.Contains(method.MetadataToken))
                    state.Todo.Enqueue(method);

                return $"{method.Type.Name}.{method.Name}";
            }
            else // call    qword ptr [rax+20h] 
            {
                // let's try to enqueue all method calls that we can find in IL and were not printed yet
                var callInstructions = correspondingIL.Where(
                    instruction => instruction.OpCode == OpCodes.Call
                        || instruction.OpCode == OpCodes.Calli
                        || instruction.OpCode == OpCodes.Callvirt);

                foreach (var callInstruction in callInstructions)
                {
                    if (callInstruction.Operand is MethodReference methodReference)
                    {
                        var declaringType = state.Runtime.Heap.GetTypeByName(methodReference.DeclaringType.FullName);

                        var calledMethod = declaringType.Methods
                            .SingleOrDefault(m => m.MetadataToken == methodReference.MetadataToken.ToUInt32());

                        if (calledMethod == default(ClrMethod))
                        {
                            // comparing metadata tokens does not work correctly for some NGENed types like Random.Next
                            // Mono.Cecil reports different metadat token value than Clrmd for the same method 
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
            }

            return null; // todo: get the right name
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

        private static string UnifyName(MethodReference method)
        {
            // Cecil returns sth like "System.Int32 System.Random::Next(System.Int32,System.Int32)"
            // Clrmd expects sth like "System.Random.Next(Int32, Int32)
            // this method does not handle generic method/types and nested types

            return $"{method.DeclaringType.Namespace}.{method.DeclaringType.Name}.{method.Name}({string.Join(", ", method.Parameters.Select(param => param.ParameterType.Name))})";
        }
    }

    class Settings
    {
        private Settings(int processId, string typeName, string methodName, bool printAsm, bool printIL, bool printSource, bool printPrologAndEpilog, bool printRecursive)
        {
            ProcessId = processId;
            TypeName = typeName;
            MethodName = methodName;
            PrintAsm = printAsm;
            PrintIL = printIL;
            PrintSource = printSource;
            PrintPrologAndEpilog = printPrologAndEpilog;
            PrintRecursive = printRecursive;
        }

        public int ProcessId { get; }
        public string TypeName { get; }
        public string MethodName { get; }
        public bool PrintAsm { get; }
        public bool PrintIL { get; }
        public bool PrintSource { get; }
        public bool PrintPrologAndEpilog { get; }
        public bool PrintRecursive { get; }

        public static Settings FromArgs(string[] args)
            => new Settings(
                processId: int.Parse(args[0]),
                typeName: args[1],
                methodName: args[2],
                printAsm: bool.Parse(args[3]),
                printIL: bool.Parse(args[4]),
                printSource: bool.Parse(args[5]),
                printPrologAndEpilog: bool.Parse(args[6]),
                printRecursive: bool.Parse(args[7])
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
