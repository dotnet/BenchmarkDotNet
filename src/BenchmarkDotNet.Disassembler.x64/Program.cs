using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace BenchmarkDotNet.Disassemblers
{
    internal static class Program
    {
        // the goals of the existence of this process:
        // 1. attach to benchmarked process
        // 2. disassemble the code
        // 3. save it to xml file
        // 4. detach & shut down
        //
        // requirements: must not have any dependencies to BenchmarkDotNet itself, KISS
        public static void Main(string[] args)
        {
            var options = Settings.FromArgs(args);

            if (Process.GetProcessById(options.ProcessId).HasExited) // possible when benchmark has already finished
                throw new Exception($"The process {options.ProcessId} has already exited"); // if we don't throw here the Clrmd will fail with some mysterious HRESULT: 0xd000010a ;)

            try
            {
                var methodsToExport = ClrMdV1Disassembler.AttachAndDisassemble(options);

                SaveToFile(methodsToExport, options.ResultsPath);
            }
            catch (OutOfMemoryException) // thrown by clrmd when pdb is missing or in invalid format
            {
                Console.WriteLine("\\ ---------------------------");
                Console.WriteLine("Failed to read source code location!");
                Console.WriteLine("Please make sure that the project, which defines benchmarks contains following settings:");
                Console.WriteLine("\t <DebugType>pdbonly</DebugType>");
                Console.WriteLine("\t <DebugSymbols>true</DebugSymbols>");
                Console.WriteLine("\\ ---------------------------");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to disassemble with following exception:");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private static void SaveToFile(DisassemblyResult disassemblyResult, string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write))
            using (var writer = XmlWriter.Create(stream))
            {
                var serializer = new XmlSerializer(typeof(DisassemblyResult));

                serializer.Serialize(writer, disassemblyResult);
            }
        }
    }
}
