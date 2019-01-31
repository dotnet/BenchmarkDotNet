﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.CsProj
{
    [PublicAPI]
    public class CsProjGenerator : DotNetCliGenerator
    {
        public string RuntimeFrameworkVersion { get; }

        public CsProjGenerator(string targetFrameworkMoniker, string cliPath, string packagesPath, string runtimeFrameworkVersion)
            : base(targetFrameworkMoniker, cliPath, packagesPath)
        {
            RuntimeFrameworkVersion = runtimeFrameworkVersion;
        }

        protected override string GetBuildArtifactsDirectoryPath(BuildPartition buildPartition, string programName)
        {
            string directoryName = Path.GetDirectoryName(buildPartition.AssemblyLocation)
                ?? throw new DirectoryNotFoundException(buildPartition.AssemblyLocation);
            return Path.Combine(directoryName, programName);
        }

        protected override string GetProjectFilePath(string buildArtifactsDirectoryPath)
            => Path.Combine(buildArtifactsDirectoryPath, "BenchmarkDotNet.Autogenerated.csproj");

        protected override string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath, string configuration)
            => Path.Combine(buildArtifactsDirectoryPath, "bin", configuration, TargetFrameworkMoniker);

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        protected override void GenerateProject(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, ILogger logger)
        {
            string template = ResourceHelper.LoadTemplate("CsProj.txt");
            var benchmark = buildPartition.RepresentativeBenchmarkCase;
            var projectFile = GetProjectFilePath(benchmark.Descriptor.Type, logger);

            string platform = buildPartition.Platform.ToConfig();
            string content = SetPlatform(template, platform);
            content = SetCodeFileName(content, Path.GetFileName(artifactsPaths.ProgramCodePath));
            content = content.Replace("$CSPROJPATH$", projectFile.FullName);
            content = SetTargetFrameworkMoniker(content, TargetFrameworkMoniker);
            content = content.Replace("$PROGRAMNAME$", artifactsPaths.ProgramName);
            content = content.Replace("$RUNTIMESETTINGS$", GetRuntimeSettings(benchmark.Job.Environment.Gc, buildPartition.Resolver));
            content = content.Replace("$COPIEDSETTINGS$", GetSettingsThatNeedsToBeCopied(projectFile));
            content = content.Replace("$CONFIGURATIONNAME$", buildPartition.BuildConfiguration);
            content = content.Replace("$REDIRECTS$", benchmark.Job.HasValue(InfrastructureMode.NuGetReferencesCharacteristic).ToLowerCase());

            File.WriteAllText(artifactsPaths.ProjectFilePath, content);
        }

        /// <summary>
        /// returns an MSBuild string that defines Runtime settings
        /// </summary>
        [PublicAPI]
        protected virtual string GetRuntimeSettings(GcMode gcMode, IResolver resolver)
        {
            if (!gcMode.HasChanges)
                return string.Empty;

            return new StringBuilder(80)
                .AppendLine("<PropertyGroup>")
                    .AppendLine($"<ServerGarbageCollection>{gcMode.ResolveValue(GcMode.ServerCharacteristic, resolver).ToLowerCase()}</ServerGarbageCollection>")
                    .AppendLine($"<ConcurrentGarbageCollection>{gcMode.ResolveValue(GcMode.ConcurrentCharacteristic, resolver).ToLowerCase()}</ConcurrentGarbageCollection>")
                    .AppendLine($"<RetainVMGarbageCollection>{gcMode.ResolveValue(GcMode.RetainVmCharacteristic, resolver).ToLowerCase()}</RetainVMGarbageCollection>")
                .AppendLine("</PropertyGroup>")
                .ToString();
        }

        // the host project or one of the .props file that it imports might contain some custom settings that needs to be copied, sth like
        // <NetCoreAppImplicitPackageVersion>2.0.0-beta-001607-00</NetCoreAppImplicitPackageVersion>
	    // <RuntimeFrameworkVersion>2.0.0-beta-001607-00</RuntimeFrameworkVersion>
        private string GetSettingsThatNeedsToBeCopied(FileInfo projectFile)
        {
            if (!string.IsNullOrEmpty(RuntimeFrameworkVersion)) // some power users knows what to configure, just do it and copy nothing more
                return $"<RuntimeFrameworkVersion>{RuntimeFrameworkVersion}</RuntimeFrameworkVersion>";

            var customSettings = new StringBuilder();
            using (var file = new StreamReader(File.OpenRead(projectFile.FullName)))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    if (line.Contains("NetCoreAppImplicitPackageVersion") || line.Contains("RuntimeFrameworkVersion") || line.Contains("PackageTargetFallback") || line.Contains("LangVersion"))
                    {
                        customSettings.Append(line);
                    }
                    else if (line.Contains("<Import Project"))
                    {
                        string propsFilePath = line.Trim().Split('"')[1]; // its sth like   <Import Project="..\..\build\common.props" />
                        var directoryName = projectFile.DirectoryName ?? throw new DirectoryNotFoundException(projectFile.DirectoryName);
                        string absolutePath = File.Exists(propsFilePath)
                            ? propsFilePath // absolute path or relative to current dir
                            : Path.Combine(directoryName, propsFilePath); // relative to csproj

                        if (File.Exists(absolutePath))
                        {
                            customSettings.Append(GetSettingsThatNeedsToBeCopied(new FileInfo(absolutePath)));
                        }
                    }
                }
            }

            return customSettings.ToString();
        }

        /// <summary>
        /// returns a path to the project file which defines the benchmarks
        /// </summary>
        [PublicAPI]
        protected virtual FileInfo GetProjectFilePath(Type benchmarkTarget, ILogger logger)
        {
            if (!GetSolutionRootDirectory(out var solutionRootDirectory))
            {
                logger.WriteLineError(
                    $"Unable to find .sln file. Will use current directory {Directory.GetCurrentDirectory()} to search for project file. If you don't use .sln file on purpose it should not be a problem.");
                solutionRootDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
            }

            // important assumption! project's file name === output dll name
            string projectName = benchmarkTarget.GetTypeInfo().Assembly.GetName().Name;

            // I was afraid of using .GetFiles with some smart search pattern due to the fact that the method was designed for Windows
            // and now .NET is cross platform so who knows if the pattern would be supported for other OSes
            var possibleNames = new HashSet<string> { $"{projectName}.csproj", $"{projectName}.fsproj", $"{projectName}.vbproj" };
            var projectFile = solutionRootDirectory
                .EnumerateFiles("*.*", SearchOption.AllDirectories)
                .FirstOrDefault(file => possibleNames.Contains(file.Name));

            if (projectFile == default(FileInfo))
            {
                throw new NotSupportedException(
                    $"Unable to find {projectName} in {solutionRootDirectory.FullName} and its subfolders. Most probably the name of output exe is different than the name of the .(c/f)sproj");
            }
            return projectFile;
        }
    }
}
