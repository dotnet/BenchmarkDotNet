using System;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Portability;
using Perfolizer.Horology;
using Perfolizer.Phd.Dto;

namespace BenchmarkDotNet.Tests.Builders
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class HostEnvironmentInfoBuilder
    {
        private string architecture = "64mock";
        private string benchmarkDotNetVersion = "0.10.x-mock";
        private Frequency chronometerFrequency = new Frequency(2531248);
        private string configuration = "CONFIGURATION";
        private string? dotNetSdkVersion = "1.0.x.mock";
        private HardwareTimerKind hardwareTimerKind = HardwareTimerKind.Tsc;
        private bool hasAttachedDebugger = false;
        private bool hasRyuJit = true;
        private bool isConcurrentGC = false;
        private bool isServerGC = false;
        private string jitInfo = "RyuJIT-v4.6.x.mock";
        private string jitModules = "clrjit-v4.6.x.mock";
        private PhdOs os = new () { Display = "Microsoft Windows NT 10.0.x.mock" };
        private string runtimeVersion = "Clr 4.0.x.mock";

        private readonly PhdCpu cpu = new ()
        {
            ProcessorName = "MockIntel(R) Core(TM) i7-6700HQ CPU 2.60GHz",
            PhysicalProcessorCount = 1,
            PhysicalCoreCount = 4,
            LogicalCoreCount = 8,
            NominalFrequencyHz = Frequency.FromMHz(3100).Hertz.RoundToLong(),
            MaxFrequencyHz = Frequency.FromMHz(3100).Hertz.RoundToLong()
        };

        private VirtualMachineHypervisor? virtualMachineHypervisor = HyperV.Default;

        public HostEnvironmentInfoBuilder WithVMHypervisor(VirtualMachineHypervisor hypervisor)
        {
            virtualMachineHypervisor = hypervisor;
            return this;
        }

        public HostEnvironmentInfoBuilder WithoutVMHypervisor()
        {
            virtualMachineHypervisor = null;
            return this;
        }

        public HostEnvironmentInfoBuilder WithoutDotNetSdkVersion()
        {
            dotNetSdkVersion = null;
            return this;
        }

        public HostEnvironmentInfo Build()
        {
            return new MockHostEnvironmentInfo(architecture, benchmarkDotNetVersion, chronometerFrequency, configuration,
                dotNetSdkVersion, hardwareTimerKind, hasAttachedDebugger, hasRyuJit, isConcurrentGC, isServerGC,
                jitInfo, jitModules, os, cpu, runtimeVersion, virtualMachineHypervisor);
        }
    }

    internal class MockHostEnvironmentInfo : HostEnvironmentInfo
    {
        public MockHostEnvironmentInfo(
            string architecture, string benchmarkDotNetVersion, Frequency chronometerFrequency, string configuration, string dotNetSdkVersion,
            HardwareTimerKind hardwareTimerKind, bool hasAttachedDebugger, bool hasRyuJit, bool isConcurrentGC, bool isServerGC,
            string jitInfo, string jitModules, PhdOs os, PhdCpu cpu,
            string runtimeVersion, VirtualMachineHypervisor virtualMachineHypervisor)
        {
            Architecture = architecture;
            BenchmarkDotNetVersion = benchmarkDotNetVersion;
            ChronometerFrequency = chronometerFrequency;
            Configuration = configuration;
            DotNetSdkVersion = new Lazy<string>(() => dotNetSdkVersion);
            HardwareTimerKind = hardwareTimerKind;
            HasAttachedDebugger = hasAttachedDebugger;
            HasRyuJit = hasRyuJit;
            IsConcurrentGC = isConcurrentGC;
            IsServerGC = isServerGC;
            JitInfo = jitInfo;
            HardwareIntrinsicsShort = "";
            Os = new Lazy<PhdOs>(() => os);
            Cpu = new Lazy<PhdCpu>(() => cpu);
            RuntimeVersion = runtimeVersion;
            VirtualMachineHypervisor = new Lazy<VirtualMachineHypervisor>(() => virtualMachineHypervisor);
        }
    }
}