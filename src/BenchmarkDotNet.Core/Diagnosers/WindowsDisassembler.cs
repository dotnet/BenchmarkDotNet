using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Serialization;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Diagnosers
{
    internal class WindowsDisassembler
    {
        private readonly bool printAsm, printIL, printSource, printPrologAndEpilog;
        private readonly int recursiveDepth;

        internal WindowsDisassembler(DisassemblyDiagnoserConfig config)
        {
            printIL = config.PrintIL;
            printAsm = config.PrintAsm;
            printSource = config.PrintSource;
            printPrologAndEpilog = config.PrintPrologAndEpilog;
            recursiveDepth = config.RecursiveDepth;
        }

        internal DisassemblyResult Dissasemble(DiagnoserActionParameters parameters, Assembly windowsDiagnosticsAssembly) // the benchmark is already compiled
        {
            var resultsPath = Path.GetTempFileName();

            var errors = ProcessHelper.RunAndReadOutput(
                GetDisassemblerPath(parameters.Process, parameters.Benchmark.Job.Env.Platform, windowsDiagnosticsAssembly),
                BuildArguments(parameters, resultsPath));

            if(!string.IsNullOrEmpty(errors))
                parameters.Config.GetCompositeLogger().WriteError(errors);

            try
            {
                using (var stream = new FileStream(resultsPath, FileMode.Open, FileAccess.Read))
                using (var reader = XmlReader.Create(stream))
                {
                    var serializer = new XmlSerializer(typeof(DisassemblyResult));

                    return (DisassemblyResult)serializer.Deserialize(reader);
                }
            }
            finally
            {
                File.Delete(resultsPath);
            }
        }

        private string GetDisassemblerPath(Process process, Platform platform, Assembly windowsDiagnosticsAssembly)
        {
            switch (platform)
            {
                case Platform.AnyCpu:
                    return GetDisassemblerPath(process,
                        NativeMethods.Is64Bit(process)
                            ? Platform.X64
                            : Platform.X86,
                        windowsDiagnosticsAssembly);
                case Platform.X86:
                    return GetDisassemblerPath("x86", windowsDiagnosticsAssembly);
                case Platform.X64:
                    return GetDisassemblerPath("x64", windowsDiagnosticsAssembly);
                default:
                    throw new NotSupportedException($"Platform {platform} not supported!");
            }
        }

        private string GetDisassemblerPath(string architectureName, Assembly windowsDiagnosticsAssembly)
        {
            // one can only attach to a process of same target architecture, this is why we need exe for x64 and for x86
            var exeName = $"BenchmarkDotNet.Disassembler.{architectureName}.exe";

            var disassemblerPath =
                Path.Combine(
                    new FileInfo(windowsDiagnosticsAssembly.Location).Directory.FullName, // all required dependencies are there
                    (Properties.BenchmarkDotNetInfo.FullVersion // possible update
                    + exeName)); // separate process per architecture!!

#if !PRERELEASE_DEVELOP // for development we always want to copy the file to not ommit any dev changes (Properties.BenchmarkDotNetInfo.FullVersion in file name is not enough)
            if (File.Exists(disassemblerPath))
                return disassemblerPath;
#endif

            // the disassembler has not been yet retrived from the resources
            using (var resourceStream = windowsDiagnosticsAssembly.GetManifestResourceStream($"BenchmarkDotNet.Diagnostics.Windows.Disassemblers.net46.win7_{architectureName}.{exeName}"))
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

#if !CORE
                bool isWow64;
                if (!IsWow64Process(process.Handle, out isWow64))
                    throw new Exception("Not Windows");
                return !isWow64;
#else
                throw new NotSupportedException();
#endif
            }

            [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool IsWow64Process([In] IntPtr process, [Out] out bool wow64Process);
        }
    }
}