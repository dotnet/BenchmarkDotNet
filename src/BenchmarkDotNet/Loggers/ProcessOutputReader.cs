using System;
using System.Collections.Immutable;
using System.Diagnostics;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Toolchains.Parameters;

namespace BenchmarkDotNet.Loggers
{
    internal class ProcessOutputReader
    {
        private readonly ILogger logger;
        private readonly Process benchmarkProcess;
        private readonly IDiagnoser diagnoser;
        private readonly DiagnoserActionParameters diagnoserActionParameters;
        private readonly ImmutableArray<string>.Builder linesWithResults;
        private readonly ImmutableArray<string>.Builder linesWithExtraOutput;

        public ProcessOutputReader(Process process, ExecuteParameters executeParameters)
        {
            if (!process.StartInfo.RedirectStandardOutput)
                throw new NotSupportedException("set RedirectStandardOutput to true first");
            if (!process.StartInfo.RedirectStandardInput)
                throw new NotSupportedException("set RedirectStandardInput to true first");

            benchmarkProcess = process;
            logger = executeParameters.Logger;
            diagnoser = executeParameters.Diagnoser;
            diagnoserActionParameters = new DiagnoserActionParameters(process, executeParameters);
            linesWithResults = ImmutableArray.CreateBuilder<string>();
            linesWithExtraOutput = ImmutableArray.CreateBuilder<string>();
        }

        internal ImmutableArray<string> GetLinesWithResults() => linesWithResults.ToImmutable();

        internal ImmutableArray<string> GetLinesWithExtraOutput() => linesWithExtraOutput.ToImmutable();

        internal void ReadToEnd()
        {
            // Peek -1 or 0 can indicate internal error.
            while (!benchmarkProcess.StandardOutput.EndOfStream && benchmarkProcess.StandardOutput.Peek() > 0)
            {
                // ReadLine() can actually return string.Empty and null as valid values.
                string line = benchmarkProcess.StandardOutput.ReadLine();

                if (line == null)
                    continue;
                
                logger.WriteLine(LogKind.Default, line);

                if (!line.StartsWith("//"))
                {
                    linesWithResults.Add(line);
                }
                else if (Engine.Signals.TryGetSignal(line, out var signal))
                {
                    diagnoser?.Handle(signal, diagnoserActionParameters);
                    benchmarkProcess.StandardInput.WriteLine(Engine.Signals.Acknowledgment);
                }
                else if (!string.IsNullOrEmpty(line))
                {
                    linesWithExtraOutput.Add(line);

                    if (line.Contains("BadImageFormatException"))
                        logger.WriteLineError("You are probably missing <PlatformTarget>AnyCPU</PlatformTarget> in your .csproj file.");
                }
            }
        }
    }
}