﻿using System;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Portability.Cpu;
using BenchmarkDotNet.Portability.Memory;

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

        private CpuInfo cpuInfo = new CpuInfo("MockIntel(R) Core(TM) i7-6700HQ CPU 2.60GHz",
                                              physicalProcessorCount: 1,
                                              physicalCoreCount: 4,
                                              logicalCoreCount: 8,
                                              nominalFrequency: Frequency.FromMHz(3100),
                                              maxFrequency: Frequency.FromMHz(3100),
                                              minFrequency: Frequency.FromMHz(3100));

        private MemoryInfo memoryInfo = new MemoryInfo(8309008, 2883320);

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

        public HostEnvironmentInfoBuilder WithoutDotNetSdkVersion()
        {
            dotNetSdkVersion = null;
            return this;
        }

        public HostEnvironmentInfo Build()
        {
            return new MockHostEnvironmentInfo(architecture, benchmarkDotNetVersion, chronometerFrequency, configuration,
                dotNetSdkVersion, hardwareTimerKind, hasAttachedDebugger, hasRyuJit, isConcurrentGC, isServerGC,
                jitInfo, jitModules, osVersion, cpuInfo, memoryInfo, runtimeVersion, virtualMachineHypervisor);
        }
    }

    internal class MockHostEnvironmentInfo : HostEnvironmentInfo
    {
        public MockHostEnvironmentInfo(
            string architecture, string benchmarkDotNetVersion, Frequency chronometerFrequency, string configuration, string dotNetSdkVersion,
            HardwareTimerKind hardwareTimerKind, bool hasAttachedDebugger, bool hasRyuJit, bool isConcurrentGC, bool isServerGC,
            string jitInfo, string jitModules, string osVersion, CpuInfo cpuInfo, MemoryInfo memoryInfo,
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
            CpuInfo = new Lazy<CpuInfo>(() => cpuInfo);
            MemoryInfo = new Lazy<MemoryInfo>(() => memoryInfo);
            RuntimeVersion = runtimeVersion;
            VirtualMachineHypervisor = new Lazy<VirtualMachineHypervisor>(() => virtualMachineHypervisor);
        }
    }
}