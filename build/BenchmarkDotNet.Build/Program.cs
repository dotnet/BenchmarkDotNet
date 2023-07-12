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

[TaskName("Restore")]
[TaskDescription("Restore NuGet packages")]
public class RestoreTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => context.BuildRunner.Restore();
}

[TaskName("Build")]
[TaskDescription("Build BenchmarkDotNet.sln solution")]
[IsDependentOn(typeof(RestoreTask))]
public class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => context.BuildRunner.Build();
}

[TaskName("UnitTests")]
[TaskDescription("Run unit tests (fast)")]
[IsDependentOn(typeof(BuildTask))]
public class UnitTestsTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => context.UnitTestRunner.RunUnitTests();
}

[TaskName("InTestsFull")]
[TaskDescription("Run integration tests using .NET Framework 4.6.2+ (slow)")]
[IsDependentOn(typeof(BuildTask))]
public class InTestsFullTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) => context.IsRunningOnWindows();

    public override void Run(BuildContext context) => context.UnitTestRunner.RunInTests("net462");
}

[TaskName("InTestsCore")]
[TaskDescription("Run integration tests using .NET 7 (slow)")]
[IsDependentOn(typeof(BuildTask))]
public class InTestsCoreTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => context.UnitTestRunner.RunInTests("net7.0");
}

[TaskName("AllTests")]
[TaskDescription("Run all unit and integration tests (slow)")]
[IsDependentOn(typeof(UnitTestsTask))]
[IsDependentOn(typeof(InTestsFullTask))]
[IsDependentOn(typeof(InTestsCoreTask))]
public class AllTestsTask : FrostingTask<BuildContext>
{
}

[TaskName("Pack")]
[TaskDescription("Pack Nupkg packages")]
[IsDependentOn(typeof(BuildTask))]
public class PackTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => context.BuildRunner.Pack();
}

[TaskName("CI")]
[TaskDescription("Perform all CI-related tasks: Restore, Build, AllTests, Pack")]
[IsDependentOn(typeof(BuildTask))]
[IsDependentOn(typeof(AllTestsTask))]
[IsDependentOn(typeof(PackTask))]
public class CiTask : FrostingTask<BuildContext>
{
}

[TaskName("DocsUpdate")]
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

[TaskName("DocsPrepare")]
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

// In order to work around xref issues in DocFx, BenchmarkDotNet and BenchmarkDotNet.Annotations must be build
// before running the DocFX_Build target. However, including a dependency on BuildTask here may have unwanted
// side effects (CleanTask).
// TODO: Define dependencies when a CI workflow scenario for using the "DocFX_Build" target exists.
[TaskName("DocsBuild")]
[TaskDescription("Build final documentation")]
[IsDependentOn(typeof(DocsPrepareTask))]
public class DocsBuildTask : FrostingTask<BuildContext>, IHelpProvider
{
    public override void Run(BuildContext context) => context.DocumentationRunner.Build();

    public HelpInfo GetHelp() => new()
    {
        Options = new IOption[] { KnownOptions.DocsPreview }
    };
}

[TaskName("Release")]
[TaskDescription("Release new version")]
[IsDependentOn(typeof(BuildTask))]
[IsDependentOn(typeof(PackTask))]
[IsDependentOn(typeof(DocsUpdateTask))]
[IsDependentOn(typeof(DocsBuildTask))]
public class ReleaseTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => context.ReleaseRunner.Run();
}

[TaskName("FastTests")]
[TaskDescription("OBSOLETE: use 'UnitTests'")]
[IsDependentOn(typeof(UnitTestsTask))]
public class FastTestsTask : FrostingTask<BuildContext>
{
}

[TaskName("SlowFullFrameworkTests")]
[TaskDescription("OBSOLETE: use 'InTestsFull'")]
[IsDependentOn(typeof(InTestsFullTask))]
public class SlowFullFrameworkTestsTask : FrostingTask<BuildContext>
{
}

[TaskName("SlowTestsNetCore")]
[TaskDescription("OBSOLETE: use 'InTestsCore'")]
[IsDependentOn(typeof(InTestsCoreTask))]
public class SlowTestsNetCoreTask : FrostingTask<BuildContext>
{
}

[TaskName("DocFX_Changelog_Download")]
[TaskDescription("OBSOLETE: use 'DocsUpdate'")]
[IsDependentOn(typeof(DocsUpdateTask))]
public class DocFxChangelogDownloadTask : FrostingTask<BuildContext>
{
}

[TaskName("DocFX_Changelog_Generate")]
[TaskDescription("OBSOLETE: use 'DocsPrepare'")]
[IsDependentOn(typeof(DocsPrepareTask))]
public class DocfxChangelogGenerateTask : FrostingTask<BuildContext>
{
}

[TaskName("DocFX_Generate_Redirects")]
[TaskDescription("OBSOLETE: use 'DocsBuild'")]
[IsDependentOn(typeof(DocsBuildTask))]
public class DocfxGenerateRedirectsTask : FrostingTask<BuildContext>
{
}

[TaskName("DocFX_Build")]
[TaskDescription("OBSOLETE: use 'DocsBuild'")]
[IsDependentOn(typeof(DocsBuildTask))]
public class DocfxBuildTask : FrostingTask<BuildContext>
{
}