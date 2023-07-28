---
uid: BenchmarkDotNet.Samples.IntroExceptionDiagnoser
---

## Sample: IntroExceptionDiagnoser

The `ExceptionDiagnoser` uses [AppDomain.FirstChanceException](https://learn.microsoft.com/en-us/dotnet/api/system.appdomain.firstchanceexception) API to report:

* Exception frequency: The number of exceptions thrown during the operations divided by the number of operations.

### Source code

[!code-csharp[IntroExceptionDiagnoser.cs](../../../samples/BenchmarkDotNet.Samples/IntroExceptionDiagnoser.cs)]

### Output

|                 Method |     Mean |     Error |    StdDev | Exception frequency |
|----------------------- |---------:|----------:|----------:|--------------------:|
| ThrowExceptionRandomly | 4.936 us | 0.1542 us | 0.4499 us |              0.1381 |

### Links

* @docs.diagnosers
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroExceptionDiagnoser

---