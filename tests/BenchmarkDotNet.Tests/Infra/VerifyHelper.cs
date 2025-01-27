using VerifyTests;

namespace BenchmarkDotNet.Tests.Infra;

public static class VerifyHelper
{
    public static VerifySettings Create(string? typeName = null)
    {
        var result = new VerifySettings();
        result.UseDirectory("VerifiedFiles");
        result.DisableDiff();
        if (typeName != null)
            result.UseTypeName(typeName);
        return result;
    }
}