using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Locators;

public class ProjectFileLocator : IFileLocator
{
    private static readonly string[] ProjectExtensions = { ".csproj", ".fsproj", ".vbroj" };

    public static ProjectFileLocator Default { get; } = new ProjectFileLocator();

    public FileLocatorType LocatorType => FileLocatorType.Project;

    public bool TryLocate(LocatorArgs locatorArgs, out FileInfo fileInfo)
    {
        if (!GetRootDirectory(IsRootSolutionFolder, out var rootDirectory) && !GetRootDirectory(IsRootProjectFolder, out rootDirectory))
        {
            rootDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
        }

        // important assumption! project's file name === output dll name
        var type = locatorArgs.BenchmarkCase.Descriptor.Type;
        var projectName = type.GetTypeInfo().Assembly.GetName().Name;

        var possibleNames = new HashSet<string> { $"{projectName}.csproj", $"{projectName}.fsproj", $"{projectName}.vbproj" };
        var projectFiles = rootDirectory
                           .EnumerateFiles("*proj", SearchOption.AllDirectories)
                           .Where(file => possibleNames.Contains(file.Name))
                           .ToArray();

        if (projectFiles.Length == 0)
        {
            fileInfo = null;
            return false;
        }

        if (projectFiles.Length > 1)
        {
            throw new InvalidOperationException(
                $"Found more than one matching project file for {projectName} in {rootDirectory.FullName} and its subfolders: {string.Join(",", projectFiles.Select(pf => $"'{pf.FullName}'"))}. Benchmark project names needs to be unique.");
        }

        fileInfo = projectFiles[0];
        return true;
    }

    private static bool GetRootDirectory(Func<DirectoryInfo, bool> condition, out DirectoryInfo? directoryInfo)
    {
        directoryInfo = null;
        try
        {
            directoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directoryInfo != null)
            {
                if (condition(directoryInfo))
                {
                    return true;
                }

                directoryInfo = directoryInfo.Parent;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static bool IsRootSolutionFolder(DirectoryInfo directoryInfo)
        => directoryInfo
            .GetFileSystemInfos()
            .Any(fileInfo => fileInfo.Extension == ".sln" || fileInfo.Name == "global.json");

    private static bool IsRootProjectFolder(DirectoryInfo directoryInfo)
        => directoryInfo
            .GetFileSystemInfos()
            .Any(fileInfo => ProjectExtensions.Contains(fileInfo.Extension));
}