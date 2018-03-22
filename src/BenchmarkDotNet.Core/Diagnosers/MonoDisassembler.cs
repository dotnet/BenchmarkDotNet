using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
            Debug.Assert(mono == null || !RuntimeInformation.IsMono, "Must never be called for Non-Mono benchmarks");

            var benchmarkTarget = benchmark.Target;
            string fqnMethod = GetMethodName(benchmarkTarget);
            string exePath = benchmarkTarget.Type.GetTypeInfo().Assembly.Location;
            
            var environmentVariables = new Dictionary<string, string> { ["MONO_VERBOSE_METHOD"] = fqnMethod };
            string monoPath = mono?.CustomPath ?? "mono";
            string arguments = $"--compile {fqnMethod} {exePath}";
            
            var output = ProcessHelper.RunAndReadOutputLineByLine(monoPath, arguments, environmentVariables);
            string commandLine = $"{GetEnvironmentVariables(environmentVariables)} {monoPath} {arguments}";
            
            return OutputParser.Parse(output, benchmarkTarget.Method.Name, commandLine);
        }

        private static string GetEnvironmentVariables(Dictionary<string, string> environmentVariables) 
            => string.Join(" ", environmentVariables.Select(e => $"{e.Key}={e.Value}"));

        private static string GetMethodName(Target target)
            => $"{target.Type.GetTypeInfo().Namespace}.{target.Type.GetTypeInfo().Name}:{target.Method.Name}";

        internal static class OutputParser
        {
            internal static DisassemblyResult Parse(IReadOnlyList<string> input, string methodName, string commandLine)
            {
                var instructions = new List<Code>();

                var listing = input.SkipWhile(i => !i.Contains("(__TEXT,__text) section") && !i.Contains("Disassembly of section .text:"))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Skip(2);

                foreach (string line in listing)
                    if (TryParseInstruction(line, out var instruction))
                        instructions.Add(instruction);

                return new DisassemblyResult
                {
                    Methods = new[]
                    {
                        new DisassembledMethod
                        {
                            Name = methodName,
                            Maps = new[] { new Map { Instructions = instructions.ToArray() } },
                            CommandLine = commandLine
                        }
                    }
                };
            }
            
            //line example 1:  0:	48 83 ec 28          	sub    $0x28,%rsp
            //line example 2: 0000000000000000	subq	$0x28, %rsp
            private static Regex instructionRegex = new Regex(@"\s*(?<address>[0-9a-f]+)(\:\s+([0-9a-f]{2}\s+)+)?\s+(?<instruction>.*)\s*");

            public static bool TryParseInstruction(string line, out Code instruction)
            {
                instruction = null;
                var match = instructionRegex.Match(line);
                if (!match.Success)
                    return false;

                instruction = new Code { TextRepresentation = match.Groups["instruction"].ToString() };
                return true;
            }
        }
    }
}