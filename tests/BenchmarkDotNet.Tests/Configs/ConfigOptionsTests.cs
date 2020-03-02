using BenchmarkDotNet.Configs;
using Xunit;

namespace BenchmarkDotNet.Tests.Configs
{
    public class ConfigOptionsTests
    {
        [Fact]
        public void DefaultIsTheDefaultOption() => Assert.Equal(ConfigOptions.Default, default(ConfigOptions));

        [Fact]
        public void DefaultConfigDoesNotKeepFiles() => Assert.False(DefaultConfig.Instance.Options.HasFlag(ConfigOptions.KeepBenchmarkFiles));

        [Fact]
        public void DefaultConfigDoesNotJoinSummaries() => Assert.False(DefaultConfig.Instance.Options.HasFlag(ConfigOptions.JoinSummary));

        [Fact]
        public void DefaultConfigDoesNotStopOnFirstError() => Assert.False(DefaultConfig.Instance.Options.HasFlag(ConfigOptions.StopOnFirstError));

        [Fact]
        public void DefaultConfigDoesNotDisableLogFile() => Assert.False(DefaultConfig.Instance.Options.HasFlag(ConfigOptions.DisableLogFile));

        [Fact]
        public void DefaultConfigDoesDisableOptimizationsValidator() => Assert.False(DefaultConfig.Instance.Options.HasFlag(ConfigOptions.DisableOptimizationsValidator));

        [Fact]
        public void EmptyConfigDoesDisableOptimizationsValidator() => Assert.False(ManualConfig.CreateEmpty().Options.HasFlag(ConfigOptions.DisableOptimizationsValidator));

        [Fact]
        public void ConfigFlagCanBeEnabledOrDisabled()
        {
            var flag = ConfigOptions.Default;

            flag = flag.Set(true, ConfigOptions.StopOnFirstError);
            Assert.True(flag.IsSet(ConfigOptions.StopOnFirstError));
            Assert.True(flag.HasFlag(ConfigOptions.StopOnFirstError));

            flag = flag.Set(false, ConfigOptions.StopOnFirstError);
            Assert.False(flag.IsSet(ConfigOptions.StopOnFirstError));
            Assert.False(flag.HasFlag(ConfigOptions.StopOnFirstError));
        }

        [Fact]
        public void ConfigFlagsCanBeCombined()
        {
            var flag = ConfigOptions.Default;

            flag = flag.Set(true, ConfigOptions.StopOnFirstError);
            flag = flag.Set(true, ConfigOptions.JoinSummary);

            Assert.True(flag.IsSet(ConfigOptions.StopOnFirstError));
            Assert.True(flag.HasFlag(ConfigOptions.StopOnFirstError));
            Assert.True(flag.IsSet(ConfigOptions.JoinSummary));
            Assert.True(flag.HasFlag(ConfigOptions.JoinSummary));

            flag = flag.Set(false, ConfigOptions.StopOnFirstError);
            Assert.False(flag.IsSet(ConfigOptions.StopOnFirstError));
            Assert.False(flag.HasFlag(ConfigOptions.StopOnFirstError));
            Assert.True(flag.IsSet(ConfigOptions.JoinSummary));
            Assert.True(flag.HasFlag(ConfigOptions.JoinSummary));

            flag = flag.Set(false, ConfigOptions.JoinSummary);
            Assert.False(flag.IsSet(ConfigOptions.StopOnFirstError));
            Assert.False(flag.HasFlag(ConfigOptions.StopOnFirstError));
            Assert.False(flag.IsSet(ConfigOptions.JoinSummary));
            Assert.False(flag.HasFlag(ConfigOptions.JoinSummary));
        }

        [Fact]
        public void ConfigFlagCanBeEnabledOrDisabledUsedManualConfigMethods()
        {
            var config = ManualConfig.Create(DefaultConfig.Instance);

            config.WithOption(ConfigOptions.StopOnFirstError, true);
            Assert.True(config.Options.IsSet(ConfigOptions.StopOnFirstError));
            Assert.True(config.Options.HasFlag(ConfigOptions.StopOnFirstError));

            config.WithOption(ConfigOptions.StopOnFirstError, false);
            Assert.False(config.Options.IsSet(ConfigOptions.StopOnFirstError));
            Assert.False(config.Options.HasFlag(ConfigOptions.StopOnFirstError));

            config.WithOptions(ConfigOptions.StopOnFirstError);
            Assert.True(config.Options.IsSet(ConfigOptions.StopOnFirstError));
            Assert.True(config.Options.HasFlag(ConfigOptions.StopOnFirstError));
        }
    }
}