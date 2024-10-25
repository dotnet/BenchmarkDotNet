---
uid: BenchmarkDotNet.Samples.IntroWakeLock
---

## Sample: IntroWakeLock

Running Benchmarks usually takes enough time such that the system enters sleep or turns of the display.

Using a WakeLock prevents the system doing so.

### Source code

[!code-csharp[IntroWakeLock.cs](../../../samples/BenchmarkDotNet.Samples/IntroWakeLock.cs)]

### Command line

```
--preventSleep No
```
```
--preventSleep RequireSystem
```
```
--preventSleep RequireDisplay
```

### Links

* @BenchmarkDotNet.Attributes.WakeLockAttribute
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroWakeLock

---
