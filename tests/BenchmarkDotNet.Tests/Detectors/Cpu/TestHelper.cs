using System.IO;
using System.Reflection;

namespace BenchmarkDotNet.Tests.Detectors.Cpu;

public static class TestHelper
{
    public static string ReadTestFile(string name)
    {
        var assembly = typeof(TestHelper).GetTypeInfo().Assembly;
        string resourceName = $"{typeof(TestHelper).Namespace}.TestFiles.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return "";

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}