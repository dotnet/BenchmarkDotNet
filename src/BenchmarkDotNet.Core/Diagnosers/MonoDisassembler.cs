using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Diagnosers
{
    internal class MonoDisassembler
    {
        private readonly bool printAsm, printIL, printSource, printPrologAndEpilog;
        private readonly int recursiveDepth = 1;

        internal MonoDisassembler(DisassemblyDiagnoserConfig config)
        {
            printIL = config.PrintIL;
            printAsm = config.PrintAsm;
            printSource = config.PrintSource;
            printPrologAndEpilog = config.PrintPrologAndEpilog;
            recursiveDepth = config.RecursiveDepth;
        }

        internal DisassemblyResult Disassemble(Benchmark benchmark, MonoRuntime mono)
        {
            Debug.Assert(mono == null || !RuntimeInformation.IsMono(), "Must never be called for Non-Mono benchmarks");

            var monoMethodName = GetMethodName(benchmark.Target);

            var output = ProcessHelper.RunAndReadOutputLineByLine(
                mono?.CustomPath ?? "mono",
                "-v -v -v -v "
                + $"--compile {monoMethodName} "
                + (benchmark.Job.Env.Jit == Jit.Llvm ? "--llvm" : "--nollvm")
                + $" \"{benchmark.Target.Type.GetTypeInfo().Assembly.Location}\"");

            return OutputParser.Parse(output, monoMethodName, benchmark.Target.Method.Name);
        }

        static string GetMethodName(Target target)
            => $"{target.Type.GetTypeInfo().Namespace}.{target.Type.GetTypeInfo().Name}:{target.Method.Name}";

        internal static class OutputParser
        {
            internal static DisassemblyResult Parse(IReadOnlyList<string> input, string monoMethodName, string methodName)
            {
                var instructions = new List<Code>();

                bool found = false;

                foreach (var line in input.Reverse())
                {
                    if (!found)
                    {
                        if (IsStartLine(line, monoMethodName))
                            found = true;

                        continue;
                    }

                    if (IsEndLine(line))
                        break;

                    if (TryParseInstruction(line, out var instruction))
                        instructions.Add(instruction);
                }

                return new DisassemblyResult
                {
                    Methods = new[]
                    {
                        new DisassembledMethod
                        {
                            Name = methodName,
                            Maps = new [] { new Map { Instructions = instructions.ToArray() } }
                        }
                    }
                };
            }

            // the input is sth like "	4  storei4_membase_reg [%edi + 0xc] <- %eax"
            private static bool TryParseInstruction(string line, out Code instruction)
            {
                instruction = null;

                // in the future we could parse it, use Mono.Cecil and combine the il seq point with IL for given method as we do for regular .NET
                if (line.Contains("il_seq_point"))
                    return false;

                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || !char.IsDigit(trimmed[0]))
                    return false;

                int startIndex = 0;
                while (char.IsDigit(trimmed[startIndex]) || char.IsWhiteSpace(trimmed[startIndex]))
                    startIndex++;

                instruction = new Code { TextRepresentation = trimmed.Substring(startIndex) };

                return true;
            }

            private static bool IsEndLine(string line)
                => string.IsNullOrWhiteSpace(line) || line.Contains("liveness");

            private static bool IsStartLine(string line, string methodName)
                => line != null && line.Contains(methodName) && line.Contains("emitted at");
        }
    }
}