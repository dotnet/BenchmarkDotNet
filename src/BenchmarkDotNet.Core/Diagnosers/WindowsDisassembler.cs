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
using System.Linq;

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

        internal DisassemblyResult Dissasemble(DiagnoserActionParameters parameters)
        {
            var resultsPath = Path.GetTempFileName();

            var errors = ProcessHelper.RunAndReadOutput(
                GetDisassemblerPath(parameters.Process, parameters.Benchmark.Job.Env.Platform),
                BuildArguments(parameters, resultsPath));

            if (!string.IsNullOrEmpty(errors))
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
            var assemblyWithDisassemblersInResources = typeof(WindowsDisassembler).GetTypeInfo().Assembly;

            var disassemblerPath =
                Path.Combine(
                    new FileInfo(assemblyWithDisassemblersInResources.Location).Directory.FullName,
                    (Properties.BenchmarkDotNetInfo.FullVersion // possible update
                    + exeName)); // separate process per architecture!!

#if !PRERELEASE_DEVELOP // for development we always want to copy the file to not ommit any dev changes (Properties.BenchmarkDotNetInfo.FullVersion in file name is not enough)
            if (File.Exists(disassemblerPath))
                return disassemblerPath;
#endif
            // the disassembler has not been yet retrived from the resources
            CopyFromResources(
                assemblyWithDisassemblersInResources,
                $"BenchmarkDotNet.Disassemblers.net46.win7_{architectureName}.{exeName}",
                disassemblerPath);

            CopyAllRequiredDependencies(assemblyWithDisassemblersInResources, Path.GetDirectoryName(disassemblerPath));

            return disassemblerPath;
        }

        private void CopyAllRequiredDependencies(Assembly assemblyWithDisassemblersInResources, string destinationFolder)
        {
            // ClrMD and Cecil are also embeded in the resources, we need to copy them as well
            foreach (var dependency in assemblyWithDisassemblersInResources.GetManifestResourceNames().Where(name => name.EndsWith(".dll")))
            {
                // dependency is sth like "BenchmarkDotNet.Disassemblers.net46.win7_x64.Microsoft.Diagnostics.Runtime.dll"
                var fileName = dependency.Replace("BenchmarkDotNet.Disassemblers.net46.win7_x64.", string.Empty);
                var dllPath = Path.Combine(destinationFolder, fileName);

                if (!File.Exists(dllPath))
                    CopyFromResources(
                        assemblyWithDisassemblersInResources,
                        dependency,
                        dllPath);
            }
        }

        private void CopyFromResources(Assembly assembly, string resourceName, string destinationPath)
        {
            using (var resourceStream = assembly.GetManifestResourceStream(resourceName))
            using (var exeStream = File.Create(destinationPath))
            {
                resourceStream.CopyTo(exeStream);
            }
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

#if !NETCOREAPP1_1
                bool isWow64;
                if (!IsWow64Process(process.Handle, out isWow64))
                    throw new Exception("Not Windows");
                return !isWow64;
#else
                return System.IntPtr.Size == 8; // todo: find the way to cover all scenarios for .NET Core
#endif
            }

            [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool IsWow64Process([In] IntPtr process, [Out] out bool wow64Process);
        }
    }
}