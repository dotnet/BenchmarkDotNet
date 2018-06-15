---
uid: docs.columns
name: Columns
---

# Columns

A *column* is a column in the summary table.

## Default columns

In this section, default columns (which be added to the Summary table by default) are presented.
Some of columns are optional, i.e. they can be omitted (it depends on the measurements from the summary).

### Target

There are 3 default columns which describes the target benchmark:
  `Namespace`, `Type`, `Method`. `Namespace` and `Type` will be omitted
  when all the benchmarks have the same namespace or type name.
`Method` column always be a part of the summary table.

### Job

There are many different job characteristics,
  but the summary includes only characteristics which has at least one non-default value.

### Statistics

There are also a lot of different statistics which can be considered.
It will be really hard to analyse the summary table, if all of the available statistics will be shown.
Fortunately, BenchmarkDotNet has some heuristics for statistics columns and shows only important columns.
For example, if all of the standard deviations are zero (we run our benchmarks against Dry job),
  this column will be omitted.
The standard error will be shown only for cases when we are failed to achieve required accuracy level.

Only `Mean` will be always shown.
If the distribution looks strange,
  BenchmarkDotNet could also print additional columns like `Median` or `P95` (95th percentile).

If you need specific statistics, you always could add them manually.

### Params

If you have `params`, the corresponded columns will be automatically added.

### Diagnosers

If you turned on diagnosers which providers additional columns, they will be also included in the summary page.

## Custom columns

Of course, you can define own custom columns and use it everywhere. Here is the definition of `TagColumn`:

[!code-csharp[IntroTagColumn.cs](../../../src/BenchmarkDotNet/Columns/TagColumn.cs)]

---

[!include[IntroTagColumn](../samples/IntroTagColumn.md)]