using BenchmarkDotNet.Build.Meta;
using BenchmarkDotNet.Build.Options;
using Cake.Common;
using Cake.Frosting;

namespace BenchmarkDotNet.Build;

public static class Program
{
    public static int Main(string[] args)
    {
        var cakeArgs = CommandLineParser.Instance.Parse(args);
        return cakeArgs == null
            ? 0
            : new CakeHost().UseContext<BuildContext>().Run(cakeArgs);
    }
}

[TaskName("restore")]
[TaskDescription("Restore NuGet packages")]
public class RestoreTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => context.BuildRunner.Restore();
}

[TaskName("build")]
[TaskDescription("Build BenchmarkDotNet.sln solution")]
[IsDependentOn(typeof(RestoreTask))]
public class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => context.BuildRunner.Build();
}

[TaskName("unit-tests")]
[TaskDescription("Run unit tests (fast)")]
[IsDependentOn(typeof(BuildTask))]
public class UnitTestsTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => context.UnitTestRunner.RunUnitTests();
}

[TaskName("in-tests-full")]
[TaskDescription("Run integration tests using .NET Framework 4.6.2+ (slow)")]
[IsDependentOn(typeof(BuildTask))]
public class InTestsFullTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) => context.IsRunningOnWindows();

    public override void Run(BuildContext context) => context.UnitTestRunner.RunInTests("net462");
}

[TaskName("in-tests-core")]
[TaskDescription("Run integration tests using .NET 7 (slow)")]
[IsDependentOn(typeof(BuildTask))]
public class InTestsCoreTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => context.UnitTestRunner.RunInTests("net7.0");
}

[TaskName("all-tests")]
[TaskDescription("Run all unit and integration tests (slow)")]
[IsDependentOn(typeof(UnitTestsTask))]
[IsDependentOn(typeof(InTestsFullTask))]
[IsDependentOn(typeof(InTestsCoreTask))]
public class AllTestsTask : FrostingTask<BuildContext>
{
}

[TaskName("pack")]
[TaskDescription("Pack Nupkg packages")]
[IsDependentOn(typeof(BuildTask))]
public class PackTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => context.BuildRunner.Pack();
}

[TaskName("docs-update")]
[TaskDescription("Update generated documentation files")]
public class DocsUpdateTask : FrostingTask<BuildContext>, IHelpProvider
{
    public override void Run(BuildContext context) => context.DocumentationRunner.Update();

    public HelpInfo GetHelp()
    {
        return new HelpInfo
        {
            Options = new IOption[] { KnownOptions.DocsPreview, KnownOptions.DocsDepth },
            EnvironmentVariables = new[] { GitHubCredentials.TokenVariableName }
        };
    }
}

[TaskName("docs-prepare")]
[TaskDescription("Prepare auxiliary documentation files")]
public class DocsPrepareTask : FrostingTask<BuildContext>, IHelpProvider
{
    public override void Run(BuildContext context) => context.DocumentationRunner.Prepare();

    public HelpInfo GetHelp()
    {
        return new HelpInfo
        {
            Options = new IOption[] { KnownOptions.DocsPreview }
        };
    }
}

[TaskName("docs-build")]
[TaskDescription("Build final documentation")]
[IsDependentOn(typeof(DocsPrepareTask))]
public class DocsBuildTask : FrostingTask<BuildContext>, IHelpProvider
{
    public override void Run(BuildContext context) => context.DocumentationRunner.Build();

    public HelpInfo GetHelp() => new()
    {
        Description = "The 'build' task should be run manually to build api docs",
        Options = new IOption[] { KnownOptions.DocsPreview }
    };
}

[TaskName("release")]
[TaskDescription("Release new version")]
[IsDependentOn(typeof(BuildTask))]
[IsDependentOn(typeof(PackTask))]
[IsDependentOn(typeof(DocsUpdateTask))]
[IsDependentOn(typeof(DocsBuildTask))]
public class ReleaseTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => context.ReleaseRunner.Run();
}