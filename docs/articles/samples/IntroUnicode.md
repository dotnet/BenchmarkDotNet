---
uid: BenchmarkDotNet.Samples.IntroUnicode
---

## Sample: IntroUnicode

Some of the BenchmarkDotNet exporters use Unicode symbols that are not ASCII-compatible (e.g., `μ` or `±`).
Unfortunately, some terminals are not supported such symbols.
That's why BenchmarkDotNet prints only ASCII characters by default (`μ` will be replaced by `u`).
If you want to display Unicode symbols in your terminal, you should use `[UnicodeConsoleLoggerAttribute]` (see usage examples below).

> [!WARNING]
> This feature works only with terminal(s)|text editor(s) that support Unicode.
> On Windows, you may have some troubles with Unicode symbols
>   if system default code page configured as non-English
>   (in Control Panel + Regional and Language Options, Language for Non-Unicode Programs).

### Source code

[!code-csharp[IntroUnicode.cs](../../../samples/BenchmarkDotNet.Samples/IntroUnicode.cs)]

### Output

```markdown
Mean = 1.0265 μs, StdErr = 0.0005 μs (0.05%); N = 15, StdDev = 0.0018 μs
Min = 1.0239 μs, Q1 = 1.0248 μs, Median = 1.0264 μs, Q3 = 1.0280 μs, Max = 1.0296 μs
IQR = 0.0033 μs, LowerFence = 1.0199 μs, UpperFence = 1.0329 μs
ConfidenceInterval = [1.0245 μs; 1.0285 μs] (CI 99.9%), Margin = 0.0020 μs (0.19% of Mean)
Skewness = 0.12, Kurtosis = 1.56, MValue = 2
-------------------- Histogram --------------------
[1.023 μs ; 1.030 μs) | @@@@@@@@@@@@@@@
---------------------------------------------------
```

```markdown
 Method |     Mean |     Error |    StdDev |
------- |---------:|----------:|----------:|
    Foo | 1.027 μs | 0.0020 μs | 0.0018 μs |
```

### Links

* @BenchmarkDotNet.Attributes.UnicodeConsoleLoggerAttribute
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroUnicode

---
