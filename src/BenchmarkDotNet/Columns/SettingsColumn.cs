using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;

namespace BenchmarkDotNet.Columns;

/// <summary>
/// Displays a single toolchain-settings value in the summary table. Settings types that expose the same key
/// (e.g. <c>CliPath</c> from the shared base) share one column.
/// <para>
/// The column is only shown when the value differs between the benchmarks that actually have the setting.
/// A benchmark whose toolchain lacks the setting renders as <c>NA</c> and does not affect that decision, so a
/// setting is not reported just because another toolchain lacks it. A null value renders as <c>?</c>.
/// </para>
/// </summary>
public class SettingsColumn : IColumn
{
    // Distinct marker for "this benchmark's settings don't expose the key"; rendered as NA and excluded from the
    // variance check. Kept separate from a null value (rendered as "?"), which means the setting exists but is unset.
    private static readonly object NotApplicable = new();
    private const string NotApplicableText = "NA";

    private readonly string key;

    public SettingsColumn(string key)
    {
        this.key = key;
        Id = $"Settings.{key}";
        ColumnName = key;
        Legend = $"Value of the '{key}' toolchain setting";
    }

    public string Id { get; }
    public string ColumnName { get; }
    public string Legend { get; }
    public bool AlwaysShow => false;
    public ColumnCategory Category => ColumnCategory.Job;
    public int PriorityInCategory => 1; // after the job characteristic columns, which use 0
    public bool IsNumeric => false;
    public UnitType UnitType => UnitType.Dimensionless;

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
    {
        object? value = GetRawValue(benchmarkCase);
        return ReferenceEquals(value, NotApplicable)
            ? NotApplicableText
            : value?.ToString() ?? ParameterInstance.NullParameterTextRepresentation;
    }

    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => ReferenceEquals(GetRawValue(benchmarkCase), NotApplicable);

    // Only show when the value varies among the benchmarks that actually have this setting; NA benchmarks don't count.
    // Values are compared by object equality, so implementations must emit value-comparable representations.
    public bool IsAvailable(Summary summary)
        => summary.BenchmarksCases
            .Select(GetRawValue)
            .Where(value => !ReferenceEquals(value, NotApplicable))
            .Distinct()
            .Take(2)
            .Count() > 1;

    private object? GetRawValue(BenchmarkCase benchmarkCase)
    {
        if ((benchmarkCase.GetToolchain() as IHasSettings)?.Settings is not { } settings)
            return NotApplicable;

        var filled = new Dictionary<string, object?>();
        settings.FillSettings(filled);
        return filled.TryGetValue(key, out object? value) ? value : NotApplicable;
    }
}
