using System;
using System.Collections.Generic;
using System.Linq;
using Cake.Common;
using Cake.Core;

namespace BenchmarkDotNet.Build.Options;

public abstract class Option<T> : IOption
{
    public string CommandLineName { get; }
    public string Description { get; init; } = "";
    public string[] Aliases { get; init; } = Array.Empty<string>();

    private IEnumerable<string> AllNames
    {
        get
        {
            yield return CommandLineName;
            foreach (var alias in Aliases)
                yield return alias;
        }
    }

    private IEnumerable<string> AllStrippedNames => AllNames.Select(name => name.TrimStart('-'));

    protected Option(string commandLineName)
    {
        CommandLineName = commandLineName;
    }

    public abstract T Resolve(BuildContext context);

    protected bool HasArgument(BuildContext context) => AllStrippedNames.Any(context.HasArgument);

    protected string? GetArgument(BuildContext context) => AllStrippedNames
        .Where(context.HasArgument)
        .Select(name => context.Arguments.GetArgument(name))
        .FirstOrDefault();
}