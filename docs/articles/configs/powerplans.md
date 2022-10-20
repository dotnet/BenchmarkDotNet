---
uid: docs.powerplans
name: Power Plans
---

# Power Plans

BenchmarkDotNet forces Windows OS to execute on the High-Performance power plan. You can disable this feature by modify PowerPlanMode property. You can see it in the @BenchmarkDotNet.Samples.IntroPowerPlan.

Please note. During an execution, BenchmarkDotNet saves the current power plan and applies it according to the PowerPlanMode property. When all of the benchmarks finish, a previous power plan comes back. However, if someone killed process or energy was plugged off, we could stay with the High-Performance power plan. In this situation, we should return it manually in Windows Control Panel or by powercfg command. 

### Links

* Power policy settings: https://learn.microsoft.com/windows/win32/power/power-policy-settings
* Powercfg command: https://learn.microsoft.com/windows-hardware/design/device-experiences/powercfg-command-line-options
* @BenchmarkDotNet.Samples.IntroPowerPlan

---