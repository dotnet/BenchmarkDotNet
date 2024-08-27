using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using Perfolizer.Helpers;
using Perfolizer.Phd.Dto;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Detectors.Cpu;

public static class TestHelper
{
    public static string ReadTestFile(string name)
    {
        var assembly = typeof(TestHelper).GetTypeInfo().Assembly;
        string resourceName = $"{typeof(TestHelper).Namespace}.TestFiles.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new FileNotFoundException($"Resource {resourceName} not found in {assembly.FullName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    [AssertionMethod]
    public static void AssertEqual(this ITestOutputHelper output, PhdCpu expected, PhdCpu actual)
    {
        output.WriteLine($"Expected : {expected.ToFullBrandName()}");
        output.WriteLine($"Actual   : {actual.ToFullBrandName()}");
        Assert.Equal(expected.ProcessorName, actual.ProcessorName);
        Assert.Equal(expected.PhysicalProcessorCount, actual.PhysicalProcessorCount);
        Assert.Equal(expected.PhysicalCoreCount, actual.PhysicalCoreCount);
        Assert.Equal(expected.LogicalCoreCount, actual.LogicalCoreCount);
        Assert.Equal(expected.NominalFrequency(), actual.NominalFrequency());
        Assert.Equal(expected.MaxFrequency(), actual.MaxFrequency());
    }
}