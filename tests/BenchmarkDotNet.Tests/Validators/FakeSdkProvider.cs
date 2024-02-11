using BenchmarkDotNet.Validators;
using System.Collections.Generic;

namespace BenchmarkDotNet.Tests.Validators
{
    public class FakeSdkProvider : ISdkProvider
    {
        private readonly string[] installedSdks;
        public string CustomDotNetCliPath { get; set; }

        public FakeSdkProvider(string[] installedSdks, string customDotNetCliPath = "")
        {
            this.installedSdks = installedSdks;
            this.CustomDotNetCliPath = customDotNetCliPath;
        }

        public IEnumerable<string> GetInstalledSdks()
        {
            if (!string.IsNullOrEmpty(CustomDotNetCliPath))
            {
                return installedSdks;
            }

            return installedSdks;
        }
    }
}