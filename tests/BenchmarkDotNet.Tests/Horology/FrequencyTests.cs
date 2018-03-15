﻿using System;
using System.Linq;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Portability;
using JetBrains.Annotations;
using Xunit;

namespace BenchmarkDotNet.Tests.Horology
{
    public class FrequencyTests
    {
        private static void Check(FrequencyUnit unit, Func<double, Frequency> fromMethod, Func<Frequency, double> toMethod)
        {
            int[] values = { 1, 42, 10000 };
            foreach (int value in values)
            {
                var timeInterval = new Frequency(value, unit);
                AreEqual(timeInterval, fromMethod(value));
                AreEqual(toMethod(timeInterval), value);
            }
        }

        [Fact]
        public void ConversionTest()
        {
            Check(FrequencyUnit.Hz, Frequency.FromHz, it => it.ToHz());
            Check(FrequencyUnit.KHz, Frequency.FromKHz, it => it.ToKHz());
            Check(FrequencyUnit.MHz, Frequency.FromMHz, it => it.ToMHz());
            Check(FrequencyUnit.GHz, Frequency.FromGHz, it => it.ToGHz());
        }

        [Fact]
        public void OperatorTest()
        {
            AreEqual(Frequency.KHz / Frequency.Hz, 1000);
            AreEqual((Frequency.KHz / 5.0).Hertz, 200);
            AreEqual((Frequency.KHz / 5).Hertz, 200);
            AreEqual((Frequency.KHz * 5.0).Hertz, 5000);
            AreEqual((Frequency.KHz * 5).Hertz, 5000);
            AreEqual((5.0 * Frequency.KHz).Hertz, 5000);
            AreEqual((5 * Frequency.KHz).Hertz, 5000);

            AreEqual(Frequency.Hz * 1000, Frequency.KHz);
            AreEqual(Frequency.KHz * 1000, Frequency.MHz);
            AreEqual(Frequency.MHz * 1000, Frequency.GHz);
        }

        [Fact]
        public void MetaTest()
        {
            foreach (var unit in FrequencyUnit.All)
            {
                Assert.True(unit.Name.ContainsWithIgnoreCase("hz"));
                Assert.True(unit.Description.ContainsWithIgnoreCase("hertz"));
                Assert.True(char.IsUpper(unit.Name.First()));
                Assert.True(char.IsUpper(unit.Description.First()));
            }
        }

        [AssertionMethod]
        private static void AreEqual(Frequency expected, Frequency actual) => AreEqual(expected.Hertz, actual.Hertz);

        [AssertionMethod]
        private static void AreEqual(double expected, double actual) => Assert.Equal(expected, actual, 5);
    }
}