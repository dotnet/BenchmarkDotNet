using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace BenchmarkDotNet.Toolchains.InProcess.NoEmit;

public interface IBenchmarkActionFactory
{
    bool TryCreate(object instance, MethodInfo targetMethod, int unrollFactor, [NotNullWhen(true)] out IBenchmarkAction? benchmarkAction);
}
