using System.Collections.Generic;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains
{
    public interface IToolchain
    {
        [PublicAPI] string Name { get; }
        IGenerator Generator { get; }
        IBuilder Builder { get; }
        IExecutor Executor { get; }
        bool IsInProcess { get; }

        IEnumerable<ValidationError> Validate(BenchmarkCase benchmarkCase, IResolver resolver);
    }
}