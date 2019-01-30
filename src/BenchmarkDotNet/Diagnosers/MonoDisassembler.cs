﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Diagnosers
{
    [SuppressMessage("ReSharper", "NotAccessedField.Local")] // TODO: use config fields
    internal class MonoDisassembler
    {
        private readonly bool printAsm, printIL, printSource, printPrologAndEpilog;
        private readonly int recursiveDepth;

        internal MonoDisassembler(DisassemblyDiagnoserConfig config)
        {
            printIL = config.PrintIL;
            printAsm = config.PrintAsm;
            printSource = config.PrintSource;
            printPrologAndEpilog = config.PrintPrologAndEpilog;
            recursiveDepth = config.RecursiveDepth;
        }

        internal DisassemblyResult Disassemble(BenchmarkCase benchmarkCase, MonoRuntime mono)
        {
            Debug.Assert(mono == null || !RuntimeInformation.IsMono, "Must never be called for Non-Mono benchmarks");

            var benchmarkTarget = benchmarkCase.Descriptor;
            string fqnMethod = GetMethodName(benchmarkTarget);
            string llvmFlag = GetLlvmFlag(benchmarkCase.Job);
            string exePath = benchmarkTarget.Type.GetTypeInfo().Assembly.Location;
            
            var environmentVariables = new Dictionary<string, string> { ["MONO_VERBOSE_METHOD"] = fqnMethod };
            string monoPath = mono?.CustomPath ?? "mono";
            string arguments = $"--compile {fqnMethod} {llvmFlag} {exePath}";

            (int exitCode, IReadOnlyList<string> output) = ProcessHelper.RunAndReadOutputLineByLine(monoPath, arguments, environmentVariables: environmentVariables, includeErrors: true);
            string commandLine = $"{GetEnvironmentVariables(environmentVariables)} {monoPath} {arguments}";
            
            return OutputParser.Parse(output, benchmarkTarget.WorkloadMethod.Name, commandLine);
        }

        private static string GetEnvironmentVariables(Dictionary<string, string> environmentVariables) 
            => string.Join(" ", environmentVariables.Select(e => $"{e.Key}={e.Value}"));

        private static string GetMethodName(Descriptor descriptor)
            => $"{descriptor.Type.GetTypeInfo().Namespace}.{descriptor.Type.GetTypeInfo().Name}:{descriptor.WorkloadMethod.Name}";

        // TODO: use resolver
        // TODO: introduce a global helper method for LlvmFlag 
        private static string GetLlvmFlag(Job job) =>
            job.ResolveValue(EnvironmentMode.JitCharacteristic, Jit.Default) == Jit.Llvm ? "--llvm" : "--nollvm";

        internal static class OutputParser
        {
            internal static DisassemblyResult Parse([ItemCanBeNull] IReadOnlyList<string> input, string methodName, string commandLine)
            {
                var instructions = new List<Code>();

                const string windowsHeader = "Disassembly of section .text:";
                const string macOSXHeader = "(__TEXT,__text) section";
                const string windowsWarning = "is not recognized as an internal or external command";

                var warningLines = input
                    .Where(line => line != null)
                    .Where(line => line.Contains(windowsWarning))
                    .Select(line => line.Trim(' ', '.', ','))
                    .ToList();
                if (warningLines.Any())
                {
                    string message = "It's impossible to get Mono disasm because you don't have some required tools:"
                                     + Environment.NewLine
                                     + string.Join(Environment.NewLine, warningLines);
                    return CreateErrorResult(input, methodName, commandLine, message);
                }

                if (!input.Any(line => line != null && (line.Contains(windowsHeader) || line.Contains(macOSXHeader))))
                    return CreateErrorResult(input, methodName, commandLine, "It's impossible to find assembly instructions in the mono output");

                var listing = input
                    .Where(line => line != null)
                    .SkipWhile(line => !line.Contains(macOSXHeader) && !line.Contains(windowsHeader))
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Skip(2);

                foreach (string line in listing)
                    if (TryParseInstruction(line, out var instruction))
                        instructions.Add(instruction);

                while (instructions.Any() && instructions.Last().TextRepresentation == "nop")
                    instructions.RemoveAt(instructions.Count - 1);

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

            private static DisassemblyResult CreateErrorResult([ItemCanBeNull] IReadOnlyList<string> input, 
                string methodName, string commandLine, string message)
            {
                return new DisassemblyResult
                {
                    Methods = new[]
                    {
                        new DisassembledMethod
                        {
                            Name = methodName,
                            Maps = new[] { new Map
                            {
                                Instructions = input
                                    .Where(line => !string.IsNullOrWhiteSpace(line))
                                    .Select(line => new Code { TextRepresentation = line })
                                    .ToArray()
                            } },
                            CommandLine = commandLine
                        }
                    },
                    Errors = new[] { message }
                };
            }

            //line example 1:  0:	48 83 ec 28          	sub    $0x28,%rsp
            //line example 2: 0000000000000000	subq	$0x28, %rsp
            private static readonly Regex InstructionRegex = new Regex(@"\s*(?<address>[0-9a-f]+)(\:\s+([0-9a-f]{2}\s+)+)?\s+(?<instruction>.*)\s*");

            private static bool TryParseInstruction(string line, out Code instruction)
            {
                instruction = null;
                var match = InstructionRegex.Match(line);
                if (!match.Success)
                    return false;

                instruction = new Code { TextRepresentation = match.Groups["instruction"].ToString() };
                return true;
            }
        }
    }
}