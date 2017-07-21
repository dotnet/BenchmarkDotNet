using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interop;
using Microsoft.Diagnostics.RuntimeExt;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace BenchmarkDotNet.Disassembler
{
    class Program
    {
        static readonly Dictionary<string, string[]> SourceFileCache = new Dictionary<string, string[]>();

        // one can only attach to a process of same target architecture, this is why we need exe for x64 and for x86
        //
        // the goals of the existence of this process: 
        // 1. attach to benchmarked process
        // 2. dissasemble the code
        // 3. print it to std out
        // 4. detach & shut down
        //
        // requirements: must not have any dependencies to BenchmarkDotNet itself, KISS
        static void Main(string[] args)
        {
            var options = Options.FromArgs(args);

            if (Process.GetProcessById(options.ProcessId).HasExited) // possible when benchmark has already finished
                throw new Exception("It's already dead"); // if we don't throw here the Clrmd will fail with some mysterious HRESULT: 0xd000010a ;)

            try
            {
                Handle(options);
            }
            catch (OutOfMemoryException) // thrown by clrmd when pdb is missing or in invalid format
            {
                Console.WriteLine("\\ ---------------------------");
                Console.WriteLine("Failed to read source code location!");
                Console.WriteLine("Please make sure, that the project which defines benchmarks contains following settings:");
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

        static void Handle(Options options)
        {
            using (var dataTarget = DataTarget.AttachToProcess(
                options.ProcessId,
                (uint)TimeSpan.FromMilliseconds(5000).TotalMilliseconds,
                AttachFlag.Invasive))
            {
                var runtime = dataTarget.ClrVersions.Single().CreateRuntime();

                var typeWithBenchmark = runtime.Heap.GetTypeByName(options.TypeName);

                var benchmarkedMethod = typeWithBenchmark
                    .Methods
                    .Single(method => method.Name == options.MethodName);

                DisassembleMethod(benchmarkedMethod, (IDebugControl)dataTarget.DebuggerInterface, options);
            }
        }

        static void DisassembleMethod(ClrMethod method, IDebugControl debugControl, Options options)
        {
            var module = method.Type.Module;
            string fileName = module.FileName;

            var assemblyDefinition = AssemblyDefinition.ReadAssembly(fileName);
            var typeDefinition = assemblyDefinition.MainModule.GetType(method.Type.Name);
            var methodDefinition = typeDefinition.Methods.Single(m => m.MetadataToken.ToUInt32() == method.MetadataToken);
            var ilInstructions = methodDefinition.Body.Instructions;

            if (method.ILOffsetMap == null)
                return;

            var mapByIlOffset = (from map in method.ILOffsetMap
                               where map.ILOffset >= 0 // prolog is -2, epilog -3
                               where map.StartAddress <= map.EndAddress
                               orderby map.ILOffset
                               select map).ToArray();

            if (mapByIlOffset.Length == 0 && options.PrintIl)
            {
                // The method doesn't have an offset map. Just print the whole thing.
                PrintInstructions(ilInstructions);
            }

            var prolog = method.ILOffsetMap[0];
            if (prolog.ILOffset == -2 && options.PrintPrologAndEpilog) // -2 is a magic number for prolog
                DisassembleNative(prolog, debugControl);

            for (int i = 0; i < mapByIlOffset.Length; ++i)
            {
                var map = mapByIlOffset[i];
                var nextMap = i == mapByIlOffset.Length - 1 
                    ? new ILToNativeMap { ILOffset = int.MaxValue }
                    : mapByIlOffset[i + 1];

                if (options.PrintSource)
                    PrintSource(method, map);

                if (options.PrintIl)
                    PrintInstructions(ilInstructions.Where(instr => instr.Offset >= map.ILOffset && instr.Offset < nextMap.ILOffset));

                if (options.PrintAsm)
                    DisassembleNative(map, debugControl);
            }

            var epilog = method.ILOffsetMap[method.ILOffsetMap.Length - 1];
            if (epilog.ILOffset == -3 && options.PrintPrologAndEpilog) // -3 is a magic number for epilog
                DisassembleNative(epilog, debugControl);
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

        static void PrintInstructions(IEnumerable<Instruction> instructions)
        {
            foreach (var instr in instructions)
            {
                Console.WriteLine(instr.ToString());
            }
        }

        static void DisassembleNative(ILToNativeMap map, IDebugControl debugControl)
        {
            var disasmBuffer = new StringBuilder(512);
            ulong disasmAddress = map.StartAddress;
            while (true)
            {
                ulong nextInstr;
                uint disasmSize;

                int hr = debugControl.Disassemble(disasmAddress, 0,
                    disasmBuffer, disasmBuffer.Capacity, out disasmSize,
                    out nextInstr);
                if (hr != 0)
                    break;
                Console.Write(disasmBuffer.ToString());

                if (nextInstr >= map.EndAddress)
                    break;
                disasmAddress = nextInstr;
            }
        }
    }

    class Options
    {
        private Options(int processId, string typeName, string methodName, bool printAsm, bool printIl, bool printSource, bool printPrologAndEpilog)
        {
            ProcessId = processId;
            TypeName = typeName;
            MethodName = methodName;
            PrintAsm = printAsm;
            PrintIl = printIl;
            PrintSource = printSource;
            PrintPrologAndEpilog = printPrologAndEpilog;
        }

        public int ProcessId { get; }
        public string TypeName { get; }
        public string MethodName { get; }
        public bool PrintAsm { get; }
        public bool PrintIl { get; }
        public bool PrintSource { get; }
        public bool PrintPrologAndEpilog { get; }

        public static Options FromArgs(string[] args)
            => new Options(
                processId: int.Parse(args[0]),
                typeName: args[1],
                methodName: args[2],
                printAsm: bool.Parse(args[3]),
                printIl: bool.Parse(args[4]),
                printSource: bool.Parse(args[5]),
                printPrologAndEpilog: bool.Parse(args[6])
            );
    }
}
