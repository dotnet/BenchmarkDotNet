using System;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class ServicesProviderContractTests
    {
        [Fact]
        public void AllMandatoryServicesAreRegistered()
        {
            Assert.NotNull(ServicesProvider.Resolve<RuntimeInformation>());
            Assert.NotNull(ServicesProvider.Resolve<IDiagnosersLoader>());
            Assert.NotNull(ServicesProvider.Resolve<IResourcesService>());
            Assert.NotNull(ServicesProvider.Resolve<Func<ILogger, IDisposable>>());
            Assert.NotNull(ServicesProvider.Resolve<IDotNetStandardWorkarounds>());
            Assert.NotNull(ServicesProvider.Resolve<IBenchmarkConverter>());
        }
    }
}