using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Properties;
using JetBrains.Annotations;
using RuntimeInformation = BenchmarkDotNet.Portability.RuntimeInformation;

namespace BenchmarkDotNet.Disassemblers
{
    [PublicAPI]
    public class WindowsDisassembler
    {
        private readonly DisassemblyDiagnoserConfig config;

        [PublicAPI]
        public WindowsDisassembler(DisassemblyDiagnoserConfig config) => this.config = config;

        [PublicAPI]
        public DisassemblyResult Disassemble(DiagnoserActionParameters parameters)
        {
            string resultsPath = Path.GetTempFileName();

            string errors = ProcessHelper.RunAndReadOutput(
                GetDisassemblerPath(parameters.Process, parameters.BenchmarkCase.Job.Environment.Platform),
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

        private static string GetDisassemblerPath(Process process, Platform platform)
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

        private static string GetDisassemblerPath(string architectureName)
        {
            // one can only attach to a process of same target architecture, this is why we need exe for x64 and for x86
            string exeName = $"BenchmarkDotNet.Disassembler.{architectureName}.exe";
            var assemblyWithDisassemblersInResources = typeof(WindowsDisassembler).GetTypeInfo().Assembly;

            var dir = new FileInfo(assemblyWithDisassemblersInResources.Location).Directory ?? throw new DirectoryNotFoundException();
            string disassemblerPath = Path.Combine(
                    dir.FullName,
                    FolderNameHelper.ToFolderName(BenchmarkDotNetInfo.FullVersion), // possible update
                    exeName); // separate process per architecture!!

            Path.GetDirectoryName(disassemblerPath).CreateIfNotExists();

#if !PRERELEASE_DEVELOP // for development we always want to copy the file to not omit any dev changes (Properties.BenchmarkDotNetInfo.FullVersion in file name is not enough)
            if (File.Exists(disassemblerPath))
                return disassemblerPath;
#endif
            // the disassembler has not been yet retrieved from the resources
            CopyFromResources(
                assemblyWithDisassemblersInResources,
                $"BenchmarkDotNet.Disassemblers.net461.win7_{architectureName}.{exeName}",
                disassemblerPath);

            CopyAllRequiredDependencies(assemblyWithDisassemblersInResources, Path.GetDirectoryName(disassemblerPath));

            return disassemblerPath;
        }

        private static void CopyAllRequiredDependencies(Assembly assemblyWithDisassemblersInResources, string destinationFolder)
        {
            // ClrMD and Iced are also embedded in the resources, we need to copy them as well
            foreach (string dependency in assemblyWithDisassemblersInResources.GetManifestResourceNames().Where(name => name.EndsWith(".dll")))
            {
                // dependency is sth like "BenchmarkDotNet.Disassemblers.net461.win7_x64.Microsoft.Diagnostics.Runtime.dll"
                string fileName = dependency.Replace("BenchmarkDotNet.Disassemblers.net461.win7_x64.", string.Empty);
                string dllPath = Path.Combine(destinationFolder, fileName);

                if (!File.Exists(dllPath))
                    CopyFromResources(
                        assemblyWithDisassemblersInResources,
                        dependency,
                        dllPath);
            }
        }

        private static void CopyFromResources(Assembly assembly, string resourceName, string destinationPath)
        {
            using (var resourceStream = assembly.GetManifestResourceStream(resourceName))
            using (var exeStream = File.Create(destinationPath))
            {
                if (resourceStream == null)
                    throw new InvalidOperationException($"{nameof(resourceName)} is null");
                resourceStream.CopyTo(exeStream);
            }
        }

        // if the benchmark requires jitting we use disassembler entry method, if not we use benchmark method name
        private string BuildArguments(DiagnoserActionParameters parameters, string resultsPath)
            => new StringBuilder(200)
                .Append(parameters.Process.Id).Append(' ')
                .Append("BenchmarkDotNet.Autogenerated.Runnable_").Append(parameters.BenchmarkId.Value).Append(' ')
                .Append(DisassemblerConstants.DisassemblerEntryMethodName).Append(' ')
                .Append(config.PrintSource).Append(' ')
                .Append(config.MaxDepth).Append(' ')
                .Append($"\"{resultsPath}\"")
                .ToString();

        // code copied from https://stackoverflow.com/a/33206186/5852046
        private static class NativeMethods
        {
            // see https://msdn.microsoft.com/en-us/library/windows/desktop/ms684139%28v=vs.85%29.aspx
            public static bool Is64Bit(Process process)
            {
                if (Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") == "x86")
                    return false;

                if (RuntimeInformation.IsWindows())
                {
                    IsWow64Process(process.Handle, out bool isWow64);

                    return !isWow64;
                }

                return RuntimeInformation.Is64BitPlatform(); // todo: find the way to cover all scenarios for .NET Core
            }

            [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool IsWow64Process([In] IntPtr process, [Out] out bool wow64Process);
        }
    }
}