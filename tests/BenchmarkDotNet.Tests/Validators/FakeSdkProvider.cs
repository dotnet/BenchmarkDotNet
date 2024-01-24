using BenchmarkDotNet.Validators;
using System.Collections.Generic;

namespace BenchmarkDotNet.Tests.Validators
{
    public class FakeSdkProvider : ISdkProvider
    {
        private readonly string[] installedSdks;

        public FakeSdkProvider(string[] installedSdks)
        {
            this.installedSdks = installedSdks;
        }

        public IEnumerable<string> GetInstalledSdks()
        {
            return installedSdks;
        }
    }
}