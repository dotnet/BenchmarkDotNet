using System.Linq;
using Cake.Core.IO;
using Cake.FileHelpers;

namespace BenchmarkDotNet.Build.Meta;

public class VersionHistory
{
    public string FirstCommit { get; }
    public string[] StableVersions { get; }
    public string CurrentVersion { get; }
    
    public VersionHistory(BuildContext context, FilePath versionFilePath)
    {
        var lines = context.FileReadLines(versionFilePath).Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
        FirstCommit = lines.First();
        CurrentVersion = lines.Last();
        StableVersions = lines.Skip(1).SkipLast(1).ToArray();
    }
}