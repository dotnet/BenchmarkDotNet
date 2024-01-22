using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Utilities;
using Microsoft.Diagnostics.Runtime.Utilities.Pdb;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace BenchmarkDotNet.Disassemblers
{
    // This is taken from the Samples\FileAndLineNumbers projects from microsoft/clrmd,
    // and replaces the previously-available SourceLocation functionality.

    internal class SourceLocation
    {
        public string FilePath;
        public int LineNumber;
        public int LineNumberEnd;
        public int ColStart;
        public int ColEnd;
    }

    internal class SourceCodeProvider : IDisposable
    {
        private readonly Dictionary<string, string[]> sourceFileCache = new Dictionary<string, string[]>();
        private readonly Dictionary<PdbInfo, PdbReader> pdbReaders = new Dictionary<PdbInfo, PdbReader>();

        public void Dispose()
        {
            foreach (var reader in pdbReaders.Values)
            {
                reader?.Dispose();
            }
        }

        internal IEnumerable<Sharp> GetSource(ClrMethod method, ILToNativeMap map)
        {
            var sourceLocation = GetSourceLocation(method, map.ILOffset);
            if (sourceLocation == null)
                yield break;

            for (int line = sourceLocation.LineNumber; line <= sourceLocation.LineNumberEnd; ++line)
            {
                var sourceLine = ReadSourceLine(sourceLocation.FilePath, line);
                if (sourceLine == null)
                    continue;

                var text = sourceLine + Environment.NewLine
                    + GetSmartPointer(sourceLine,
                        start: line == sourceLocation.LineNumber ? sourceLocation.ColStart - 1 : default(int?),
                        end: line == sourceLocation.LineNumberEnd ? sourceLocation.ColEnd - 1 : default(int?));

                yield return new Sharp
                {
                    Text = text,
                    InstructionPointer = map.StartAddress,
                    FilePath = sourceLocation.FilePath,
                    LineNumber = line
                };
            }
        }

        private string ReadSourceLine(string file, int line)
        {
            if (!sourceFileCache.TryGetValue(file, out string[] contents))
            {
                // sometimes the symbols report some disk location from MS CI machine like "E:\A\_work\308\s\src\mscorlib\shared\System\Random.cs" for .NET Core 2.0
                if (!File.Exists(file))
                    return null;

                contents = File.ReadAllLines(file);
                sourceFileCache.Add(file, contents);
            }

            return line - 1 < contents.Length
                ? contents[line - 1]
                : null; // "nop" can have no corresponding c# code ;)
        }

        private static string GetSmartPointer(string sourceLine, int? start, int? end)
        {
            Debug.Assert(start is null || start < sourceLine.Length);
            Debug.Assert(end is null || end <= sourceLine.Length);

            var prefix = new char[end ?? sourceLine.Length];
            var index = 0;

            // write offset using whitespaces
            while (index < (start ?? prefix.Length))
            {
                prefix[index] =
                    sourceLine.Length > index &&
                    sourceLine[index] == '\t'
                    ? '\t'
                    : ' ';
                index++;
            }

            // write smart pointer
            while (index < prefix.Length)
            {
                prefix[index] = '^';
                index++;
            }

            return new string(prefix);
        }

        internal SourceLocation GetSourceLocation(ClrMethod method, int ilOffset)
        {
            PdbReader reader = GetReaderForMethod(method);
            if (reader == null)
                return null;

            PdbFunction function = reader.GetFunctionFromToken(method.MetadataToken);
            return FindNearestLine(function, ilOffset);
        }

        internal SourceLocation GetSourceLocation(ClrStackFrame frame)
        {
            PdbReader reader = GetReaderForMethod(frame.Method);
            if (reader == null)
                return null;

            PdbFunction function = reader.GetFunctionFromToken(frame.Method.MetadataToken);
            int ilOffset = FindIlOffset(frame);

            return FindNearestLine(function, ilOffset);
        }

        private static SourceLocation FindNearestLine(PdbFunction function, int ilOffset)
        {
            if (function == null || function.SequencePoints == null)
                return null;

            int distance = int.MaxValue;
            SourceLocation? nearest = null;

            foreach (PdbSequencePointCollection sequenceCollection in function.SequencePoints)
            {
                foreach (PdbSequencePoint point in sequenceCollection.Lines)
                {
                    int dist = (int)Math.Abs(point.Offset - ilOffset);
                    if (dist < distance)
                    {
                        if (nearest == null)
                            nearest = new SourceLocation();

                        nearest.FilePath = sequenceCollection.File.Name;
                        nearest.LineNumber = (int)point.LineBegin;
                        nearest.LineNumberEnd = (int)point.LineEnd;
                        nearest.ColStart = (int)point.ColBegin;
                        nearest.ColEnd = (int)point.ColEnd;

                        distance = dist;
                    }
                }
            }

            return nearest;
        }

        private static int FindIlOffset(ClrStackFrame frame)
        {
            ulong ip = frame.InstructionPointer;
            int last = -1;
            foreach (ILToNativeMap item in frame.Method.ILOffsetMap)
            {
                if (item.StartAddress > ip)
                    return last;

                if (ip <= item.EndAddress)
                    return item.ILOffset;

                last = item.ILOffset;
            }

            return last;
        }

        private PdbReader GetReaderForMethod(ClrMethod method)
        {
            ClrModule module = method?.Type?.Module;
            PdbInfo info = module?.Pdb;

            PdbReader? reader = null;
            if (info != null)
            {
                if (!pdbReaders.TryGetValue(info, out reader))
                {
                    SymbolLocator locator = GetSymbolLocator(module);
                    string pdbPath = locator.FindPdb(info);
                    if (pdbPath != null)
                    {
                        try
                        {
                            reader = new PdbReader(pdbPath);
                        }
                        catch (IOException)
                        {
                            // This will typically happen when trying to load information
                            // from public symbols, or symbol files generated by some weird
                            // compiler. We can ignore this, but there's no need to load
                            // this PDB anymore, so we will put null in the dictionary and
                            // be done with it.
                            reader = null;
                        }
                    }

                    pdbReaders[info] = reader;
                }
            }

            return reader;
        }

        private static SymbolLocator GetSymbolLocator(ClrModule module)
        {
            return module.Runtime.DataTarget.SymbolLocator;
        }
    }
}
