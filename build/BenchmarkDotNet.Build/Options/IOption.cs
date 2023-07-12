namespace BenchmarkDotNet.Build.Options;

public interface IOption
{
    string CommandLineName { get; }
    string Description { get; }
    string[] Aliases { get; }
}