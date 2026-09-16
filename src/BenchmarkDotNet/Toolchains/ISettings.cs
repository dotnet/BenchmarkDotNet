using System.Collections.Generic;

namespace BenchmarkDotNet.Toolchains;

/// <summary>
/// A toolchain settings record that can surface its values in the summary table (see <see cref="Columns.SettingsColumn"/>).
/// </summary>
public interface ISettings
{
    /// <summary>
    /// Adds the settings to surface in the summary table. Keys become column names; values may be null.
    /// <para>
    /// Values are compared with <see cref="object.Equals(object)"/> to decide whether a column varies, and rendered
    /// via <see cref="object.ToString"/>, so they must be value-comparable — use <see cref="string"/>, primitives,
    /// enums or records. Emit a path's <see cref="System.IO.FileSystemInfo.FullName"/> rather than a
    /// <see cref="System.IO.FileInfo"/>/<see cref="System.IO.DirectoryInfo"/>, which compare by reference.
    /// </para>
    /// </summary>
    void FillSettings(IDictionary<string, object?> settings);
}
