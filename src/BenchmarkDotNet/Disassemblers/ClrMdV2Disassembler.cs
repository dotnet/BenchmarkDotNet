using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Filters;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Disassemblers
{
    // This Disassembler uses ClrMd v2x. Please keep it in sync with ClrMdV1Disassembler (if possible).
    internal abstract class ClrMdV2Disassembler
    {
        private static readonly ulong MinValidAddress = GetMinValidAddress();

        private static ulong GetMinValidAddress()
        {
            // https://github.com/dotnet/BenchmarkDotNet/pull/2413#issuecomment-1688100117
            if (RuntimeInformation.IsWindows())
                return ushort.MaxValue + 1;
            if (RuntimeInformation.IsLinux())
                return (ulong) Environment.SystemPageSize;
            if (RuntimeInformation.IsMacOS())
                return RuntimeInformation.GetCurrentPlatform() switch
                {
                    Environments.Platform.X86 or Environments.Platform.X64 => 4096,
                    Environments.Platform.Arm64 => 0x100000000,
                    _ => throw new NotSupportedException($"{RuntimeInformation.GetCurrentPlatform()} is not supported")
                };
            throw new NotSupportedException($"{System.Runtime.InteropServices.RuntimeInformation.OSDescription} is not supported");
        }

        private static bool IsValidAddress(ulong address)
            // -1 (ulong.MaxValue) address is invalid, and will crash the runtime in older runtimes. https://github.com/dotnet/runtime/pull/90794
            // 0 is NULL and therefore never valid.
            // Addresses less than the minimum virtual address are also invalid.
            => address != ulong.MaxValue
                && address != 0
                && address >= MinValidAddress;

        internal DisassemblyResult AttachAndDisassemble(Settings settings)
        {
            using (var dataTarget = DataTarget.AttachToProcess(
                settings.ProcessId,
                suspend: false))
            {
                var runtime = dataTarget.ClrVersions.Single().CreateRuntime();

                ConfigureSymbols(dataTarget);

                var state = new State(runtime, settings.TargetFrameworkMoniker);

                if (settings.Filters.Length > 0)
                {
                    FilterAndEnqueue(state, settings);
                }
                else
                {
                    ClrType typeWithBenchmark = state.Runtime.EnumerateModules().Select(module => module.GetTypeByName(settings.TypeName)).First(type => type != null);

                    state.Todo.Enqueue(
                        new MethodInfo(
                            // the Disassembler Entry Method is always parameterless, so check by name is enough
                            typeWithBenchmark.Methods.Single(method => method.IsPublic && method.Name == settings.MethodName),
                            0));
                }

                var disassembledMethods = Disassemble(settings, state);

                // we don't want to export the disassembler entry point method which is just an artificial method added to get generic types working
                var filteredMethods = disassembledMethods.Length == 1
                    ? disassembledMethods // if there is only one method we want to return it (most probably benchmark got inlined)
                    : disassembledMethods.Where(method => !method.Name.Contains(DisassemblerConstants.DisassemblerEntryMethodName)).ToArray();

                return new DisassemblyResult
                {
                    Methods = filteredMethods,
                    SerializedAddressToNameMapping = state.AddressToNameMapping.Select(x => new DisassemblyResult.MutablePair { Key = x.Key, Value = x.Value }).ToArray(),
                    PointerSize = (uint)IntPtr.Size
                };
            }
        }

        private static void ConfigureSymbols(DataTarget dataTarget)
        {
            // code copied from https://github.com/Microsoft/clrmd/issues/34#issuecomment-161926535
            dataTarget.SetSymbolPath("http://msdl.microsoft.com/download/symbols");
        }

        private static void FilterAndEnqueue(State state, Settings settings)
        {
            Regex[] filters = GlobFilter.ToRegex(settings.Filters);

            foreach (ClrModule module in state.Runtime.EnumerateModules())
                foreach (ClrType type in module.EnumerateTypeDefToMethodTableMap().Select(map => state.Runtime.GetTypeByMethodTable(map.MethodTable)).Where(type => type is not null))
                    foreach (ClrMethod method in type.Methods.Where(method => method.Signature != null))
                    {
                        if (method.NativeCode > 0)
                        {
                            if (!state.AddressToNameMapping.TryGetValue(method.NativeCode, out _))
                            {
                                state.AddressToNameMapping.Add(method.NativeCode, method.Signature);
                            }
                        }

                        if (CanBeDisassembled(method))
                        {
                            foreach (Regex filter in filters)
                            {
                                if (filter.IsMatch(method.Signature))
                                {
                                    state.Todo.Enqueue(new MethodInfo(method,
                                        depth: settings.MaxDepth)); // don't allow for recursive disassembling
                                    break;
                                }
                            }
                        }
                    }
        }

        private DisassembledMethod[] Disassemble(Settings settings, State state)
        {
            var result = new List<DisassembledMethod>();
            DisassemblySyntax syntax = (DisassemblySyntax)Enum.Parse(typeof(DisassemblySyntax), settings.Syntax);

            while (state.Todo.Count != 0)
            {
                var methodInfo = state.Todo.Dequeue();

                if (!state.HandledMethods.Add(methodInfo.Method)) // add it now to avoid StackOverflow for recursive methods
                    continue; // already handled

                if (settings.MaxDepth >= methodInfo.Depth)
                    result.Add(DisassembleMethod(methodInfo, state, settings, syntax));
            }

            return result.ToArray();
        }

        private static bool CanBeDisassembled(ClrMethod method) => method.ILOffsetMap.Length > 0 && method.NativeCode > 0;

        private DisassembledMethod DisassembleMethod(MethodInfo methodInfo, State state, Settings settings, DisassemblySyntax syntax)
        {
            var method = methodInfo.Method;

            if (!CanBeDisassembled(method))
            {
                if (method.IsPInvoke)
                    return CreateEmpty(method, "PInvoke method");
                if (method.IL is null || method.IL.Length == 0)
                    return CreateEmpty(method, "Extern method");
                if (method.CompilationType == MethodCompilationType.None)
                    return CreateEmpty(method, "Method was not JITted yet.");

                return CreateEmpty(method, $"No valid {nameof(method.ILOffsetMap)} and {nameof(method.HotColdInfo)}");
            }

            var codes = new List<SourceCode>();
            if (settings.PrintSource && method.ILOffsetMap.Length > 0)
            {
                // we use HashSet to prevent from duplicates
                var uniqueSourceCodeLines = new HashSet<Sharp>(new SharpComparer());
                // for getting C# code we always use the original ILOffsetMap
                foreach (var map in method.ILOffsetMap.Where(map => map.StartAddress < map.EndAddress && map.ILOffset >= 0).OrderBy(map => map.StartAddress))
                    foreach (var sharp in SourceCodeProvider.GetSource(method, map))
                        uniqueSourceCodeLines.Add(sharp);

                codes.AddRange(uniqueSourceCodeLines);
            }

            foreach (var map in GetCompleteNativeMap(method, state.Runtime))
            {
                codes.AddRange(Decode(map, state, methodInfo.Depth, method, syntax));
            }

            Map[] maps = settings.PrintSource
                ? codes.GroupBy(code => code.InstructionPointer).OrderBy(group => group.Key).Select(group => new Map() { SourceCodes = group.ToArray() }).ToArray()
                : new[] { new Map() { SourceCodes = codes.ToArray() } };

            return new DisassembledMethod
            {
                Maps = maps,
                Name = method.Signature,
                NativeCode = method.NativeCode
            };
        }

        private IEnumerable<Asm> Decode(ILToNativeMap map, State state, int depth, ClrMethod currentMethod, DisassemblySyntax syntax)
        {
            ulong startAddress = map.StartAddress;
            uint size = (uint)(map.EndAddress - map.StartAddress);

            byte[] code = new byte[size];

            int totalBytesRead = 0;
            do
            {
                int bytesRead = state.Runtime.DataTarget.DataReader.Read(startAddress + (ulong)totalBytesRead, new Span<byte>(code, totalBytesRead, (int)size - totalBytesRead));
                if (bytesRead <= 0)
                {
                    throw new EndOfStreamException($"Tried to read {size} bytes for {currentMethod.Signature}, got only {totalBytesRead}");
                }
                totalBytesRead += bytesRead;
            } while (totalBytesRead != size);

            return Decode(code, startAddress, state, depth, currentMethod, syntax);
        }

        protected abstract IEnumerable<Asm> Decode(byte[] code, ulong startAddress, State state, int depth, ClrMethod currentMethod, DisassemblySyntax syntax);

        private static ILToNativeMap[] GetCompleteNativeMap(ClrMethod method, ClrRuntime runtime)
        {
            if (!TryReadNativeCodeAddresses(runtime, method, out ulong startAddress, out ulong endAddress))
            {
                startAddress = method.NativeCode;
                endAddress = ulong.MaxValue;
            }

            ILToNativeMap[] sortedMaps = method.ILOffsetMap // CanBeDisassembled ensures that there is at least one map in ILOffsetMap
                .Where(map => map.StartAddress >= startAddress && map.StartAddress < endAddress) // can be false for Tier 0 maps, EndAddress is not checked on purpose here
                .Where(map => map.StartAddress < map.EndAddress) // some maps have 0 length (they don't have corresponding assembly code?)
                .OrderBy(map => map.StartAddress) // we need to print in the machine code order, not IL! #536
                .Select(map => new ILToNativeMap()
                {
                    StartAddress = map.StartAddress,
                    // some maps have EndAddress > codeHeaderData.MethodStart + codeHeaderData.MethodSize and contain garbage (#2074). They need to be fixed!
                    EndAddress = Math.Min(map.EndAddress, endAddress),
                    ILOffset = map.ILOffset
                })
                .ToArray();

            if (sortedMaps.Length == 0)
            {
                // In such situation ILOffsetMap most likely describes Tier 0, while CodeHeaderData Tier 1.
                // Since we care about Tier 1 (if it's present), we "fake" a Tier 1 map.
                return new[] { new ILToNativeMap() { StartAddress = startAddress, EndAddress = endAddress } };
            }
            else if (sortedMaps[0].StartAddress != startAddress || (sortedMaps[sortedMaps.Length - 1].EndAddress != endAddress && endAddress != ulong.MaxValue))
            {
                // In such situation ILOffsetMap most likely is missing few bytes. We just "extend" it to avoid producing "bad" instructions.
                return new[] { new ILToNativeMap() { StartAddress = startAddress, EndAddress = endAddress } };
            }

            return sortedMaps;
        }

        private static DisassembledMethod CreateEmpty(ClrMethod method, string reason)
            => DisassembledMethod.Empty(method.Signature, method.NativeCode, reason);

        protected static bool TryReadNativeCodeAddresses(ClrRuntime runtime, ClrMethod method, out ulong startAddress, out ulong endAddress)
        {
            if (method is not null
                && runtime.DacLibrary.SOSDacInterface.GetCodeHeaderData(method.NativeCode, out var codeHeaderData) == HResult.S_OK
                && codeHeaderData.MethodSize > 0) // false for extern methods!
            {
                // HotSize can be missing or be invalid (https://github.com/microsoft/clrmd/issues/1036).
                // So we fetch the method size on our own.
                startAddress = codeHeaderData.MethodStart;
                endAddress = codeHeaderData.MethodStart + codeHeaderData.MethodSize;
                return true;
            }

            startAddress = endAddress = 0;
            return false;
        }

        protected void TryTranslateAddressToName(ulong address, bool isAddressPrecodeMD, State state, int depth, ClrMethod currentMethod)
        {
            if (!IsValidAddress(address) || state.AddressToNameMapping.ContainsKey(address))
                return;

            var runtime = state.Runtime;

            var jitHelperFunctionName = runtime.GetJitHelperFunctionName(address);
            if (!string.IsNullOrEmpty(jitHelperFunctionName))
            {
                state.AddressToNameMapping.Add(address, jitHelperFunctionName);
                return;
            }

            var method = runtime.GetMethodByInstructionPointer(address);
            if (method is null && (address & ((uint) runtime.DataTarget.DataReader.PointerSize - 1)) == 0)
            {
                if (runtime.DataTarget.DataReader.ReadPointer(address, out ulong newAddress) && IsValidAddress(newAddress))
                {
                    method = runtime.GetMethodByInstructionPointer(newAddress);

                    method = WorkaroundGetMethodByInstructionPointerBug(runtime, method, newAddress);
                }
            }
            else
            {
                method = WorkaroundGetMethodByInstructionPointerBug(runtime, method, address);
            }

            if (method is null)
            {
                var methodDescriptor = runtime.GetMethodByHandle(address);
                if (methodDescriptor is not null)
                {
                    if (isAddressPrecodeMD)
                    {
                        state.AddressToNameMapping.Add(address, $"Precode of {methodDescriptor.Signature}");
                    }
                    else
                    {
                        state.AddressToNameMapping.Add(address, $"MD_{methodDescriptor.Signature}");
                    }
                    return;
                }

                var methodTableName = runtime.DacLibrary.SOSDacInterface.GetMethodTableName(address);
                if (!string.IsNullOrEmpty(methodTableName))
                {
                    state.AddressToNameMapping.Add(address, $"MT_{methodTableName}");
                }
                return;
            }

            if (method.NativeCode == currentMethod.NativeCode && method.Signature == currentMethod.Signature)
                return; // in case of a call which is just a jump within the method or a recursive call

            if (!state.HandledMethods.Contains(method))
                state.Todo.Enqueue(new MethodInfo(method, depth + 1));

            var methodName = method.Signature;
            if (!methodName.Any(c => c == '.')) // the method name does not contain namespace and type name
                methodName = $"{method.Type.Name}.{method.Signature}";
            state.AddressToNameMapping.Add(address, methodName);
        }

        protected void FlushCachedDataIfNeeded(IDataReader dataTargetDataReader, ulong address, byte[] buffer)
        {
            if (!RuntimeInformation.IsWindows())
            {
                if (dataTargetDataReader.Read(address, buffer) <= 0)
                {
                    // We don't suspend the benchmark process for the time of disassembling,
                    // as it would require sudo privileges.
                    // Because of that, the Tiered JIT thread might still re-compile some methods
                    // in the meantime when the host process it trying to disassemble the code.
                    // In such case, Tiered JIT thread might create stubs which requires flushing of the cached data.
                    dataTargetDataReader.FlushCachedData();
                }
            }
        }

        // GetMethodByInstructionPointer sometimes returns wrong methods.
        // In case given address does not belong to the methods range, null is returned.
        private static ClrMethod WorkaroundGetMethodByInstructionPointerBug(ClrRuntime runtime, ClrMethod method, ulong newAddress)
            => TryReadNativeCodeAddresses(runtime, method, out ulong startAddress, out ulong endAddress) && !(startAddress >= newAddress && newAddress <= endAddress)
            ? null
            : method;

        private class SharpComparer : IEqualityComparer<Sharp>
        {
            public bool Equals(Sharp x, Sharp y)
            {
                // sometimes some C# code lines are duplicated because the same line is the best match for multiple ILToNativeMaps
                // we don't want to confuse the users, so this must also be removed
                return x.FilePath == y.FilePath && x.LineNumber == y.LineNumber;
            }

            public int GetHashCode(Sharp obj) => HashCode.Combine(obj.FilePath, obj.LineNumber);
        }
    }
}