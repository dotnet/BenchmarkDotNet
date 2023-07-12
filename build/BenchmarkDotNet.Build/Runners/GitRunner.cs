using System;
using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Core.IO;
using Cake.Git;

namespace BenchmarkDotNet.Build.Runners;

// Cake.Git 3.0.0 may experience the following issues on macOS:
// > Error: System.TypeInitializationException: The type initializer for 'LibGit2Sharp.Core.NativeMethods' threw an exception.
// >    ---> System.DllNotFoundException: Unable to load shared library 'git2-6777db8' or one of its dependencies. In order to help diagnose loading problems, consider setting the DYLD_PRINT_LIBRARIES environment variable
// In order to workaround this problem, we provide command-line fallbacks for all the used commands.
public class GitRunner
{
    private BuildContext context;

    public GitRunner(BuildContext context)
    {
        this.context = context;
    }

    public void Clone(DirectoryPath workDirectoryPath, string sourceUrl, string branchName)
    {
        context.Information($"[GitClone]");
        context.Information($"  Repo: {sourceUrl}");
        context.Information($"  Branch: {branchName}");
        context.Information($"  Path: {workDirectoryPath}");

        var settings = new GitCloneSettings { Checkout = true, BranchName = branchName };
        RunCommand(
            () => context.GitClone(sourceUrl, workDirectoryPath, settings),
            $"clone -b {branchName} {sourceUrl} {workDirectoryPath}");
    }

    public void Tag(string tagName)
    {
        context.Information("[GitTag]");
        context.Information($"  Path: {context.RootDirectory}");
        context.Information($"  TagName: {tagName}");

        RunCommand(
            () => context.GitTag(context.RootDirectory, tagName),
            $"tag {tagName}");
    }

    public void BranchMove(string branchName, string target)
    {
        context.Information("[GitBranchMove]");
        context.Information($"  Branch: {branchName}");
        context.Information($"  Target: {target}");
        RunCommand($"branch -f {branchName} {target}");
    }

    public void Commit(string message)
    {
        context.Information("[GitCommit]");
        context.Information($"  Message: {message}");
        RunCommand($"commit --all --message \"{message}\"");
    }

    public void Push(string target, bool force = false)
    {
        context.Information("[GitPush]");
        context.Information($"  Target: {target}");
        context.Information($"  Force: {force}");
        context.RunOnlyInPushMode(() =>
        {
            var forceFlag = force ? " --force" : "";
            RunCommand($"push origin {target}{forceFlag}");
        });
    }

    private void RunCommand(string commandLineArgs) => RunCommand(null, commandLineArgs);

    private void RunCommand(Action? call, string commandLineArgs)
    {
        try
        {
            if (call == null)
                throw new NotImplementedException();
            call();
            context.Information("  Success");
        }
        catch (Exception e)
        {
            if (e is not NotImplementedException)
                context.Information($"  Failed to perform operation via API ({e.Message})");
            try
            {
                context.Information($"  Run command in terminal: 'git {commandLineArgs}'");
                context.StartProcess("git", new ProcessSettings
                {
                    Arguments = commandLineArgs,
                    WorkingDirectory = context.RootDirectory
                });
                context.Information("  Success");
            }
            catch (Exception e2)
            {
                throw new Exception($"Failed to run 'git ${commandLineArgs}'", e2);
            }
        }
    }
}