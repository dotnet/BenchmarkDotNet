---
uid: docs.powerplans
name: Power Plans
---

# Power Plans

This description concerns only version v0.11.2 and above of BenchmarkDotNet. In the previous versions, benchmarks in the power plans.
BenchmarkDotNet forces Windows OS to execute on the High-Performance power plan. You can disable this feature by setting the HighPerofrmancePowerPlan flag to false. You can see it in the @BenchmarkDotNet.Samples.IntroPowerPlan.

Please note. During an execution, BenchmarkDotNet saves the current power plan and change if it is required according to the HighPerformancePowerPlan flag. When all of the benchmarks finish, a previous power plan comes back. However, if someone killed process or energy was plugged off, we could stay with the High-Performance power plan. In this situation, we should return it manually in Windows Control Panel or by powercfg command. 

### Links

* Power policy settings: https://docs.microsoft.com/en-us/windows/desktop/power/power-policy-settings
* Powercfg command: https://docs.microsoft.com/en-us/windows-hardware/design/device-experiences/powercfg-command-line-options
* @BenchmarkDotNet.Samples.IntroPowerPlan

---