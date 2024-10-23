using JetBrains.Annotations;

namespace BenchmarkDotNet.Diagnosers;

public class ExceptionDiagnoserConfig
{
    /// <param name="displayExceptionsIfZeroValue">Display Exceptions column if it's value is not calculated. True by default.</param>
    [PublicAPI]
    public ExceptionDiagnoserConfig(bool displayExceptionsIfZeroValue = true)
    {
        DisplayExceptionsIfZeroValue = displayExceptionsIfZeroValue;
    }

    public bool DisplayExceptionsIfZeroValue { get; }
}