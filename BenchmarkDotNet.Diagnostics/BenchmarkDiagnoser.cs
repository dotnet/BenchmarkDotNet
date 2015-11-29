using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using BenchmarkDotNet.Plugins.Diagnosers;
using BenchmarkDotNet.Plugins.Loggers;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Desktop;
using Microsoft.Diagnostics.Runtime.Interop;

namespace BenchmarkDotNet.Diagnostics
{
    public class BenchmarkDiagnoser : IBenchmarkDiagnoser
    {
        private Process process { get; set; }
        private string codeExeName { get; set; }

        private string fullTypeName { get; set; }
        private string fullMethodName { get; set; }

        private Dictionary<ulong, Tuple<string, string>> MethodAddressLookup { get; set; }

        private IBenchmarkLogger logger { get; set; }

        public BenchmarkDiagnoser(Benchmark benchmark, Process process, string codeExeName, IBenchmarkLogger logger)
        {
            this.process = process;
            this.codeExeName = codeExeName;
            this.logger = logger;

            //Method name format: "BenchmarkDotNet.Samples.Infra.RunFast()" (NOTE: WITHOUT the return type)
            var methodInfo = benchmark.Target.Method;
            fullTypeName = methodInfo.DeclaringType.FullName;

            var methodParams = string.Join(", ", methodInfo.GetParameters().Select(p => p.ParameterType.FullName));
            fullMethodName = $"{fullTypeName}.{methodInfo.Name}({methodParams})";
        }

        /// <summary>
        /// Code from http://stackoverflow.com/questions/2057781/is-there-a-way-to-get-the-stacktraces-for-all-threads-in-c-like-java-lang-thre/24315960#24315960
        /// also see http://stackoverflow.com/questions/31633541/clrmd-throws-exception-when-creating-runtime/31745689#31745689
        /// </summary>
        public void PrintCodeForMethod(bool printAssembly, bool printIL, bool printDiagnostics)
        {
            logger?.WriteLine($"\nPrintAssembly={printAssembly}, PrintIL={printIL}");
            logger?.WriteLine($"Attaching to process {Path.GetFileName(process.MainModule.FileName)}, Pid={process.Id}");
            logger?.WriteLine($"Path {process.MainModule.FileName}");
            using (var dataTarget = DataTarget.AttachToProcess(process.Id, 5000, AttachFlag.NonInvasive))
            {
                var runtime = SetupClrRuntime(dataTarget);
                if (printDiagnostics)
                    PrintRuntimeDiagnosticInfo(dataTarget, runtime);

                if (printAssembly == false && printIL == false)
                    return;

                ClrType @class = runtime.GetHeap().GetTypeByName(fullTypeName);
                ClrMethod @method = @class.Methods.Single(m => m.GetFullSignature() == fullMethodName);
                DesktopModule module = (DesktopModule)@method.Type.Module;
                if (!module.IsPdbLoaded)
                {
                    string pdbLocation = module.TryDownloadPdb(null);
                    if (pdbLocation != null)
                        module.LoadPdb(pdbLocation);
                }

                logger?.WriteLine($"Module: {Path.GetFileName(module.Name)}");
                logger?.WriteLine($"Type: {method.Type.Name}");
                logger?.WriteLine($"Method: {method.Name}");

                if (printAssembly)
                    MethodAddressLookup = CreateMethodAddressLookup(runtime);

                // TODO work out why this returns locations inside OTHER methods, it's like it doesn't have an upper bound and just keeps going!?
                var ilOffsetLocations = module.GetSourceLocationsForMethod(@method.MetadataToken);

                string filePath = null;
                string[] lines = null;
                logger?.WriteLine("");
                for (int i = 0; i < ilOffsetLocations.Count; i++)
                {
                    var location = ilOffsetLocations[i];
                    var ilMaps = @method.ILOffsetMap.Where(il => il.ILOffset == location.ILOffset).ToList();
                    if (ilMaps.Any() == false)
                        continue;

                    if (lines == null || location.SourceLocation.FilePath != filePath)
                    {
                        filePath = location.SourceLocation.FilePath;
                        lines = File.ReadAllLines(filePath);
                        logger?.WriteLine($"Parsing file {Path.GetFileName(location.SourceLocation.FilePath)}");
                    }

                    PrintLocationAndILMapInfo(@method, location, ilMaps);
                    PrintSourceCode(lines, location);

                    if (printAssembly)
                    {
                        var debugControl = dataTarget.DebuggerInterface as IDebugControl;
                        PrintAssemblyCode(@method, ilMaps, debugControl);
                    }
                }
            }
        }

        private ClrRuntime SetupClrRuntime(DataTarget dataTarget)
        {
            var version = dataTarget.ClrVersions.Single();
            logger?.WriteLine($"\nCLR Version: {version.Version} ({version.Flavor}), Dac: {version.DacInfo}");
            var dacFileName = version.TryDownloadDac();
            logger?.WriteLine($"DacFile: {Path.GetFileName(dacFileName)}");
            logger?.WriteLine($"DacPath: {Path.GetDirectoryName(dacFileName)}");
            ClrRuntime runtime = dataTarget.CreateRuntime(dacFileName);
            return runtime;
        }

        private void PrintLocationAndILMapInfo(ClrMethod method, ILOffsetSourceLocation location, IList<ILToNativeMap> ilMaps)
        {
            try
            {
                var ilMapsInfo = ilMaps.Select(ilMap =>
                                        string.Format("IL_{0:X4} [{1:X8}-{2:X8} ({3:X8}-{4:X8})] ",
                                            ilMap.ILOffset,
                                            ilMap.StartAddress,
                                            ilMap.EndAddress,
                                            ilMap.StartAddress - @method.NativeCode,
                                            ilMap.EndAddress - @method.NativeCode));
                logger?.WriteLine(BenchmarkLogKind.Statistic, string.Join("\n  ", ilMapsInfo));
            }
            catch (Exception ex)
            {
                logger?.WriteLine(BenchmarkLogKind.Error, ex.ToString());
            }
        }

        private void PrintSourceCode(IList<string> lines, ILOffsetSourceLocation location)
        {
            try
            {
                const int indent = 7;
                var lineToPrint = location.SourceLocation.LineNumber - 1;
                if (lineToPrint >= 0 && lineToPrint < lines.Count)
                {
                    logger?.WriteLine(BenchmarkLogKind.Help, "{0,6}:{1}", location.SourceLocation.LineNumber, lines[location.SourceLocation.LineNumber - 1]);
                    logger?.WriteLine(BenchmarkLogKind.Info,
                                      new string(' ', location.SourceLocation.ColStart - 1 + indent) +
                                      new string('*', location.SourceLocation.ColEnd - location.SourceLocation.ColStart));
                }
                else
                {
                    logger?.WriteLine("Unable to show line {0} (0x{0:X8}), there are only {1} lines", lineToPrint, lines.Count);
                }
            }
            catch (Exception ex)
            {
                logger?.WriteLine(BenchmarkLogKind.Error, ex.ToString());
            }
        }

        /// <summary>
        /// See https://github.com/goldshtn/msos/commit/705d3758d15835d2520b31fcf3028353bdbca73b#commitcomment-12499813
        /// and https://github.com/Microsoft/dotnetsamples/blob/master/Microsoft.Diagnostics.Runtime/CLRMD/ClrMemDiag/Debugger/IDebugControl.cs#L126-L156
        /// </summary>
        private void PrintAssemblyCode(ClrMethod method, IList<ILToNativeMap> ilMaps, IDebugControl debugControl)
        {
            // This is the first instruction of the JIT'ed (or NGEN'ed) machine code.
            ulong startAddress = ilMaps.Select(entry => entry.StartAddress).Min();

            // Unfortunately there's not a great way to get the size of the code, or the end address.
            // You are supposed to do code flow analysis like "uf" in windbg to find the size, but
            // in practice you can use the IL to native mapping:
            ulong endAddress = ilMaps.Select(entry => entry.EndAddress).Max();

            var bufferSize = 500; // per-line
            var lineOfAssembly = new StringBuilder(bufferSize);
            ulong startOffset = startAddress, endOffset;
            uint disassemblySize;
            do
            {
                // result always seems to be = 0?!
                var flags = DEBUG_DISASM.EFFECTIVE_ADDRESS; // DEBUG_DISASM.SOURCE_FILE_NAME | DEBUG_DISASM.SOURCE_LINE_NUMBER;
                var result = debugControl.Disassemble(startOffset, flags, lineOfAssembly, bufferSize, out disassemblySize, out endOffset);
                startOffset = endOffset;
                logger?.Write(lineOfAssembly.ToString());

                if (lineOfAssembly.ToString().Contains(" call ") == false)
                    continue;

                var methodCallInfo = GetCalledMethodFromAssemblyOutput(lineOfAssembly.ToString());
                if (string.IsNullOrWhiteSpace(methodCallInfo) == false)
                    logger?.WriteLine(BenchmarkLogKind.Info, $"  *** {methodCallInfo} ***");
            } while (disassemblySize > 0 && endOffset <= endAddress);
            logger?.WriteLine();
        }

        private string GetCalledMethodFromAssemblyOutput(string assemblyString)
        {
            string hexAddressText = "";
            var callLocation = assemblyString.LastIndexOf("call");
            var qwordPtrLocation = assemblyString.IndexOf("qword ptr");
            var openBracket = assemblyString.IndexOf('(');
            var closeBracket = assemblyString.IndexOf(')');

            if (openBracket != -1 && closeBracket != -1 && closeBracket > openBracket)
            {
                // 000007fe`979171fb e800261b5e      call    mscorlib_ni+0x499800 (000007fe`f5ac9800)
                hexAddressText = assemblyString.Substring(openBracket, closeBracket - openBracket)
                                               .Replace("(", "")
                                               .Replace(")", "")
                                               .Replace("`", "");
            }
            else if (callLocation != -1 && qwordPtrLocation != -1)
            {
                // TODO have to also deal with:
                // 000007fe`979f666f 41ff13          call    qword ptr [r11] ds:000007fe`978e0050=000007fe978ed260
            }
            else if (callLocation != -1)
            {
                // 000007fe`979e6721 e8e2b5ffff      call    000007fe`979e1d08
                var endOfCallLocation = callLocation + 4;
                hexAddressText = assemblyString.Substring(endOfCallLocation, assemblyString.Length - endOfCallLocation)
                                               .Replace("`", "")
                                               .Trim(' ');
            }

            ulong actualAddress;
            if (ulong.TryParse(hexAddressText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out actualAddress))
            {
                if (MethodAddressLookup.ContainsKey(actualAddress))
                    return string.Format("Call: {0:X16} -> {1}", actualAddress, MethodAddressLookup[actualAddress].Item2);
                else
                    return string.Format("Call: {0:X16} -> No matching method found", actualAddress);
            }

            return $"{hexAddressText} -> Not a valid hex value";
        }

        private Dictionary<ulong, Tuple<string, string>> CreateMethodAddressLookup(ClrRuntime runtime)
        {
            var methodLookup = new Dictionary<ulong, Tuple<string, string>>();
            long dupes = 0, overriddenMethods = 0, skips = 0;
            logger?.WriteLine("\nScanning loaded Modules for Method Definitions:");
            foreach (var module in runtime.EnumerateModules())
            {
                logger?.WriteLine($"  {Path.GetFileName(module.FileName)}");
                foreach (var type in module.EnumerateTypes().Where(t => t.IsPublic))
                {
                    foreach (var method in type.Methods.Where(m => m.IsPublic && //!m.IsAbstract &&
                                                                   m.NativeCode != ulong.MaxValue))
                    {
                        var cleanedUpMethodName = method.ToString().Replace("ClrMethod signature=", "")
                                                                   .Replace("<'", "")
                                                                   .Replace("' />", "");
                        if (cleanedUpMethodName.StartsWith(type.Name) == false)
                        {
                            overriddenMethods++;
                            continue;
                        }

                        var methodAddress = method.NativeCode;
                        if (methodLookup.ContainsKey(methodAddress))
                        {
                            dupes++;
                        }
                        else
                        {
                            methodLookup.Add(methodAddress, Tuple.Create(type.Name, cleanedUpMethodName));
                        }
                    }
                }
            }

            logger?.WriteLine("After analysis there are {0:N0} items in the lookup, {1:N0} overridden Methods, {2:N0} skips and {3:N0} dupes",
                              methodLookup.Count, overriddenMethods, skips, dupes);
            return methodLookup;
        }
        private void PrintRuntimeDiagnosticInfo(DataTarget dataTarget, ClrRuntime runtime)
        {
            logger?.WriteLine(BenchmarkLogKind.Header, "\nRuntime Diagnostic Information");
            logger?.WriteLine(BenchmarkLogKind.Header, "------------------------------");

            logger?.WriteLine(BenchmarkLogKind.Header, "\nDataTarget Info:");
            logger?.WriteLine(BenchmarkLogKind.Info, "  ClrVersion{0}: {1}", dataTarget.ClrVersions.Count > 1 ? "s" : "", string.Join(", ", dataTarget.ClrVersions));
            logger?.WriteLine(BenchmarkLogKind.Info, "  Architecture: " + dataTarget.Architecture);
            logger?.WriteLine(BenchmarkLogKind.Info, "  PointerSize: {0} ({1}-bit)", dataTarget.PointerSize, dataTarget.PointerSize == 8 ? 64 : 32);
            logger?.WriteLine(BenchmarkLogKind.Info, "  SymbolPath: " + dataTarget.GetSymbolPath());

            logger?.WriteLine(BenchmarkLogKind.Header, "\nClrRuntime Info:");
            logger?.WriteLine(BenchmarkLogKind.Info, "  ServerGC: " + runtime.ServerGC);
            logger?.WriteLine(BenchmarkLogKind.Info, "  HeapCount: " + runtime.HeapCount);
            logger?.WriteLine(BenchmarkLogKind.Info, "  Thread Count: " + runtime.Threads.Count);

            logger?.WriteLine(BenchmarkLogKind.Header, "\nClrRuntime Modules:");
            foreach (var module in runtime.EnumerateModules())
            {
                logger?.WriteLine(BenchmarkLogKind.Info,
                                  "  {0,36} Id:{1} - {2,10:N0} bytes @ 0x{3:X16}",
                                  Path.GetFileName(module.FileName),
                                  module.AssemblyId.ToString().PadRight(10),
                                  module.Size,
                                  module.ImageBase);
            }

            ClrHeap heap = runtime.GetHeap();
            logger?.WriteLine(BenchmarkLogKind.Header, "\nClrHeap Info:");
            logger?.WriteLine(BenchmarkLogKind.Info, "  TotalHeapSize: {0:N0} bytes ({1:N2} MB)", heap.TotalHeapSize, heap.TotalHeapSize / 1024.0 / 1024.0);
            logger?.WriteLine(BenchmarkLogKind.Info, "  Gen0: {0,10:N0} bytes", heap.GetSizeByGen(0));
            logger?.WriteLine(BenchmarkLogKind.Info, "  Gen1: {0,10:N0} bytes", heap.GetSizeByGen(1));
            logger?.WriteLine(BenchmarkLogKind.Info, "  Gen2: {0,10:N0} bytes", heap.GetSizeByGen(2));
            logger?.WriteLine(BenchmarkLogKind.Info, "   LOH: {0,10:N0} bytes", heap.GetSizeByGen(3));

            logger?.WriteLine(BenchmarkLogKind.Info, "  Segments: " + heap.Segments.Count);
            foreach (var segment in heap.Segments)
            {
                logger?.WriteLine(BenchmarkLogKind.Info,
                                  "    Segment: {0,10:N0} bytes, {1,10}, Gen0: {2,10:N0} bytes, Gen1: {3,10:N0} bytes, Gen2: {4,10:N0} bytes",
                                  segment.Length,
                                  segment.IsLarge ? "Large" : (segment.IsEphemeral ? "Ephemeral" : "Unknown"),
                                  segment.Gen0Length,
                                  segment.Gen1Length,
                                  segment.Gen2Length);
            }

            logger?.WriteLine();
        }
    }
}
