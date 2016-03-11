﻿using System;
using System.IO;
using System.Reflection;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Classic;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    internal class DotNetCliGenerator : ClassicGenerator
    {
        private const string ProjectFileName = "project.json";

        private Func<Framework, string> TargetFrameworkMonikerProvider { get; }

        private string ExtraDependencies { get; }

        private Func<Platform, string> PlatformProvider { get; }

        public DotNetCliGenerator(Func<Framework, string> targetFrameworkMonikerProvider, string extraDependencies, Func<Platform, string> platformProvider)
        {
            TargetFrameworkMonikerProvider = targetFrameworkMonikerProvider;
            ExtraDependencies = extraDependencies;
            PlatformProvider = platformProvider;
        }

        /// <summary>
        /// we need our folder to be on the same level as the project that we want to reference
        /// we are limited by xprojs (by default compiles all .cs files in all subfolders, Program.cs could be doubled and fail the build)
        /// and also by nuget internal implementation like looking for global.json file in parent folders
        /// </summary>
        protected override string GetDirectoryPath(Benchmark benchmark)
        {
            return Path.Combine(
                new DirectoryInfo(Directory.GetCurrentDirectory()).Parent.FullName, 
                benchmark.ShortInfo);
        }

        protected override void GenerateProjectFile(ILogger logger, string projectDir, Benchmark benchmark)
        {
            var template = ResourceHelper.LoadTemplate("BenchmarkProject.json");

            var content = SetPlatform(template, PlatformProvider(benchmark.Job.Platform));
            content = SetDependencyToExecutingAssembly(content, benchmark.Target.Type);
            content = SetTargetFrameworkMoniker(content, TargetFrameworkMonikerProvider(benchmark.Job.Framework));
            content = SetExtraDependencies(content, ExtraDependencies);

            var projectJsonFilePath = Path.Combine(projectDir, ProjectFileName);

            File.WriteAllText(projectJsonFilePath, content);
        }

        protected override void GenerateProjectBuildFile(string projectDir)
        {
            // do nothing on purpose, we do not need bat file
        }

        private static string SetPlatform(string template, string platform)
        {
            return template.Replace("$PLATFORM$", platform);
        }

        private static string SetDependencyToExecutingAssembly(string template, Type benchmarkTarget)
        {
            var assemblyName = benchmarkTarget.Assembly().GetName();
            var packageVersion = GetPackageVersion(assemblyName);

            return template
                .Replace("$EXECUTINGASSEMBLYVERSION$", packageVersion) 
                .Replace("$EXECUTINGASSEMBLY$", assemblyName.Name);
        }

        private static string SetTargetFrameworkMoniker(string content, string targetFrameworkMoniker)
        {
            return content.Replace("$TFM$", targetFrameworkMoniker);
        }

        private static string SetExtraDependencies(string content, string extraDependencies)
        {
            return content.Replace("$REQUIREDDEPENDENCY$", extraDependencies);
        }

        /// <summary>
        /// we can not simply call assemblyName.Version.ToString() because it is different than package version which can contain (and often does) text
        /// we are using the wildcard to get latest version of package/project restored
        /// </summary>
        private static string GetPackageVersion(AssemblyName assemblyName)
        {
            return $"{assemblyName.Version.Major}.{assemblyName.Version.Minor}.{assemblyName.Version.Build}-*";
        }
    }
}