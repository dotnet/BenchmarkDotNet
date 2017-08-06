using BenchmarkDotNet.Diagnosers;
using System;
using System.Collections.Generic;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using BenchmarkDotNet.Helpers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public class DisassemblyDiagnoser : IDisassemblyDiagnoser
    {
        public static readonly IDiagnoser Asm = new DisassemblyDiagnoser(printAsm: true);
        public static readonly IDiagnoser AsmFullRecursive = new DisassemblyDiagnoser(printAsm: true, printPrologAndEpilog: true, recursiveDepth: int.MaxValue);
        public static readonly IDiagnoser IL = new DisassemblyDiagnoser(printAsm: false, printIL: true);
        public static readonly IDiagnoser AsmAndIL = new DisassemblyDiagnoser(printAsm: true, printIL: true);
        public static readonly IDiagnoser All = new DisassemblyDiagnoser(true, true, true, true, int.MaxValue);

        private readonly bool printAsm = true, printIL = false, printSource = false, printPrologAndEpilog = false;
        private readonly int recursiveDepth = 1;

        private readonly Dictionary<Benchmark, DisassemblyResult> results = new Dictionary<Benchmark, DisassemblyResult>();

        // ReSharper disable once EmptyConstructor parameterless ctor is mandatory for DiagnosersLoader.CreateDiagnoser
        public DisassemblyDiagnoser() { }

        /// <param name="printAsm">ASM will be printed. True by default.</param>
        /// <param name="printIL">IL will be printed. False by default.</param>
        /// <param name="printSource">C# source code will be printed. False by default.</param>
        /// <param name="printPrologAndEpilog">ASM for prolog and epilog will be printed. False by default.</param>
        /// <param name="recursiveDepth">Includes called methods to given level. 1 by default, indexed from 1. To print just benchmark set to 0</param>
        public DisassemblyDiagnoser(bool printAsm = true, bool printIL = false, bool printSource = false, bool printPrologAndEpilog = false, int recursiveDepth = 1)
        {
            this.printIL = printIL;
            this.printAsm = printAsm;
            this.printSource = printSource;
            this.printPrologAndEpilog = printPrologAndEpilog;
            this.recursiveDepth = recursiveDepth;
        }

        public IReadOnlyDictionary<Benchmark, DisassemblyResult> Results => results;

        public IEnumerable<string> Ids => new[] { nameof(DisassemblyDiagnoser) };

        public IEnumerable<IExporter> Exporters => new[] { new DisassemblyExporter(results) };

        public void BeforeGlobalCleanup(DiagnoserActionParameters parameters) { }
        public void BeforeAnythingElse(DiagnoserActionParameters parameters) { }
        public void BeforeMainRun(DiagnoserActionParameters parameters) { }
        public void ProcessResults(Benchmark benchmark, BenchmarkReport report) { }
        public IColumnProvider GetColumnProvider() => EmptyColumnProvider.Instance;

        public void DisplayResults(ILogger logger)
            => logger.WriteInfo("The results were exported to \".\\BenchmarkDotNet.Artifacts\\results\\*-disassembly-report.html\"");

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            foreach (var benchmark in validationParameters.Benchmarks)
            {
                if (benchmark.Job.Env.HasValue(EnvMode.RuntimeCharacteristic) && benchmark.Job.Env.Runtime is MonoRuntime)
                    yield return new ValidationError(true, "Mono is not supported by the DisassemblyDiagnoser yet.", benchmark);

                if (benchmark.Job.Infrastructure.HasValue(InfrastructureMode.ToolchainCharacteristic)
                    && benchmark.Job.Infrastructure.Toolchain is InProcessToolchain)
                {
                    yield return new ValidationError(true, "InProcessToolchain has no DisassemblyDiagnoser support", benchmark);
                }
            }
        }

        public void AfterGlobalSetup(DiagnoserActionParameters parameters) // the benchmark is already compiled
        {
            var resultsPath = Path.GetTempFileName();

            var errors = ProcessHelper.RunAndReadOutput(
                GetDisassemblerPath(parameters.Process, parameters.Benchmark.Job.Env.Platform),
                BuildArguments(parameters, resultsPath));

            if(!string.IsNullOrEmpty(errors))
                parameters.Config.GetCompositeLogger().WriteError(errors);

            using (var stream = new FileStream(resultsPath, FileMode.Open, FileAccess.Read))
            using (var reader = XmlReader.Create(stream))
            {
                var serializer = new XmlSerializer(typeof(DisassemblyResult));

                results.Add(
                    parameters.Benchmark,
                    (DisassemblyResult)serializer.Deserialize(reader));
            }

            File.Delete(resultsPath);
        }

        internal string GetOutput() => results.Single().Value.ToString();

        private string GetDisassemblerPath(Process process, Platform platform)
        {
            switch (platform)
            {
                case Platform.AnyCpu:
                    return GetDisassemblerPath(process,
                        NativeMethods.Is64Bit(process)
                            ? Platform.X64
                            : Platform.X86);
                case Platform.X86:
                    return GetDisassemblerPath("x86");
                case Platform.X64:
                    return GetDisassemblerPath("x64");
                default:
                    throw new NotSupportedException($"Platform {platform} not supported!");
            }
        }

        private string GetDisassemblerPath(string architectureName)
        {
            // one can only attach to a process of same target architecture, this is why we need exe for x64 and for x86
            var exeName = $"BenchmarkDotNet.Disassembler.{architectureName}.exe";
            var diagnosersAssembly = typeof(DisassemblyDiagnoser).Assembly;

            var disassemblerPath =
                Path.Combine(
                    new FileInfo(diagnosersAssembly.Location).Directory.FullName, // all required dependencies are there
                    (Properties.BenchmarkDotNetInfo.FullVersion // possible update
                    + exeName)); // separate process per architecture!!

#if !PRERELEASE_DEVELOP // for development we always want to copy the file to not ommit any dev changes (Properties.BenchmarkDotNetInfo.FullVersion in file name is not enough)
            if (File.Exists(disassemblerPath))
                return disassemblerPath;
#endif

            // the disassembler has not been yet retrived from the resources
            using (var resourceStream = diagnosersAssembly.GetManifestResourceStream($"BenchmarkDotNet.Diagnostics.Windows.Disassemblers.net46.win7_{architectureName}.{exeName}"))
            using (var exeStream = File.Create(disassemblerPath))
            {
                resourceStream.CopyTo(exeStream);
            }

            return disassemblerPath;
        }

        // must be kept in sync with BenchmarkDotNet.Disassembler.Program.Main
        private string BuildArguments(DiagnoserActionParameters parameters, string resultsPath)
            => $"{parameters.Process.Id} \"{parameters.Benchmark.Target.Type.FullName}\" \"{parameters.Benchmark.Target.Method.Name}\""
             + $" {printAsm} {printIL} {printSource} {printPrologAndEpilog}"
             + $" {recursiveDepth}"
             + $" \"{resultsPath}\"";

        // code copied from https://stackoverflow.com/a/33206186/5852046
        internal static class NativeMethods
        {
            // see https://msdn.microsoft.com/en-us/library/windows/desktop/ms684139%28v=vs.85%29.aspx
            public static bool Is64Bit(Process process)
            {
                if (Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") == "x86")
                    return false;

                bool isWow64;
                if (!IsWow64Process(process.Handle, out isWow64))
                    throw new Exception("Not Windows");
                return !isWow64;
            }

            [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool IsWow64Process([In] IntPtr process, [Out] out bool wow64Process);
        }
    }
}