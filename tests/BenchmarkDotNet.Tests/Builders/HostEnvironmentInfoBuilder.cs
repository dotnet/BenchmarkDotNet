﻿using System;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Portability.Cpu;
using BenchmarkDotNet.Tests.Mocks;

namespace BenchmarkDotNet.Tests.Builders
{
    public class HostEnvironmentInfoBuilder
    {
        private string architecture = "64mock";
        private string benchmarkDotNetVersion = "0.10.x-mock";
        private Frequency chronometerFrequency = new Frequency(2531248);
        private string configuration = "CONFIGURATION";
        private string dotNetSdkVersion = "1.0.x.mock";
        private HardwareTimerKind hardwareTimerKind = HardwareTimerKind.Tsc;
        private bool hasAttachedDebugger = false;
        private bool hasRyuJit = true;
        private bool isConcurrentGC = false;
        private bool isServerGC = false;
        private string jitInfo = "RyuJIT-v4.6.x.mock";
        private string jitModules = "clrjit-v4.6.x.mock";
        private string osVersion = "Microsoft Windows NT 10.0.x.mock";
        private string runtimeVersion = "Clr 4.0.x.mock";
        private int logicalCoreCount = 8;
        private ICpuInfo cpuInfo = new MockCpuInfo
        {
            PhysicalCoreCount = 4,
            PhysicalProcessorCount = 1,
            LogicalCoreCount = 8,
            ProcessorName = "MockIntel(R) Core(TM) i7-6700HQ CPU 2.60GHz",
        };

        private VirtualMachineHypervisor virtualMachineHypervisor = HyperV.Default;

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

        public HostEnvironmentInfo Build()
        {
            return new MockHostEnvironmentInfo(architecture, benchmarkDotNetVersion, chronometerFrequency, configuration,
                dotNetSdkVersion, hardwareTimerKind, hasAttachedDebugger, hasRyuJit, isConcurrentGC, isServerGC,
                jitInfo, jitModules, osVersion, logicalCoreCount, cpuInfo, runtimeVersion, virtualMachineHypervisor);
        }
    }

    internal class MockHostEnvironmentInfo : HostEnvironmentInfo
    {
        public MockHostEnvironmentInfo(
            string architecture, string benchmarkDotNetVersion, Frequency chronometerFrequency, string configuration, string dotNetSdkVersion,
            HardwareTimerKind hardwareTimerKind, bool hasAttachedDebugger, bool hasRyuJit, bool isConcurrentGC, bool isServerGC,
            string jitInfo, string jitModules, string osVersion, int logicalCoreCount, ICpuInfo cpuInfo, 
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
            JitModules = jitModules;
            OsVersion = new Lazy<string>(() => osVersion);
            CpuInfo = new Lazy<ICpuInfo>(() => cpuInfo);
            LogicalCoreCount = logicalCoreCount;
            RuntimeVersion = runtimeVersion;
            VirtualMachineHypervisor = new Lazy<VirtualMachineHypervisor>(() => virtualMachineHypervisor);
        }
    }
}