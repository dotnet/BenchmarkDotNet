using BenchmarkDotNet.Extensions;
using Microsoft.Win32;
using Perfolizer.Horology;
using Perfolizer.Models;
using System.Runtime.Versioning;
using Windows.Win32.System.SystemInformation;

namespace BenchmarkDotNet.Detectors.Cpu.Windows;

internal class DefaultCpuDetector : ICpuDetector
{
    [SupportedOSPlatform("windows")]
    public bool IsApplicable() => OsDetector.IsWindows7OrLater() && !Portability.RuntimeInformation.IsMono;

    [SupportedOSPlatform("windows6.1")]
    public CpuInfo? Detect()
    {
        if (!IsApplicable())
            return null;

        try
        {
            return ProcessorInfo.GetCpuInfo();
        }
        catch
        {
            return null;
        }
    }

    [SupportedOSPlatform("windows6.1")]
    internal static class ProcessorInfo
    {
        public static CpuInfo GetCpuInfo()
        {
            var processorModelNames = new HashSet<string>();
            double maxFrequency = 0;
            double nominalFrequency = 0;

            // Gets CPU information from registry.
            GetProcessorInfoFromRegistry(processorModelNames, ref maxFrequency, ref nominalFrequency);

            // Gets other CPU information by using Win32 API.
            var data = GetLogicalProcessorInformation() ?? new CpuData();

            string? processorName = processorModelNames.Count > 0
                ? string.Join(", ", processorModelNames)
                : null;
            Frequency? maxFrequencyActual = maxFrequency > 0
                ? Frequency.FromMHz(maxFrequency)
                : null;
            Frequency? nominalFrequencyActual = nominalFrequency > 0
                ? Frequency.FromMHz(nominalFrequency)
                : null;

            return new CpuInfo()
            {
                ProcessorName = processorName,
                NominalFrequencyHz = nominalFrequencyActual?.Hertz.RoundToLong(),
                MaxFrequencyHz = maxFrequencyActual?.Hertz.RoundToLong(),
                PhysicalProcessorCount = data.ProcessorPackageCount.ToNullableInteger(),
                PhysicalCoreCount = data.PhysicalProcessorCoreCount.ToNullableInteger(),
                LogicalCoreCount = data.LogicalCoreCount.ToNullableInteger(),
            };
        }

        private static void GetProcessorInfoFromRegistry(HashSet<string> names, ref double maxFrequency, ref double nominalFrequency)
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor");

            if (key == null)
                return;

            // HKLM\HARDWARE\DESCRIPTION\System\CentralProcessor\0..{N}
            foreach (string subKeyName in key.GetSubKeyNames())
            {
                using var processorKey = key.OpenSubKey(subKeyName);
                if (processorKey == null)
                    continue;

                if (processorKey.GetValue("ProcessorNameString") is string processorName)
                {
                    names.Add(processorName.Trim());
                }

                // Update CPU frequency information
                if (processorKey.GetValue("~MHz") is int mhz && mhz > 0)
                {
                    maxFrequency = Math.Max(maxFrequency, mhz);
                    nominalFrequency = nominalFrequency == 0
                        ? mhz
                        : Math.Min(nominalFrequency, mhz);
                }
            }
        }

        private static CpuData? GetLogicalProcessorInformation()
        {
            var buffer = LogicalProcessorInformation.Read(LOGICAL_PROCESSOR_RELATIONSHIP.RelationAll);
            return buffer == null ? null : ParseData(buffer);
        }

        private static unsafe CpuData ParseData(byte[] buffer)
        {
            // GROUP_RELATIONSHIP { WORD MaximumGroupCount; WORD ActiveGroupCount; BYTE Reserved[20]; PROCESSOR_GROUP_INFO GroupInfo[]; }
            const int groupInfoOffset = LogicalProcessorInformation.HeaderSize + 2 * sizeof(ushort) + 20;

            int processorPackageCount = 0;
            int physicalProcessorCoreCount = 0;
            int activeProcessorCount = 0;

            LogicalProcessorInformation.ForEachEntry(buffer, info =>
            {
                switch (info->Relationship)
                {
                    case LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorPackage:
                        ++processorPackageCount;
                        break;

                    case LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore:
                        ++physicalProcessorCoreCount;
                        break;

                    case LOGICAL_PROCESSOR_RELATIONSHIP.RelationGroup:
                        ref GROUP_RELATIONSHIP group = ref info->Group;
                        if (info->Size < groupInfoOffset + group.ActiveGroupCount * sizeof(PROCESSOR_GROUP_INFO))
                            break; // malformed entry

                        for (int i = 0; i < group.ActiveGroupCount; i++)
                        {
                            activeProcessorCount += group.GroupInfo[i].ActiveProcessorCount;
                        }
                        break;

                    // Other types are skipped.
                    default:
                        break;
                }
            });

            return new CpuData
            {
                ProcessorPackageCount = processorPackageCount,
                PhysicalProcessorCoreCount = physicalProcessorCoreCount,
                LogicalCoreCount = activeProcessorCount,
            };
        }

        private readonly record struct CpuData
        {
            public int ProcessorPackageCount { get; init; }

            public int PhysicalProcessorCoreCount { get; init; }

            public int LogicalCoreCount { get; init; }
        }
    }
}

file static class ExtensionMethods
{
    public static int? ToNullableInteger(this int value)
    {
        return value == 0 ? null : value;
    }
}
