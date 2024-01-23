---
uid: BenchmarkDotNet.SummaryStyle
---

## SummaryStyle in BenchmarkDotNet

`SummaryStyle` is a class in BenchmarkDotNet that allows customization of the summary reports of benchmark results. It offers several properties to fine-tune how the results are displayed.

### Usage

You can customize the summary report by specifying various properties of `SummaryStyle`. These properties include formatting options like whether to print units in the header or content, setting the maximum width for parameter columns, and choosing units for size and time measurements.

### Source Code

[!code-csharp[IntroSummaryStyle.cs](../../../samples/BenchmarkDotNet.Samples/IntroSummaryStyle.cs)]

### Properties

- `PrintUnitsInHeader`: Boolean to indicate if units should be printed in the header.
- `PrintUnitsInContent`: Boolean to control unit printing in the content.
- `PrintZeroValuesInContent`: Determines if zero values should be printed.
- `MaxParameterColumnWidth`: Integer defining the max width for parameter columns.
- `SizeUnit`: Optional `SizeUnit` to specify the unit for size measurements.
- `TimeUnit`: Optional `TimeUnit` for time measurement units.
- `CultureInfo`: `CultureInfo` to define culture-specific formatting.

### Example Output

Using SummaryStyle options:

```markdown
| Method | N   | Mean [ns]     | Error [ns] | StdDev [ns] |
|------- |---- |--------------:|-----------:|------------:|
| Sleep  | 10  |  15,644,973.1 |   32,808.7 |    30,689.3 |
| Sleep  | 100 | 109,440,686.7 |  236,673.8 |   221,384.8 |
```

Default:

```markdown
| Method | N   | Mean      | Error    | StdDev   |
|------- |---- |----------:|---------:|---------:|
| Sleep  | 10  |  15.65 ms | 0.039 ms | 0.034 ms |
| Sleep  | 100 | 109.20 ms | 0.442 ms | 0.392 ms |
```

### Links

* @docs.SummaryStyle
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroSummaryStyle

