using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Core.IO;

namespace BenchmarkDotNet.Build.Runners;

public class SelfTestRunner
{
    private readonly BuildContext context;
    private readonly FilePath buildCmd;
    private readonly DirectoryPath testArtifactsDir;

    public SelfTestRunner(BuildContext context)
    {
        this.context = context;
        this.buildCmd = context.RootDirectory.CombineWithFilePath("build.cmd");
        this.testArtifactsDir = context.RootDirectory.Combine("test-artifacts");
    }

    public void Run()
    {
        context.Information("Running build system self-tests...");

        // Clean up test artifacts directory
        if (context.DirectoryExists(testArtifactsDir))
            context.CleanDirectory(testArtifactsDir);
        
        // Define test cases
        var testCases = new[]
        {
            new TestCase
            {
                Name = "Default pack (develop build)",
                Arguments = "pack",
                ExpectedVersionPattern = @"^0\.\d+\.\d+-develop(-\d+-g[a-f0-9]+)?$",
                Description = "Should produce version like 0.15.0-develop or 0.15.0-develop-6-g7da69ade"
            },
            new TestCase
            {
                Name = "Pack with VersionSuffix",
                Arguments = "pack /p:VersionSuffix=nightly.123",
                ExpectedVersionPattern = @"^0\.\d+\.\d+-nightly\.123$",
                Description = "Should produce version like 0.15.1-nightly.123"
            },
            new TestCase
            {
                Name = "Stable pack",
                Arguments = "pack --stable",
                ExpectedVersionPattern = @"^0\.\d+\.\d+$",
                Description = "Should produce version like 0.15.1 (no suffix)"
            },
            new TestCase
            {
                Name = "Stable pack with major bump",
                Arguments = "pack /p:BumpMajorVersion=true --stable",
                ExpectedVersionPattern = @"^0\.\d+\.0$",
                Description = "Should produce version like 0.16.0 (major bumped, minor reset)"
            }
        };

        var failedTests = new List<string>();
        int testNumber = 0;

        foreach (var testCase in testCases)
        {
            testNumber++;
            context.Information($"\n[{testNumber}/{testCases.Length}] {testCase.Name}");
            context.Information($"    Arguments: {testCase.Arguments}");
            context.Information($"    Expected: {testCase.Description}");

            try
            {
                // Create test-specific artifacts directory
                var testArtifacts = testArtifactsDir.Combine($"test{testNumber}");
                context.EnsureDirectoryExists(testArtifacts);

                // Run build.cmd with the specified arguments
                var result = RunBuildCommand(testCase.Arguments, testArtifacts);

                if (result.ExitCode != 0)
                {
                    context.Error($"    FAILED: Build command exited with code {result.ExitCode}");
                    failedTests.Add($"{testCase.Name}: Build failed with exit code {result.ExitCode}");
                    continue;
                }

                // Find and validate the produced packages
                var packages = context.GetFiles(testArtifacts.FullPath + "/*.nupkg")
                    .Where(f => !f.FullPath.Contains(".symbols."))
                    .ToList();

                if (packages.Count == 0)
                {
                    context.Error($"    FAILED: No packages found in {testArtifacts}");
                    failedTests.Add($"{testCase.Name}: No packages produced");
                    continue;
                }

                // Check version of the first package (they should all have the same version)
                var packagePath = packages.First();
                var version = ExtractVersionFromPackage(packagePath);

                if (string.IsNullOrEmpty(version))
                {
                    context.Error($"    FAILED: Could not extract version from {packagePath.GetFilename()}");
                    failedTests.Add($"{testCase.Name}: Could not extract version");
                    continue;
                }

                // Validate version format
                if (!Regex.IsMatch(version, testCase.ExpectedVersionPattern))
                {
                    context.Error($"    FAILED: Version '{version}' does not match expected pattern '{testCase.ExpectedVersionPattern}'");
                    failedTests.Add($"{testCase.Name}: Version '{version}' does not match expected pattern");
                    continue;
                }

                // Ensure version starts with 0
                if (!version.StartsWith("0."))
                {
                    context.Error($"    FAILED: Version '{version}' does not start with '0.'");
                    failedTests.Add($"{testCase.Name}: Version must start with '0.'");
                    continue;
                }

                context.Information($"    SUCCESS: Version = {version}");
            }
            catch (Exception ex)
            {
                context.Error($"    FAILED: {ex.Message}");
                failedTests.Add($"{testCase.Name}: {ex.Message}");
            }
        }

        // Summary
        context.Information($"\n========================================");
        context.Information($"Self-test Summary:");
        context.Information($"Total tests: {testCases.Length}");
        context.Information($"Passed: {testCases.Length - failedTests.Count}");
        context.Information($"Failed: {failedTests.Count}");

        if (failedTests.Any())
        {
            context.Information($"\nFailed tests:");
            foreach (var failure in failedTests)
            {
                context.Error($"  - {failure}");
            }
            throw new Exception("Self-tests failed!");
        }
        else
        {
            context.Information($"\nAll tests passed! ✓");
        }
    }

    private (int ExitCode, string Output) RunBuildCommand(string arguments, DirectoryPath outputDir)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = context.IsRunningOnWindows() ? "cmd.exe" : "/bin/bash",
            Arguments = context.IsRunningOnWindows() 
                ? $"/c \"{buildCmd}\" {arguments}" 
                : $"-c \"{buildCmd} {arguments}\"",
            WorkingDirectory = context.RootDirectory.FullPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Override artifacts directory
        startInfo.Environment["BDN_ARTIFACTS_DIR"] = outputDir.FullPath;

        using var process = Process.Start(startInfo);
        if (process == null)
            throw new Exception("Failed to start build process");

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (!string.IsNullOrEmpty(error))
            context.Warning($"Build stderr: {error}");

        return (process.ExitCode, output);
    }

    private string? ExtractVersionFromPackage(FilePath packagePath)
    {
        // Extract version from the filename (e.g., BenchmarkDotNet.0.15.0-develop.nupkg)
        var filename = packagePath.GetFilenameWithoutExtension().ToString();
        var match = Regex.Match(filename, @"\.(\d+\.\d+\.\d+(?:-[^.]+)?)$");
        
        if (match.Success)
            return match.Groups[1].Value;

        // If that fails, try to extract from the package metadata
        try
        {
            using var archive = ZipFile.OpenRead(packagePath.FullPath);
            var nuspecEntry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".nuspec"));
            
            if (nuspecEntry != null)
            {
                using var stream = nuspecEntry.Open();
                using var reader = new StreamReader(stream);
                var nuspecContent = reader.ReadToEnd();
                
                var versionMatch = Regex.Match(nuspecContent, @"<version>([^<]+)</version>");
                if (versionMatch.Success)
                    return versionMatch.Groups[1].Value;
            }
        }
        catch
        {
            // Ignore errors and return null
        }

        return null;
    }

    private class TestCase
    {
        public string Name { get; set; } = "";
        public string Arguments { get; set; } = "";
        public string ExpectedVersionPattern { get; set; } = "";
        public string Description { get; set; } = "";
    }
}