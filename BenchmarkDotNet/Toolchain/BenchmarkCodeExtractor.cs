using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Logging;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Desktop;
using Microsoft.Diagnostics.Runtime.Interop;

namespace BenchmarkDotNet
{
    internal class BenchmarkCodeExtractor
    {
        private Process process { get; set; }
        private string codeExeName { get; set; }

        private string fullTypeName { get; set; }
        private string fullMethodName { get; set; }

        private IBenchmarkLogger logger { get; set; }

        public BenchmarkCodeExtractor(Benchmark benchmark, Process process, string codeExeName, IBenchmarkLogger logger)
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
        internal void PrintCodeForMethod(bool printAssembly, bool printIL)
        {
            logger?.WriteLine($"\nPrintAssembly={printAssembly}, PrintIL={printIL}");
            logger?.WriteLine($"Attaching to process {Path.GetFileName(process.MainModule.FileName)}, Pid={process.Id}");
            logger?.WriteLine($"Path {process.MainModule.FileName}");
            using (var dataTarget = DataTarget.AttachToProcess(process.Id, 5000, AttachFlag.NonInvasive))
            {
                var runtime = SetupClrRuntime(dataTarget);

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

                    var debugControl = dataTarget.DebuggerInterface as IDebugControl;
                    if (printAssembly)
                        PrintAssemblyCode(@method, ilMaps, debugControl);
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
            finally
            {
                Console.ResetColor();
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
                    logger?.WriteLine(BenchmarkLogKind.Help, "{0,6}:{1}", location.SourceLocation.LineNumber, lines[(int)location.SourceLocation.LineNumber - 1]);
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
            finally
            {
                Console.ResetColor();
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
                Console.Write(lineOfAssembly.ToString());
            } while (disassemblySize > 0 && endOffset <= endAddress);
            logger?.WriteLine();
        }
    }
}
