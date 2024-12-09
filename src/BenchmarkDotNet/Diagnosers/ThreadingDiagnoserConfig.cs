using JetBrains.Annotations;

namespace BenchmarkDotNet.Diagnosers;

public class ThreadingDiagnoserConfig
{
    /// <param name="displayWorkItemsColumnIfZeroValue">Display Work Items column if it's value is not calculated. True by default.</param>
    /// <param name="displayLockContentionsColumnIfZeroValue">Display Lock Contentions column if it's value is not calculated. True by default.</param>
    [PublicAPI]
    public ThreadingDiagnoserConfig(bool displayWorkItemsColumnIfZeroValue = true, bool displayLockContentionsColumnIfZeroValue = true)
    {
        DisplayWorkItemsColumnIfZeroValue = displayWorkItemsColumnIfZeroValue;
        DisplayLockContentionsColumnIfZeroValue = displayLockContentionsColumnIfZeroValue;
    }
    public bool DisplayWorkItemsColumnIfZeroValue { get; }

    public bool DisplayLockContentionsColumnIfZeroValue { get; }
}