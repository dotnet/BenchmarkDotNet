using BenchmarkDotNet.ConsoleArguments;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Foundation;
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

    /// <summary>
    /// Returns the affinity mask of the performance cores,
    /// or <c>null</c> when it can't be detected or when all the cores are of the same type.
    /// </summary>
    internal static ulong? TryGetPerformanceCoresMask()
    {
        try
        {
            if (OsDetector.IsWindows7OrLater())
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
    /// Returns the affinity of the performance cores that can be applied to the current process,
    /// or <c>false</c> when it can't be detected, when all the cores are of the same type,
    /// or when the mask does not fit into the current (32 bit) process.
    /// </summary>
    internal static bool TryGetPerformanceCoresAffinity(out IntPtr affinity)
    {
        affinity = default;
        return TryGetPerformanceCoresMask() is ulong mask && ConfigParser.TryConvertAffinity(mask, IntPtr.Size, out affinity);
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
    /// Returns the logical processors of processor group 0 together with the efficiency class of their core
    /// (a higher efficiency class means a more performant core).
    /// </summary>
    [SupportedOSPlatform("windows6.1")]
    private static unsafe IEnumerable<(int Processor, int Rank)> GetWindowsCoreRanks()
    {
        uint bufferLength = 0;
        PInvoke.GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore, null, ref bufferLength);
        var error = (WIN32_ERROR) Marshal.GetLastWin32Error();
        if (bufferLength == 0 || error != WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER)
            return [];

        var buffer = new byte[bufferLength];
        fixed (byte* bufferPtr = buffer)
        {
            if (!PInvoke.GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore, (SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX*) bufferPtr, &bufferLength))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return ParseWindowsProcessorCores(buffer, (int) bufferLength);
    }

    /// <summary>
    /// Parses the buffer returned by GetLogicalProcessorInformationEx(RelationProcessorCore)
    /// (an array of variable sized SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX entries).
    /// The efficiency class is reported per core, so all the logical processors of a core get the same rank.
    /// Before Windows 10 it's always 0, which is treated as a homogeneous CPU.
    /// </summary>
    internal static unsafe List<(int Processor, int Rank)> ParseWindowsProcessorCores(byte[] buffer, int length)
    {
        var result = new List<(int, int)>();
        if (length <= 0 || length > buffer.Length)
            return result;

        fixed (byte* bufferPtr = buffer)
        {
            byte* currentPtr = bufferPtr;
            byte* endPtr = bufferPtr + length;
            while (currentPtr < endPtr)
            {
                var info = (SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX*) currentPtr;
                uint size = info->Size;
                if (size == 0 || size > endPtr - currentPtr)
                    break;

                if (info->Relationship == LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore)
                {
                    ref PROCESSOR_RELATIONSHIP core = ref info->Processor;
                    for (int i = 0; i < core.GroupCount; i++)
                    {
                        GROUP_AFFINITY groupAffinity = core.GroupMask[i];
                        if (groupAffinity.Group != 0)
                            continue;

                        ulong mask = groupAffinity.Mask;
                        for (int processor = 0; processor < MaxMaskProcessorCount; processor++)
                            if ((mask & (1UL << processor)) != 0)
                                result.Add((processor, core.EfficiencyClass));
                    }
                }

                currentPtr += size;
            }
        }

        return result;
    }
}
