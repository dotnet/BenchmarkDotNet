namespace BenchmarkDotNet.Build.Options;

public static class KnownOptions
{
    public static readonly StringOption Verbosity = new("--verbosity"
    )
    {
        Description = "Specifies the amount of information to be displayed\n" +
                      "(Quiet, Minimal, Normal, Verbose, Diagnostic)",
        Aliases = new[] { "-v" }
    };

    public static readonly BoolOption Exclusive = new("--exclusive")
    {
        Description = "Executes the target task without any dependencies",
        Aliases = new[] { "-e" }
    };

    public static readonly BoolOption DocsPreview = new("--preview")
    {
        Description = "When specified, documentation changelog includes the upcoming version",
        Aliases = new[] { "-p" }
    };

    public static readonly StringOption DocsDepth = new("--depth")
    {
        Description = "The number of last stable versions that requires changelog regenerations\n" +
                      "Use 'all' for all values. The default is zero.",
        Aliases = new[] { "-d" }
    };

    public static readonly BoolOption Help = new("--help")
    {
        Description = "Prints help information",
        Aliases = new[] { "-h" }
    };

    public static readonly BoolOption Stable = new("--stable")
    {
        Description = "Removes VersionSuffix in MSBuild settings",
        Aliases = new[] { "-s" }
    };

    public static readonly StringOption NextVersion = new("--next-version")
    {
        Description = "Specifies next version number",
        Aliases = new[] { "-n" }
    };
    
    public static readonly BoolOption Push = new("--push")
    {
        Description = "When specified, the task actually perform push to GitHub and nuget.org"
    };
}