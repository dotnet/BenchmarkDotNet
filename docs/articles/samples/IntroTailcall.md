---
uid: BenchmarkDotNet.Samples.IntroTailcall
---

## Sample: IntroTailcall

You need to use the `TailcallDiagnoser` attribute to configure it. The available options are:

* logFailuresOnly: Track only the methods that failed to get tail called. True by default.
* filterByNamespace : Track only the methods from declaring type's namespace. Set to false if you want to see all Jit tail events. True by default.

### Restrictions

* Windows only
* x64

### Source code

[!code-csharp[IntroTailcall.cs](../../../samples/BenchmarkDotNet.Samples/IntroTailcall.cs)]

### Output

```markdown
// * Diagnostic Output - TailCallDiagnoser *
--------------------

--------------------
Jit_TailCalling.Calc: LegacyJitX64(Jit=LegacyJit, Platform=X64, Runtime=Clr)
--------------------

--------------------
Jit_TailCalling.Calc: LegacyJitX86(Jit=LegacyJit, Platform=X86, Runtime=Clr)
--------------------

--------------------
Jit_TailCalling.Calc: RyuJitX64(Jit=RyuJit, Platform=X64)
--------------------
Caller: <null>.<null> - <null>
Callee: BenchmarkDotNet.Samples.JIT.Jit_TailCalling.FactorialWithTailing - int64  (int32,int32)
Tail prefix: False
Tail call type: RecursiveLoop
-------------------
```

### Links

* @docs.diagnosers
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroTailcall

---