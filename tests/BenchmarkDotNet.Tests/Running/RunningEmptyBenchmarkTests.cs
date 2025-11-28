using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Xunit;
using System.Reflection;
using System.Reflection.Emit;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Tests.Running
{
    public class RunningEmptyBenchmarkTests
    {
        #region BenchmarkRunner Methods Overview
        /*
         * Available BenchmarkRunner.Run methods:
         * 1. Generic Type:
         *    - BenchmarkRunner.Run<T>(IConfig? config = null, string[]? args = null)
         * 2. Type-based:
         *    - BenchmarkRunner.Run(Type type, IConfig? config = null, string[]? args = null)
         *    - BenchmarkRunner.Run(Type[] types, IConfig? config = null, string[]? args = null)
         *    - BenchmarkRunner.Run(Type type, MethodInfo[] methods, IConfig? config = null)
         * 3. Assembly-based:
         *    - BenchmarkRunner.Run(Assembly assembly, IConfig? config = null, string[]? args = null)
         * 4. BenchmarkRunInfo-based:
         *    - BenchmarkRunner.Run(BenchmarkRunInfo benchmarkRunInfo)
         *    - BenchmarkRunner.Run(BenchmarkRunInfo[] benchmarkRunInfos)
         * 5. Deprecated methods:
         *    - BenchmarkRunner.RunUrl(string url, IConfig? config = null)
         *    - BenchmarkRunner.RunSource(string source, IConfig? config = null)
         */
        #endregion
        #region Generic Type Tests


#pragma warning disable BDN1000
        /// <summary>
        /// Tests for <see cref="BenchmarkRunner.Run{T}"/> method
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData(new object[] { new string[] { " " } })]
        public void GenericTypeWithoutBenchmarkAttribute_ThrowsValidationError_WhenNoBenchmarkAttribute(string[]? args)
        {
            GetConfigWithLogger(out var logger, out var config);

            var summary = BenchmarkRunner.Run<EmptyBenchmark>(config, args);

            if (args == null)
            {
                Assert.True(summary.HasCriticalValidationErrors);
                Assert.Contains(summary.ValidationErrors, validationError => validationError.Message == GetValidationErrorForType(typeof(EmptyBenchmark)));
                Assert.Contains(GetValidationErrorForType(typeof(EmptyBenchmark)), logger.GetLog());
            }
            else
            {
                // When args is provided and type is invalid, we get a ValidationFailed summary
                // instead of an unhandled exception
                Assert.NotNull(summary);
            }
        }
#pragma warning restore BDN1000

        [Theory]
        [InlineData(null)]
        [InlineData(new object[] { new string[] { " " } })]
        public void GenericTypeWithBenchmarkAttribute_RunsSuccessfully(string[]? args)
        {
            GetConfigWithLogger(out var logger, out var config);

            var summary = BenchmarkRunner.Run<NotEmptyBenchmark>(config, args);
            Assert.False(summary.HasCriticalValidationErrors);
            Assert.DoesNotContain(summary.ValidationErrors, validationError => validationError.Message == GetValidationErrorForType(typeof(NotEmptyBenchmark)));
            Assert.DoesNotContain(GetValidationErrorForType(typeof(NotEmptyBenchmark)), logger.GetLog());
        }
        #endregion
        #region Type-based Tests

#pragma warning disable BDN1000
        /// <summary>
        /// Tests for BenchmarkRunner.Run(Type) method
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData(new object[] { new string[] { " " } })]
        public void TypeWithoutBenchmarkAttribute_ThrowsValidationError_WhenNoBenchmarkAttribute(string[]? args)
        {
            GetConfigWithLogger(out var logger, out var config);

            var summary = BenchmarkRunner.Run(typeof(EmptyBenchmark), config, args);

            if (args == null)
            {
                Assert.True(summary.HasCriticalValidationErrors);
                Assert.Contains(summary.ValidationErrors, validationError => validationError.Message == GetValidationErrorForType(typeof(EmptyBenchmark)));
                Assert.Contains(GetValidationErrorForType(typeof(EmptyBenchmark)), logger.GetLog());
            }
            else
            {
                // When args is provided and type is invalid, we get a ValidationFailed summary
                // instead of an unhandled exception
                Assert.NotNull(summary);
            }
        }
#pragma warning restore BDN1000

        [Theory]
        [InlineData(null)]
        [InlineData(new object[] { new string[] { " " } })]
        public void TypeWithBenchmarkAttribute_RunsSuccessfully(string[]? args)
        {
            GetConfigWithLogger(out var logger, out var config);

            var summaries = BenchmarkRunner.Run(typeof(NotEmptyBenchmark), config, args);
            Assert.False(summaries.HasCriticalValidationErrors);
            Assert.DoesNotContain(summaries.ValidationErrors, validationError => validationError.Message == GetValidationErrorForType(typeof(NotEmptyBenchmark)));
            Assert.DoesNotContain(GetValidationErrorForType(typeof(NotEmptyBenchmark)), logger.GetLog());
        }

        /// <summary>
        /// Tests for BenchmarkRunner.Run(Type[]) method
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData(new object[] { new string[] { " " } })]
        public void TypesWithoutBenchmarkAttribute_ThrowsValidationError_WhenNoBenchmarkAttribute(string[]? args)
        {
            GetConfigWithLogger(out var logger, out var config);

            var summaries = BenchmarkRunner.Run(new[] { typeof(EmptyBenchmark), typeof(EmptyBenchmark2) }, config, args);
            if (args != null)
            {
                Assert.Contains(GetValidationErrorForType(typeof(EmptyBenchmark)), logger.GetLog());
                Assert.Contains(GetValidationErrorForType(typeof(EmptyBenchmark2)), logger.GetLog());
            }
            else
            {
                var summary = summaries[0];
                Assert.True(summary.HasCriticalValidationErrors);
                Assert.Contains(summary.ValidationErrors, validationError => validationError.Message == GetValidationErrorForType(typeof(EmptyBenchmark)));
                Assert.Contains(summary.ValidationErrors, validationError => validationError.Message == GetValidationErrorForType(typeof(EmptyBenchmark2)));
                Assert.Contains(GetValidationErrorForType(typeof(EmptyBenchmark)), logger.GetLog());
                Assert.Contains(GetValidationErrorForType(typeof(EmptyBenchmark2)), logger.GetLog());
            }


        }

        [Theory]
        [InlineData(null)]
        [InlineData(new object[] { new string[] { " " } })]
        public void TypesWithBenchmarkAttribute_RunsSuccessfully(string[]? args)
        {
            GetConfigWithLogger(out var logger, out var config);

            var summaries = BenchmarkRunner.Run(new[] { typeof(NotEmptyBenchmark) }, config, args);
            var summary = summaries[0];
            Assert.False(summary.HasCriticalValidationErrors);
            Assert.DoesNotContain(summary.ValidationErrors, validationError => validationError.Message == GetValidationErrorForType(typeof(NotEmptyBenchmark)));
            Assert.DoesNotContain(GetValidationErrorForType(typeof(NotEmptyBenchmark)), logger.GetLog());
        }
        #endregion
        #region BenchmarkRunInfo Tests
        /// <summary>
        /// Tests for BenchmarkRunner.Run(BenchmarkRunInfo) method
        /// </summary>
        [Fact]
        public void BenchmarkRunInfoWithoutBenchmarkAttribute_ThrowsValidationError_WhenNoBenchmarkAttribute()
        {
            GetConfigWithLogger(out var logger, out var config);

            var summary = BenchmarkRunner.Run(BenchmarkConverter.TypeToBenchmarks(typeof(EmptyBenchmark), config));
            Assert.True(summary.HasCriticalValidationErrors);
            Assert.Contains(summary.ValidationErrors, validationError => validationError.Message == GetValidationErrorForType(typeof(EmptyBenchmark)));
            Assert.Contains(GetValidationErrorForType(typeof(EmptyBenchmark)), logger.GetLog());
        }

        [Fact]
        public void BenchmarkRunInfoWithBenchmarkAttribute_RunsSuccessfully()
        {
            GetConfigWithLogger(out var logger, out var config);

            var summary = BenchmarkRunner.Run(BenchmarkConverter.TypeToBenchmarks(typeof(NotEmptyBenchmark), config));
            Assert.False(summary.HasCriticalValidationErrors);
            Assert.DoesNotContain(summary.ValidationErrors, validationError => validationError.Message == GetValidationErrorForType(typeof(EmptyBenchmark)));
            Assert.DoesNotContain(GetValidationErrorForType(typeof(NotEmptyBenchmark)), logger.GetLog());
        }

        /// <summary>
        /// Tests for BenchmarkRunner.Run(BenchmarkRunInfo[]) method
        /// </summary>
        [Fact]
        public void BenchmarkRunInfosWithoutBenchmarkAttribute_ThrowsValidationError_WhenNoBenchmarkAttribute()
        {
            GetConfigWithLogger(out var logger, out var config);

            var summaries = BenchmarkRunner.Run(new[] {
                BenchmarkConverter.TypeToBenchmarks(typeof(EmptyBenchmark), config),
                BenchmarkConverter.TypeToBenchmarks(typeof(EmptyBenchmark2), config)
            });
            var summary = summaries[0];
            Assert.True(summary.HasCriticalValidationErrors);
            Assert.Contains(summary.ValidationErrors, validationError => validationError.Message == GetValidationErrorForType(typeof(EmptyBenchmark)));
            Assert.Contains(summary.ValidationErrors, validationError => validationError.Message == GetValidationErrorForType(typeof(EmptyBenchmark2)));
            Assert.Contains(GetValidationErrorForType(typeof(EmptyBenchmark)), logger.GetLog());
            Assert.Contains(GetValidationErrorForType(typeof(EmptyBenchmark2)), logger.GetLog());
        }

        [Fact]
        public void BenchmarkRunInfosWithBenchmarkAttribute_RunsSuccessfully()
        {
            GetConfigWithLogger(out var logger, out var config);

            var summaries = BenchmarkRunner.Run(new[] { BenchmarkConverter.TypeToBenchmarks(typeof(NotEmptyBenchmark), config) });
            var summary = summaries[0];
            Assert.False(summary.HasCriticalValidationErrors);
            Assert.DoesNotContain(summary.ValidationErrors, validationError => validationError.Message == GetValidationErrorForType(typeof(NotEmptyBenchmark)));
            Assert.DoesNotContain(GetValidationErrorForType(typeof(NotEmptyBenchmark)), logger.GetLog());
        }
        #endregion
        #region Mixed Types Tests

        [Theory]
        [InlineData(null)]
        [InlineData(new object[] { new string[] { " " } })]
        public void MixedTypes_ThrowsValidationError_WhenNoBenchmarkAttribute(string[]? args)
        {
            GetConfigWithLogger(out var logger, out var config);

            var summaries = BenchmarkRunner.Run(new[] { typeof(EmptyBenchmark), typeof(NotEmptyBenchmark) }, config, args);
            if (args != null)
            {
                Assert.Contains(GetExpandedValidationErrorForType(typeof(EmptyBenchmark)), logger.GetLog());
            }
            else
            {
                var summary = summaries[0];
                Assert.True(summary.HasCriticalValidationErrors);
                Assert.Contains(summary.ValidationErrors, validationError => validationError.Message == GetValidationErrorForType(typeof(EmptyBenchmark)));
                Assert.Contains(GetValidationErrorForType(typeof(EmptyBenchmark)), logger.GetLog());
            }
        }
        #endregion
        #region Assembly Tests
        // In this tests there is no config and logger because the logger is initiated at CreateCompositeLogger when the BenchmarkRunInfo[] is empty
        // those cannot be inserted using config
        [Theory]

        [InlineData(null)]
        [InlineData(new object[] { new string[] { " " } })]
        public void AssemblyWithoutBenchmarks_ThrowsValidationError_WhenNoBenchmarksFound(string[]? args)
        {

            // Create a mock assembly with no benchmark types
            var assemblyName = new AssemblyName("MockAssembly");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("MockModule");
            // Create a simple type in the assembly (no benchmarks)
            var typeBuilder = moduleBuilder.DefineType("MockType", TypeAttributes.Public);
            typeBuilder.CreateType();

            Summary[] summaries = null;
            if (args != null)
            {
                GetConfigWithLogger(out var logger, out var config);
                summaries = BenchmarkRunner.Run(assemblyBuilder, config, args);
                Assert.Contains(GetAssemblylValidationError(assemblyBuilder), logger.GetLog());
            }
            else
            {
                summaries = BenchmarkRunner.Run(assemblyBuilder, null, args);
                var summary = summaries[0];
                Assert.True(summary.HasCriticalValidationErrors);
                Assert.Contains(summary.ValidationErrors, validationError => validationError.Message == GetGeneralValidationError());
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData(new object[] { new string[] { " " } })]
        public void AssemblyWithBenchmarks_RunsSuccessfully_WhenBenchmarkAttributePresent(string[]? args)
        {
            // Skip test on .NET Framework 4.6.2
            if (RuntimeInformation.FrameworkDescription.Contains(".NET Framework 4"))
                return;

            // Create a mock assembly with benchmark types
            var assemblyName = new AssemblyName("MockAssemblyWithBenchmarks");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("MockModule");

            // Create a benchmark type
            var benchmarkTypeBuilder = moduleBuilder.DefineType("MockBenchmark", TypeAttributes.Public);
            var benchmarkMethod = benchmarkTypeBuilder.DefineMethod("Benchmark", MethodAttributes.Public, typeof(void), Type.EmptyTypes);

            // Generate method body
            var ilGenerator = benchmarkMethod.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret); // Just return from the method

            var benchmarkAttributeCtor = typeof(BenchmarkAttribute).GetConstructor(new[] { typeof(int), typeof(string) });
            if (benchmarkAttributeCtor == null)
                throw new InvalidOperationException("Could not find BenchmarkAttribute constructor");
            benchmarkMethod.SetCustomAttribute(new CustomAttributeBuilder(
                benchmarkAttributeCtor,
                new object[] { 0, "" }));
            benchmarkTypeBuilder.CreateType();

            Summary[] summaries = null;
            if (args != null)
            {
                GetConfigWithLogger(out var logger, out var config);
                summaries = BenchmarkRunner.Run(assemblyBuilder, config, args);
                Assert.DoesNotContain(GetAssemblylValidationError(assemblyBuilder), logger.GetLog());
            }
            else
            {
                summaries = BenchmarkRunner.Run(assemblyBuilder);
                var summary = summaries[0];
                Assert.False(summary.HasCriticalValidationErrors);
                Assert.DoesNotContain(summary.ValidationErrors, validationError => validationError.Message == GetGeneralValidationError());
            }
        }
        #endregion
        #region Helper Methods
        private string GetValidationErrorForType(Type type)
        {
            return $"No [Benchmark] attribute found on '{type.Name}' benchmark case.";
        }

        private string GetAssemblylValidationError(Assembly assembly)
        {
            return $"No [Benchmark] attribute found on '{assembly.GetName().Name}' assembly.";
        }

        private string GetExpandedValidationErrorForType(Type type)
        {
            return $"Type {type} is invalid. Only public, non-generic (closed generic types with public parameterless ctors are supported), non-abstract, non-sealed, non-static types with public instance [Benchmark] method(s) are supported.";
        }

        private string GetGeneralValidationError()
        {
            return $"No benchmarks were found.";
        }

        private void GetConfigWithLogger(out AccumulationLogger logger, out ManualConfig manualConfig)
        {
            logger = new AccumulationLogger();
            manualConfig = ManualConfig.CreateEmpty()
                .AddLogger(logger)
                .AddColumnProvider(DefaultColumnProviders.Instance);
        }

        #endregion
        #region Test Classes
        public class EmptyBenchmark
        {
        }

        public class EmptyBenchmark2
        {
        }

        [SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 1, invocationCount: 1, id: "QuickJob")]
        public class NotEmptyBenchmark
        {
            [Benchmark]
            public void Benchmark()
            {
                var sum = 0;
                for (int i = 0; i < 1; i++)
                {
                    sum += i;
                }
            }
        }
        #endregion
    }
}