using System.Collections.Generic;
using BenchmarkDotNet.Build.Options;

namespace BenchmarkDotNet.Build;

public class Example
{
    private readonly List<Argument> arguments = new();

    public string TaskName { get; }
    public IReadOnlyCollection<Argument> Arguments => arguments;

    public Example(string taskName)
    {
        TaskName = taskName;
    }

    public Example WithMsBuildArgument(string name, string value)
    {
        arguments.Add(new Argument(name, value, true));
        return this;
    }

    public Example WithArgument(BoolOption option)
    {
        arguments.Add(new Argument(option.CommandLineName, null, false));
        return this;
    }

    public Example WithArgument(StringOption option, string value)
    {
        arguments.Add(new Argument(option.CommandLineName, value, false));
        return this;
    }


    public record Argument(string Name, string? Value, bool IsMsBuild);
}