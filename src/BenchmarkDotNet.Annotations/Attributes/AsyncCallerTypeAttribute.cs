using System;

namespace BenchmarkDotNet.Attributes;

/// <summary>
/// When applied to an async benchmark method, overrides the return type of the async method that calls the benchmark method.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AsyncCallerTypeAttribute(Type asyncCallerType) : Attribute
{
    /// <summary>
    /// The return type of the async method that calls the benchmark method.
    /// </summary>
    public Type AsyncCallerType { get; private set; } = asyncCallerType;
}
