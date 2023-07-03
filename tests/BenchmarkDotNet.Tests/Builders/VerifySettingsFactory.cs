using VerifyTests;

namespace BenchmarkDotNet.Tests.Builders
{
    public static class VerifySettingsFactory
    {
        public static VerifySettings Create()
        {
            var result = new VerifySettings();
            result.UseDirectory("VerifiedFiles");
            result.DisableDiff();
            return result;
        }
    }
}