using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.TestAdapter;
using Xunit;

namespace BenchmarkDotNet.Tests.TestAdapter
{
    public class BenchmarkCaseExtensionsTests
    {
        [Fact]
        public void ToVsTestCase_WithoutVSAPPIDNAME_UsesDisplayNameAsFQN()
        {
            // Arrange
            var originalValue = Environment.GetEnvironmentVariable("VSAPPIDNAME");
            try
            {
                Environment.SetEnvironmentVariable("VSAPPIDNAME", null);
                var benchmarkCase = BenchmarkConverter.TypeToBenchmarks(typeof(SimpleBenchmark)).BenchmarksCases.Single();

                // Act
                var testCase = benchmarkCase.ToVsTestCase("test.dll", includeJobInName: true);

                // Assert
                // FQN should be the displayName which includes job info
                Assert.Equal(testCase.DisplayName, testCase.FullyQualifiedName);
            }
            finally
            {
                Environment.SetEnvironmentVariable("VSAPPIDNAME", originalValue);
            }
        }

        [Fact]
        public void ToVsTestCase_WithVSAPPIDNAME_UsesMethodFQNToAvoidHierarchySplit()
        {
            // Arrange
            var originalValue = Environment.GetEnvironmentVariable("VSAPPIDNAME");
            try
            {
                Environment.SetEnvironmentVariable("VSAPPIDNAME", "devenv.exe");
                var benchmarkCase = BenchmarkConverter.TypeToBenchmarks(typeof(SimpleBenchmark)).BenchmarksCases.Single();

                // Act
                var testCase = benchmarkCase.ToVsTestCase("test.dll", includeJobInName: true);

                // Assert
                // FQN should be the method FQN (without job info) to avoid hierarchy split
                // when job display name contains '.' (e.g., ".NET 8.0.6")
                var fullClassName = benchmarkCase.Descriptor.Type.GetCorrectCSharpTypeName(prefixWithGlobal: false);
                var expectedFQN = $"{fullClassName}.{nameof(SimpleBenchmark.Method)}";
                Assert.Equal(expectedFQN, testCase.FullyQualifiedName);
                // DisplayName should still include job info
                Assert.Contains("[", testCase.DisplayName);
            }
            finally
            {
                Environment.SetEnvironmentVariable("VSAPPIDNAME", originalValue);
            }
        }

        [Fact]
        public void ToVsTestCase_WithVSAPPIDNAME_FQNDoesNotContainDotFromJobName()
        {
            // Arrange - This test verifies the fix for issue #2793
            // Job display names like ".NET 8.0.6" contain dots that VS Test Explorer
            // interprets as namespace separators, causing incorrect hierarchy grouping
            var originalValue = Environment.GetEnvironmentVariable("VSAPPIDNAME");
            try
            {
                Environment.SetEnvironmentVariable("VSAPPIDNAME", "devenv.exe");
                var benchmarkCase = BenchmarkConverter.TypeToBenchmarks(typeof(SimpleBenchmark)).BenchmarksCases.Single();

                // Act
                var testCase = benchmarkCase.ToVsTestCase("test.dll", includeJobInName: true);

                // Assert
                // The FQN should only contain the class and method name,
                // not the job display info which may contain dots
                var fqnParts = testCase.FullyQualifiedName.Split('.');
                // Should end with ClassName.MethodName, not have extra parts from job name
                Assert.Equal(nameof(SimpleBenchmark), fqnParts[fqnParts.Length - 2]);
                Assert.Equal(nameof(SimpleBenchmark.Method), fqnParts[fqnParts.Length - 1]);
            }
            finally
            {
                Environment.SetEnvironmentVariable("VSAPPIDNAME", originalValue);
            }
        }

        public class SimpleBenchmark
        {
            [Benchmark]
            public void Method() { }
        }
    }
}
