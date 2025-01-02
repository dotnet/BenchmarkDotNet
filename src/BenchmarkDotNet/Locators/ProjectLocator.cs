using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Exporters;

namespace BenchmarkDotNet.Locators;

public class ProjectLocator : ILocator
{
    public static ProjectLocator Default { get; } = new ProjectLocator();

    public LocatorType LocatorType => LocatorType.ProjectFile;

    public FileInfo Locate(DirectoryInfo rootDirectory, Type type)
    {
        // important assumption! project's file name === output dll name
        string projectName = type.GetTypeInfo().Assembly.GetName().Name;

        var possibleNames = new HashSet<string> { $"{projectName}.csproj", $"{projectName}.fsproj", $"{projectName}.vbproj" };
        var projectFiles = rootDirectory
                           .EnumerateFiles("*proj", SearchOption.AllDirectories)
                           .Where(file => possibleNames.Contains(file.Name))
                           .ToArray();

        if (projectFiles.Length == 0)
        {
            return null;
        }

        if (projectFiles.Length > 1)
        {
            throw new NotSupportedException(
                $"Found more than one matching project file for {projectName} in {rootDirectory.FullName} and its subfolders: {string.Join(",", projectFiles.Select(pf => $"'{pf.FullName}'"))}. Benchmark project names needs to be unique.");
        }

        return projectFiles[0];
    }
}