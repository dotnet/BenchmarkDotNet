using System.ComponentModel;
using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Detectors.Cpu;

/// <summary>
/// Detects the performance cores (P-cores) of CPUs with heterogeneous core configurations
/// (e.g. Intel hybrid architectures or Arm big.LITTLE) and builds an affinity mask that contains only them.
/// Windows and Linux only.
/// </summary>
internal static class PerformanceCoresDetector
{
    // An affinity mask can describe at most 64 processors (Windows processor group 0).
    private const int MaxMaskProcessorCount = 64;

    /// <summary>
    /// Returns the affinity mask of the performance cores,
    /// or <c>null</c> when it can't be detected or when all the cores are of the same type.
    /// </summary>
    internal static ulong? TryGetPerformanceCoresMask()
    {
        try
        {
            if (OsDetector.IsWindows())
                return BuildMask(GetWindowsCoreRanks());
            if (OsDetector.IsLinux())
                return GetLinuxPerformanceCoresMask();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Win32Exception or EntryPointNotFoundException or DllNotFoundException)
        {
            // the information is not available, the caller treats it as "not detected"
        }

        return null;
    }

    /// <summary>
    /// Builds an affinity mask out of the processors with the highest rank.
    /// Returns <c>null</c> when there are no processors or when all of them have the same rank (homogeneous CPU).
    /// </summary>
    internal static ulong? BuildMask(IEnumerable<(int Processor, int Rank)> processors)
    {
        var list = processors.Where(p => p.Processor >= 0 && p.Processor < MaxMaskProcessorCount).ToList();
        if (list.Count == 0)
            return null;

        int maxRank = list.Max(p => p.Rank);
        if (list.All(p => p.Rank == maxRank))
            return null;

        ulong mask = 0;
        foreach (var (processor, rank) in list)
            if (rank == maxRank)
                mask |= 1UL << processor;

        return mask;
    }

    /// <summary>
    /// Parses the Linux cpu list format, e.g. "0-3,8,10-11".
    /// </summary>
    internal static IReadOnlyList<int> ParseCpuList(string? cpuList)
    {
        var result = new List<int>();
        if (string.IsNullOrWhiteSpace(cpuList))
            return result;

        foreach (string part in cpuList!.Trim().Split([','], StringSplitOptions.RemoveEmptyEntries))
        {
            string[] range = part.Trim().Split('-');
            if (range.Length == 1 && int.TryParse(range[0], out int single))
            {
                result.Add(single);
            }
            else if (range.Length == 2 && int.TryParse(range[0], out int start) && int.TryParse(range[1], out int end) && start <= end)
            {
                for (int cpu = start; cpu <= end; cpu++)
                    result.Add(cpu);
            }
            else
            {
                return []; // unexpected format, don't guess
            }
        }

        return result;
    }

    private static ulong? GetLinuxPerformanceCoresMask()
    {
        // Intel hybrid CPUs expose the P-cores and E-cores as separate PMUs.
        const string intelCoreCpus = "/sys/devices/cpu_core/cpus";
        const string intelAtomCpus = "/sys/devices/cpu_atom/cpus";
        if (File.Exists(intelCoreCpus) && File.Exists(intelAtomCpus))
        {
            var performance = ParseCpuList(File.ReadAllText(intelCoreCpus));
            var efficiency = ParseCpuList(File.ReadAllText(intelAtomCpus));
            var mask = BuildMask(performance.Select(cpu => (cpu, 1)).Concat(efficiency.Select(cpu => (cpu, 0))));
            if (mask.HasValue)
                return mask;
        }

        // Arm (big.LITTLE/DynamIQ) and some x86 systems expose the relative capacity of each cpu.
        const string cpuRoot = "/sys/devices/system/cpu";
        if (!Directory.Exists(cpuRoot))
            return null;

        var capacities = new List<(int, int)>();
        foreach (string directory in Directory.GetDirectories(cpuRoot, "cpu*"))
        {
            string name = Path.GetFileName(directory);
            if (!int.TryParse(name.Substring("cpu".Length), out int cpu))
                continue;

            string capacityFile = Path.Combine(directory, "cpu_capacity");
            if (File.Exists(capacityFile) && int.TryParse(File.ReadAllText(capacityFile).Trim(), out int capacity))
                capacities.Add((cpu, capacity));
        }

        return BuildMask(capacities);
    }

    /// <summary>
    /// Returns the logical processors of processor group 0 together with their efficiency class
    /// (a higher efficiency class means a more performant core).
    /// </summary>
    private static IEnumerable<(int Processor, int Rank)> GetWindowsCoreRanks()
    {
        GetSystemCpuSetInformation(IntPtr.Zero, 0, out uint requiredLength, IntPtr.Zero, 0);
        if (requiredLength == 0)
            return [];

        var buffer = new byte[requiredLength];
        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            if (!GetSystemCpuSetInformation(handle.AddrOfPinnedObject(), requiredLength, out requiredLength, IntPtr.Zero, 0))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        finally
        {
            handle.Free();
        }

        return ParseWindowsCpuSets(buffer, (int) requiredLength);
    }

    /// <summary>
    /// Parses the buffer returned by GetSystemCpuSetInformation (an array of variable sized SYSTEM_CPU_SET_INFORMATION entries).
    /// </summary>
    internal static List<(int Processor, int Rank)> ParseWindowsCpuSets(byte[] buffer, int length)
    {
        // SYSTEM_CPU_SET_INFORMATION layout:
        // DWORD Size; CPU_SET_INFORMATION_TYPE Type; struct { DWORD Id; WORD Group; BYTE LogicalProcessorIndex;
        // BYTE CoreIndex; BYTE LastLevelCacheIndex; BYTE NumaNodeIndex; BYTE EfficiencyClass; ... } CpuSet;
        const int typeOffset = 4, groupOffset = 12, logicalProcessorIndexOffset = 14, efficiencyClassOffset = 18;
        const int cpuSetInformation = 0; // CpuSetInformation

        var result = new List<(int, int)>();
        int offset = 0;
        while (offset + efficiencyClassOffset < length)
        {
            int size = BitConverter.ToInt32(buffer, offset);
            if (size <= 0 || offset + size > length)
                break;

            if (BitConverter.ToInt32(buffer, offset + typeOffset) == cpuSetInformation
                && BitConverter.ToUInt16(buffer, offset + groupOffset) == 0)
            {
                result.Add((buffer[offset + logicalProcessorIndexOffset], buffer[offset + efficiencyClassOffset]));
            }

            offset += size;
        }

        return result;
    }

    [DllImport("kernel32", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemCpuSetInformation(IntPtr information, uint bufferLength, out uint returnedLength, IntPtr process, uint flags);
}
