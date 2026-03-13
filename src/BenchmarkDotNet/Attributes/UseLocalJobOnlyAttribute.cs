using System;
using BenchmarkDotNet.Configs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes;

[PublicAPI]
[AttributeUsage(AttributeTargets.Class)]
public class UseLocalJobOnlyAttribute : Attribute, IConfigSource
{
    public IConfig Config { get; }

    public UseLocalJobOnlyAttribute()
    {
        Config = ManualConfig.CreateEmpty();

        // `WithUnionRule(ConfigUnionRule.UnionAndUseLocalJob)` is not set here. Because it's not used.
    }
}
