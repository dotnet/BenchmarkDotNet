using System;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using Xunit;

namespace BenchmarkDotNet.Tests.Configs
{
    public class CustomCounterTests
    {
        #region CustomCounter Class Tests

        [Fact]
        public void ProfileSourceNameIsSetCorrectly()
        {
            var counter = new CustomCounter("DCMiss");

            Assert.Equal("DCMiss", counter.ProfileSourceName);
            Assert.Equal("DCMiss", counter.ShortName); // ShortName defaults to ProfileSourceName
            Assert.Equal(CustomCounter.DefaultInterval, counter.Interval);
            Assert.False(counter.HigherIsBetter);
        }

        [Fact]
        public void ShortNameIsSetWhenProvided()
        {
            var counter = new CustomCounter("DCMiss", shortName: "L1D$Miss");

            Assert.Equal("DCMiss", counter.ProfileSourceName);
            Assert.Equal("L1D$Miss", counter.ShortName);
        }

        [Fact]
        public void IntervalIsSetWhenProvided()
        {
            var counter = new CustomCounter("DCMiss", interval: 500_000);

            Assert.Equal(500_000, counter.Interval);
        }

        [Fact]
        public void HigherIsBetterIsSetWhenProvided()
        {
            var counter = new CustomCounter("BranchInstructions", higherIsBetter: true);

            Assert.True(counter.HigherIsBetter);
        }

        [Fact]
        public void AllPropertiesAreSetWhenProvided()
        {
            var counter = new CustomCounter(
                "BranchMispredictions",
                shortName: "BrMisp",
                interval: 100_000,
                higherIsBetter: false);

            Assert.Equal("BranchMispredictions", counter.ProfileSourceName);
            Assert.Equal("BrMisp", counter.ShortName);
            Assert.Equal(100_000, counter.Interval);
            Assert.False(counter.HigherIsBetter);
        }

        [Fact]
        public void NullProfileSourceNameThrows()
        {
            Assert.Throws<ArgumentException>(() => new CustomCounter(null!));
        }

        [Fact]
        public void EmptyProfileSourceNameThrows()
        {
            Assert.Throws<ArgumentException>(() => new CustomCounter(""));
        }

        [Fact]
        public void WhitespaceProfileSourceNameThrows()
        {
            Assert.Throws<ArgumentException>(() => new CustomCounter("   "));
        }

        [Fact]
        public void CountersWithSameProfileSourceNameAreEqual()
        {
            var counter1 = new CustomCounter("DCMiss", shortName: "L1D$Miss");
            var counter2 = new CustomCounter("DCMiss", shortName: "DifferentName");

            Assert.Equal(counter1, counter2);
            Assert.Equal(counter1.GetHashCode(), counter2.GetHashCode());
        }

        [Fact]
        public void CountersWithDifferentProfileSourceNameAreNotEqual()
        {
            var counter1 = new CustomCounter("DCMiss");
            var counter2 = new CustomCounter("ICMiss");

            Assert.NotEqual(counter1, counter2);
        }

        [Fact]
        public void SpecialCharactersInNameAreAllowed()
        {
            var counter = new CustomCounter("L1-D$-Cache_Misses");

            Assert.Equal("L1-D$-Cache_Misses", counter.ProfileSourceName);
        }

        [Fact]
        public void VeryLargeIntervalIsAccepted()
        {
            var counter = new CustomCounter("DCMiss", interval: int.MaxValue);

            Assert.Equal(int.MaxValue, counter.Interval);
        }

        [Fact]
        public void ZeroIntervalIsAccepted()
        {
            // Zero interval might be invalid for actual profiling but should be accepted by the class
            var counter = new CustomCounter("DCMiss", interval: 0);

            Assert.Equal(0, counter.Interval);
        }

        #endregion

        #region ManualConfig.AddCustomCounters Tests

        [Fact]
        public void SingleCounterCanBeAdded()
        {
            var config = ManualConfig.CreateEmpty();

            config.AddCustomCounters(new CustomCounter("DCMiss"));

            Assert.Single(config.GetCustomCounters());
            Assert.Equal("DCMiss", config.GetCustomCounters().Single().ProfileSourceName);
        }

        [Fact]
        public void MultipleCountersCanBeAdded()
        {
            var config = ManualConfig.CreateEmpty();

            config.AddCustomCounters(
                new CustomCounter("DCMiss"),
                new CustomCounter("ICMiss"),
                new CustomCounter("BranchMispredictions"));

            Assert.Equal(3, config.GetCustomCounters().Count());
        }

        [Fact]
        public void DuplicateCustomCountersAreExcluded()
        {
            var config = ManualConfig.CreateEmpty();

            config.AddCustomCounters(new CustomCounter("DCMiss"));
            config.AddCustomCounters(new CustomCounter("DCMiss", shortName: "Different"));

            Assert.Single(config.GetCustomCounters());
        }

        [Fact]
        public void AddCustomCountersReturnsSameInstance()
        {
            var config = ManualConfig.CreateEmpty();

            var result = config.AddCustomCounters(new CustomCounter("DCMiss"));

            Assert.Same(config, result);
        }

        [Fact]
        public void EmptyArrayHasNoEffect()
        {
            var config = ManualConfig.CreateEmpty();

            config.AddCustomCounters();

            Assert.Empty(config.GetCustomCounters());
        }

        #endregion

        #region ImmutableConfig Custom Counters Tests

        [Fact]
        public void CustomCountersArePreservedInImmutableConfig()
        {
            var mutable = ManualConfig.CreateEmpty();
            mutable.AddCustomCounters(
                new CustomCounter("DCMiss", shortName: "L1D$Miss"),
                new CustomCounter("ICMiss", shortName: "L1I$Miss"));

            var immutable = ImmutableConfigBuilder.Create(mutable);

            Assert.Equal(2, immutable.GetCustomCounters().Count());
            Assert.Contains(immutable.GetCustomCounters(), c => c.ProfileSourceName == "DCMiss");
            Assert.Contains(immutable.GetCustomCounters(), c => c.ProfileSourceName == "ICMiss");
        }

        [Fact]
        public void DuplicateCustomCountersAreExcludedInImmutableConfig()
        {
            var mutable = ManualConfig.CreateEmpty();
            mutable.AddCustomCounters(new CustomCounter("DCMiss"));
            mutable.AddCustomCounters(new CustomCounter("DCMiss"));

            var immutable = ImmutableConfigBuilder.Create(mutable);

            Assert.Single(immutable.GetCustomCounters());
        }

        [Fact]
        public void CustomCounterPropertiesArePreservedInImmutableConfig()
        {
            var mutable = ManualConfig.CreateEmpty();
            mutable.AddCustomCounters(new CustomCounter(
                "BranchMispredictions",
                shortName: "BrMisp",
                interval: 500_000,
                higherIsBetter: false));

            var immutable = ImmutableConfigBuilder.Create(mutable);
            var counter = immutable.GetCustomCounters().Single();

            Assert.Equal("BranchMispredictions", counter.ProfileSourceName);
            Assert.Equal("BrMisp", counter.ShortName);
            Assert.Equal(500_000, counter.Interval);
            Assert.False(counter.HigherIsBetter);
        }

        #endregion

        #region Config Merging Tests

        [Fact]
        public void CustomCountersAreCombinedWhenMergingConfigs()
        {
            var config1 = ManualConfig.CreateEmpty();
            config1.AddCustomCounters(new CustomCounter("DCMiss"));

            var config2 = ManualConfig.CreateEmpty();
            config2.AddCustomCounters(new CustomCounter("ICMiss"));

            config1.Add(config2);

            Assert.Equal(2, config1.GetCustomCounters().Count());
            Assert.Contains(config1.GetCustomCounters(), c => c.ProfileSourceName == "DCMiss");
            Assert.Contains(config1.GetCustomCounters(), c => c.ProfileSourceName == "ICMiss");
        }

        [Fact]
        public void DuplicateCustomCountersAreExcludedWhenMergingConfigs()
        {
            var config1 = ManualConfig.CreateEmpty();
            config1.AddCustomCounters(new CustomCounter("DCMiss"));

            var config2 = ManualConfig.CreateEmpty();
            config2.AddCustomCounters(new CustomCounter("DCMiss"));

            config1.Add(config2);

            Assert.Single(config1.GetCustomCounters());
        }

        [Fact]
        public void OriginalCountersArePreservedWhenMergingEmptyConfig()
        {
            var config1 = ManualConfig.CreateEmpty();
            config1.AddCustomCounters(
                new CustomCounter("DCMiss"),
                new CustomCounter("ICMiss"));

            var config2 = ManualConfig.CreateEmpty();

            config1.Add(config2);

            Assert.Equal(2, config1.GetCustomCounters().Count());
        }

        #endregion

        #region DefaultConfig Tests

        [Fact]
        public void DefaultConfigHasNoCustomCounters()
        {
            var defaultConfig = DefaultConfig.Instance;

            Assert.Empty(defaultConfig.GetCustomCounters());
        }

        #endregion

        #region Combined Hardware and Custom Counters Tests

        [Fact]
        public void BothHardwareAndCustomCountersCanBeConfigured()
        {
            var config = ManualConfig.CreateEmpty();
            config.AddHardwareCounters(HardwareCounter.CacheMisses);
            config.AddCustomCounters(new CustomCounter("DCMiss"));

            Assert.Single(config.GetHardwareCounters());
            Assert.Single(config.GetCustomCounters());
        }

        [Fact]
        public void BothHardwareAndCustomCountersArePreservedInImmutableConfig()
        {
            var mutable = ManualConfig.CreateEmpty();
            mutable.AddHardwareCounters(HardwareCounter.CacheMisses);
            mutable.AddCustomCounters(new CustomCounter("DCMiss"));

            var immutable = ImmutableConfigBuilder.Create(mutable);

            Assert.Single(immutable.GetHardwareCounters());
            Assert.Single(immutable.GetCustomCounters());
        }

        #endregion
    }
}
