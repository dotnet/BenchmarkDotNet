using BenchmarkDotNet.Extensions;
using Microsoft.Win32;
using Perfolizer.Horology;
using Perfolizer.Models;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Foundation;
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

        private static unsafe CpuData? GetLogicalProcessorInformation()
        {
            // Get required buffer length.
            uint bufferLength = 0;
            PInvoke.GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP.RelationAll, null, ref bufferLength);

            var error = (WIN32_ERROR)Marshal.GetLastWin32Error();
            if (bufferLength == 0 || error != WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER)
                return null;

            var buffer = new byte[bufferLength];
            fixed (byte* bufferPtr = buffer)
            {
                var isSuccess = PInvoke.GetLogicalProcessorInformationEx(
                    LOGICAL_PROCESSOR_RELATIONSHIP.RelationAll,
                    (SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX*)bufferPtr,
                    &bufferLength
                );

                if (!isSuccess)
                    return null;

                return ParseData(bufferPtr, bufferLength);
            }
        }

        private static unsafe CpuData ParseData(byte* bufferPtr, uint actualLength)
        {
            byte* currentPtr = bufferPtr;
            byte* endPtr = currentPtr + actualLength;

            int processorPackageCount = 0;
            int physicalProcessorCoreCount = 0;
            int activeProcessorCount = 0;

            while (currentPtr < endPtr)
            {
                var info = (SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX*)currentPtr;
                uint size = info->Size;
                uint remaining = (uint)(endPtr - currentPtr);

                // Additional boundary for unexpected data. 
                if (size == 0 || size > remaining)
                    break;

                var relationship = info->Relationship;
                switch (relationship)
                {
                    case LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorPackage:
                        ++processorPackageCount;
                        break;

                    case LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore:
                        ++physicalProcessorCoreCount;
                        break;

                    case LOGICAL_PROCESSOR_RELATIONSHIP.RelationGroup:
                        ref readonly GROUP_RELATIONSHIP group = ref info->Group;
                        for (int i = 0; i < group.ActiveGroupCount; i++)
                        {
                            var groupInfo = group.GroupInfo[i];
                            activeProcessorCount += groupInfo.ActiveProcessorCount;
                        }
                        break;

                    // Other types are skipped.
                    default:
                        break;
                }

                // Move pointer to next struct.
                currentPtr += size;
            }

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
