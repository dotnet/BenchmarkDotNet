namespace BenchmarkDotNet.Toolchains;

/// <summary>
/// Implemented by toolchains that expose a settings record. Enables the summary table to surface settings
/// that differ between benchmarks using the same toolchain (see <see cref="Columns.SettingsColumn"/>).
/// </summary>
public interface IHasSettings
{
    /// <summary>The toolchain's settings.</summary>
    ISettings Settings { get; }
}
