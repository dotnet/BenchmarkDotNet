---
uid: BenchmarkDotNet.Samples.IntroWakeLock
---

## Sample: IntroWakeLock

Running benchmarks may sometimes take enough time such that the system enters sleep or turns off the display.

Using a WakeLock prevents the system doing so.

### Source code

[!code-csharp[IntroWakeLock.cs](../../../samples/BenchmarkDotNet.Samples/IntroWakeLock.cs)]

### Command line

```
--wakeLock None
```
```
--wakeLock System
```
```
--wakeLock Display
```

### Links

* @BenchmarkDotNet.Attributes.WakeLockAttribute
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroWakeLock

---
