using BenchmarkDotNet.Detectors.Cpu.Windows;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using System.Diagnostics;
using System.Runtime.Versioning;
using Windows.Win32.System.SystemInformation;

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

    // SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX header, then
    // PROCESSOR_RELATIONSHIP { BYTE Flags; BYTE EfficiencyClass; BYTE Reserved[20]; WORD GroupCount; GROUP_AFFINITY GroupMask[]; }
    private const int GroupMaskOffset = LogicalProcessorInformation.HeaderSize + 2 + 20 + sizeof(ushort);

    // The hardware does not change while the process is running, so it is probed only once.
    private static readonly Lazy<ulong?> CachedMask = new (DetectPerformanceCoresMask);

    /// <summary>
    /// Returns the affinity mask of the performance cores that the current process may run on,
    /// or <c>null</c> when it can't be detected, when all the cores are of the same type,
    /// or when none of the performance cores is available to the current process.
    /// </summary>
    internal static ulong? TryGetPerformanceCoresMask() => CachedMask.Value;

    /// <summary>
    /// Returns the affinity of the performance cores that can be applied to the current process,
    /// or <c>false</c> when <see cref="TryGetPerformanceCoresMask"/> returns <c>null</c>.
    /// </summary>
    internal static bool TryGetPerformanceCoresAffinity(out IntPtr affinity)
    {
        affinity = default;
        return TryGetPerformanceCoresMask() is ulong mask && ProcessExtensions.TryConvertAffinity(mask, IntPtr.Size, out affinity);
    }

    private static int notDetectedWarningWritten;

    /// <summary>
    /// Writes (once per process) that the performance cores affinity was requested but could not be applied.
    /// </summary>
    internal static void WarnNotDetectedOnce(ILogger logger)
    {
        if (Interlocked.Exchange(ref notDetectedWarningWritten, 1) == 0)
            logger.WriteLineWarning("// The performance cores affinity was requested, but performance cores could not be detected (the CPU has homogeneous cores, the OS is not supported or none of them is available to the process). No affinity is going to be set.");
    }

    private static ulong? DetectPerformanceCoresMask()
    {
        try
        {
            ulong? mask = null;
            if (OsDetector.IsWindows7OrLater() && !Portability.RuntimeInformation.IsMono)
                mask = BuildMask(GetWindowsCoreRanks());
            else if (OsDetector.IsLinux())
                mask = GetLinuxPerformanceCoresMask();

            return RestrictToAvailableProcessors(mask, GetCurrentProcessAffinity(), IntPtr.Size);
        }
        catch
        {
            // This is a best-effort probe, the caller treats any failure as "not detected".
            return null;
        }
    }

    /// <summary>
    /// Builds an affinity mask out of all the processors except the ones with the lowest rank (the efficiency cores),
    /// so CPUs with more than two core types (e.g. Arm prime + big + little) keep all their non-efficiency cores.
    /// Returns <c>null</c> when there are no processors or when all of them have the same rank (homogeneous CPU).
    /// </summary>
    internal static ulong? BuildMask(IEnumerable<(int Processor, int Rank)> processors)
    {
        var list = processors.Where(p => p.Processor >= 0 && p.Processor < MaxMaskProcessorCount).ToList();
        if (list.Count == 0)
            return null;

        int minRank = list.Min(p => p.Rank);
        if (list.All(p => p.Rank == minRank))
            return null;

        ulong mask = 0;
        foreach (var (processor, rank) in list)
            if (rank > minRank)
                mask |= 1UL << processor;

        return mask;
    }

    /// <summary>
    /// Keeps only the processors the current process can use: the ones of its affinity (cgroup cpuset, taskset, job object...)
    /// and, in a 32 bit process, the first 32 ones. Returns <c>null</c> when none of them is left.
    /// </summary>
    internal static ulong? RestrictToAvailableProcessors(ulong? mask, ulong? processAffinity, int pointerSize)
    {
        if (mask is not ulong result)
            return null;

        if (pointerSize < 8)
            result &= uint.MaxValue;
        if (processAffinity.HasValue)
            result &= processAffinity.Value;

        return result == 0 ? null : result;
    }

    private static ulong? GetCurrentProcessAffinity()
    {
        using var process = Process.GetCurrentProcess();
        if (process.TryGetAffinity() is not IntPtr affinity)
            return null;

        return IntPtr.Size >= 8 ? unchecked((ulong) affinity.ToInt64()) : unchecked((uint) affinity.ToInt32());
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
    /// Returns the logical processors together with the efficiency class of their core
    /// (a higher efficiency class means a more performant core).
    /// </summary>
    [SupportedOSPlatform("windows6.1")]
    private static IEnumerable<(int Processor, int Rank)> GetWindowsCoreRanks()
    {
        var buffer = LogicalProcessorInformation.Read(LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore);
        return buffer == null ? [] : ParseWindowsProcessorCores(buffer);
    }

    /// <summary>
    /// Parses the buffer returned by GetLogicalProcessorInformationEx(RelationProcessorCore).
    /// The efficiency class is reported per core, so all the logical processors of a core get the same rank.
    /// Before Windows 10 it's always 0, which is treated as a homogeneous CPU.
    /// Machines with more than one processor group are not supported (an empty list is returned):
    /// the affinity mask applies to the primary group of the benchmark process, which is not necessarily group 0.
    /// </summary>
    internal static unsafe List<(int Processor, int Rank)> ParseWindowsProcessorCores(byte[] buffer)
    {
        var result = new List<(int, int)>();
        bool multipleGroups = false;

        LogicalProcessorInformation.ForEachEntry(buffer, info =>
        {
            if (info->Relationship != LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore || info->Size < GroupMaskOffset)
                return;

            ref PROCESSOR_RELATIONSHIP core = ref info->Processor;
            if (info->Size < GroupMaskOffset + core.GroupCount * sizeof(GROUP_AFFINITY))
                return; // malformed entry

            var groups = (GROUP_AFFINITY*) ((byte*) info + GroupMaskOffset);
            for (int i = 0; i < core.GroupCount; i++)
            {
                if (groups[i].Group != 0)
                {
                    multipleGroups = true;
                    continue;
                }

                ulong mask = groups[i].Mask;
                for (int processor = 0; processor < MaxMaskProcessorCount; processor++)
                    if ((mask & (1UL << processor)) != 0)
                        result.Add((processor, core.EfficiencyClass));
            }
        });

        return multipleGroups ? [] : result;
    }
}
