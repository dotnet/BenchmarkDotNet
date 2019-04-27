---
uid: BenchmarkDotNet.Samples.IntroPowerPlan
---

## Sample: IntroPowerPlan

This sample shows how we can manipulate with power plans. In BenchmarkDotNet we could change power plan in two ways. The first one is to set one from the list:
* PowerSaver, guid: a1841308-3541-4fab-bc81-f71556f20b4a
* Balanced, guid: 381b4222-f694-41f0-9685-ff5bb260df2e
* High-Performance, guid: 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c (the default one)
* UserPowerPlan (a current power plan set in computer)
The second one rely on guid string. We could easily found currently set guids with cmd command *powercfg /list*. If we set power plans in two ways at the same time, the second one will be used.

### Source code

[!code-csharp[IntroPowerPlan.cs](../../../samples/BenchmarkDotNet.Samples/IntroPowerPlan.cs)]

### Links

* @docs.powerplans
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroPowerPlan

---