﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BenchmarkDotNet.Characteristics;
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
        private const string DefaultSdkName = "Microsoft.NET.Sdk";

        private static readonly ImmutableArray<string> SettingsWeWantToCopy =
            new[] { "NetCoreAppImplicitPackageVersion", "RuntimeFrameworkVersion", "PackageTargetFallback", "LangVersion", "UseWpf", "UseWindowsForms", "CopyLocalLockFileAssemblies", "PreserveCompilationContext", "UserSecretsId" }.ToImmutableArray();

        public string RuntimeFrameworkVersion { get; }

        public CsProjGenerator(string targetFrameworkMoniker, string cliPath, string packagesPath, string runtimeFrameworkVersion)            : base(targetFrameworkMoniker, cliPath, packagesPath)
        {
            RuntimeFrameworkVersion = runtimeFrameworkVersion;
        }

        protected override string GetBuildArtifactsDirectoryPath(BuildPartition buildPartition, string programName)
        {
            string assemblyLocation = buildPartition.AssemblyLocation;

            //Assembles loaded from a stream will have an empty location (https://docs.microsoft.com/en-us/dotnet/api/system.reflection.assembly.location).
            string directoryName = assemblyLocation.IsEmpty() ?
                Path.Combine(Directory.GetCurrentDirectory(), "BenchmarkDotNet.Bin") :
                Path.GetDirectoryName(buildPartition.AssemblyLocation);

            return Path.Combine(directoryName, programName);
        }

        protected override string GetProjectFilePath(string buildArtifactsDirectoryPath)
            => Path.Combine(buildArtifactsDirectoryPath, "BenchmarkDotNet.Autogenerated.csproj");

        protected override string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath, string configuration)
            => Path.Combine(buildArtifactsDirectoryPath, "bin", configuration, TargetFrameworkMoniker);

        [SuppressMessage("ReSharper", "StringLiteralTypo")] // R# complains about $variables$
        protected override void GenerateProject(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, ILogger logger)
        {
            var benchmark = buildPartition.RepresentativeBenchmarkCase;
            var projectFile = GetProjectFilePath(benchmark.Descriptor.Type, logger);

            using (var file = new StreamReader(File.OpenRead(projectFile.FullName)))
            {
                var (customProperties, sdkName) = GetSettingsThatNeedsToBeCopied(file, projectFile);

                var content = new StringBuilder(ResourceHelper.LoadTemplate("CsProj.txt"))
                    .Replace("$PLATFORM$", buildPartition.Platform.ToConfig())
                    .Replace("$CODEFILENAME$", Path.GetFileName(artifactsPaths.ProgramCodePath))
                    .Replace("$CSPROJPATH$", projectFile.FullName)
                    .Replace("$TFM$", TargetFrameworkMoniker)
                    .Replace("$PROGRAMNAME$", artifactsPaths.ProgramName)
                    .Replace("$RUNTIMESETTINGS$", GetRuntimeSettings(benchmark.Job.Environment.Gc, buildPartition.Resolver))
                    .Replace("$COPIEDSETTINGS$", customProperties)
                    .Replace("$CONFIGURATIONNAME$", buildPartition.BuildConfiguration)
                    .Replace("$SDKNAME$", sdkName)
                    .ToString();

                File.WriteAllText(artifactsPaths.ProjectFilePath, content);
            }
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
        internal (string customProperties, string sdkName) GetSettingsThatNeedsToBeCopied(TextReader streamReader, FileInfo projectFile)
        {
            if (!string.IsNullOrEmpty(RuntimeFrameworkVersion)) // some power users knows what to configure, just do it and copy nothing more
                return ($"<RuntimeFrameworkVersion>{RuntimeFrameworkVersion}</RuntimeFrameworkVersion>", DefaultSdkName);

            var customProperties = new StringBuilder();
            var sdkName = DefaultSdkName;

            string line;
            while ((line = streamReader.ReadLine()) != null)
            {
                var trimmedLine = line.Trim();

                foreach (string setting in SettingsWeWantToCopy)
                    if (trimmedLine.Contains(setting))
                        customProperties.Append(trimmedLine);

                if (trimmedLine.StartsWith("<Import Project"))
                {
                    string propsFilePath = trimmedLine.Split('"')[1]; // its sth like   <Import Project="..\..\build\common.props" />
                    var directoryName = projectFile.DirectoryName ?? throw new DirectoryNotFoundException(projectFile.DirectoryName);
                    string absolutePath = File.Exists(propsFilePath)
                        ? propsFilePath // absolute path or relative to current dir
                        : Path.Combine(directoryName, propsFilePath); // relative to csproj

                    if (File.Exists(absolutePath))
                        using (var importedFile = new StreamReader(File.OpenRead(absolutePath)))
                            customProperties.Append(GetSettingsThatNeedsToBeCopied(importedFile, new FileInfo(absolutePath)).customProperties);
                }

                // custom SDKs are not added for non-netcoreapp apps (like net471), so when the TFM != netcoreapp we dont parse "<Import Sdk="
                // we don't allow for that mostly to prevent from edge cases like the following
                // <Import Sdk="Microsoft.NET.Sdk.WindowsDesktop" Project="Sdk.props" Condition="'$(TargetFramework)'=='netcoreapp3.0'"/>
                if (trimmedLine.StartsWith("<Project Sdk=\"")
                    || (TargetFrameworkMoniker.StartsWith("netcoreapp", StringComparison.InvariantCultureIgnoreCase) &&  trimmedLine.StartsWith("<Import Sdk=\"")))
                    sdkName = trimmedLine.Split('"')[1]; // its sth like Sdk="name"
            }

            return (customProperties.ToString(), sdkName);
        }

        /// <summary>
        /// returns a path to the project file which defines the benchmarks
        /// </summary>
        [PublicAPI]
        protected virtual FileInfo GetProjectFilePath(Type benchmarkTarget, ILogger logger)
        {
            if (!GetSolutionRootDirectory(out var rootDirectory) && !GetProjectRootDirectory(out rootDirectory))
            {
                logger.WriteLineError(
                    $"Unable to find .sln or .csproj file. Will use current directory {Directory.GetCurrentDirectory()} to search for project file. If you don't use .sln file on purpose it should not be a problem.");
                rootDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
            }

            // important assumption! project's file name === output dll name
            string projectName = benchmarkTarget.GetTypeInfo().Assembly.GetName().Name;

            // I was afraid of using .GetFiles with some smart search pattern due to the fact that the method was designed for Windows
            // and now .NET is cross platform so who knows if the pattern would be supported for other OSes
            var possibleNames = new HashSet<string> { $"{projectName}.csproj", $"{projectName}.fsproj", $"{projectName}.vbproj" };
            var projectFile = rootDirectory
                .EnumerateFiles("*.*", SearchOption.AllDirectories)
                .FirstOrDefault(file => possibleNames.Contains(file.Name));

            if (projectFile == default(FileInfo))
            {
                throw new NotSupportedException(
                    $"Unable to find {projectName} in {rootDirectory.FullName} and its subfolders. Most probably the name of output exe is different than the name of the .(c/f)sproj");
            }
            return projectFile;
        }
    }
}
