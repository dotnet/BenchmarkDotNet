using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using JetBrains.Annotations;
using System.ComponentModel;

namespace BenchmarkDotNet.Diagnosers;

[UsedImplicitly]
[EditorBrowsable(EditorBrowsableState.Never)]
public struct InProcessDiagnoserRouter
{
    public IInProcessDiagnoserHandler handler;
    public int index;
    public RunMode runMode;

    public readonly string ToSourceCode()
        => $$"""
            new {{typeof(InProcessDiagnoserRouter).GetCorrectCSharpTypeName()}}() {
                {{nameof(handler)}} = {{handler.ToSourceCode()}},
                {{nameof(index)}} = {{index}},
                {{nameof(runMode)}} = {{SourceCodeHelper.ToSourceCode(runMode)}}
            }
            """;
}