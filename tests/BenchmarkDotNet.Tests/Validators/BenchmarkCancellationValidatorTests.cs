using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Xunit;

namespace BenchmarkDotNet.Tests.Validators
{
    public class BenchmarkCancellationValidatorTests
    {
        private static async Task<ValidationError[]> Validate<T>()
            => await BenchmarkCancellationValidator.FailOnError
                .ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(T)))
                .ToArrayAsync();

        [Fact]
        public async Task ValidProperty_NoErrors()
        {
            var errors = await Validate<ValidProperty>();
            Assert.Empty(errors);
        }

        public class ValidProperty
        {
            [BenchmarkCancellation]
            public CancellationToken CancellationToken { get; set; }

            [Benchmark]
            public void Work() { }
        }

        [Fact]
        public async Task ValidField_NoErrors()
        {
            var errors = await Validate<ValidField>();
            Assert.Empty(errors);
        }

        public class ValidField
        {
            [BenchmarkCancellation]
            public CancellationToken CancellationToken;

            [Benchmark]
            public void Work() { }
        }

        [Fact]
        public async Task ValidStaticProperty_NoErrors()
        {
            var errors = await Validate<ValidStaticProperty>();
            Assert.Empty(errors);
        }

        public class ValidStaticProperty
        {
            [BenchmarkCancellation]
            public static CancellationToken CancellationToken { get; set; }

            [Benchmark]
            public void Work() { }
        }

        [Fact]
        public async Task ValidStaticField_NoErrors()
        {
            var errors = await Validate<ValidStaticField>();
            Assert.Empty(errors);
        }

        public class ValidStaticField
        {
            [BenchmarkCancellation]
            public static CancellationToken CancellationToken;

            [Benchmark]
            public void Work() { }
        }

        [Fact]
        public async Task PropertyWithWrongType_ReportsError()
        {
            var errors = await Validate<PropertyWrongType>();
            Assert.Single(errors);
            Assert.Contains("Only CancellationToken is supported", errors[0].Message);
        }

        public class PropertyWrongType
        {
#pragma warning disable BDN1600
            [BenchmarkCancellation]
            public int CancellationToken { get; set; }
#pragma warning restore BDN1600

            [Benchmark]
            public void Work() { }
        }

        [Fact]
        public async Task FieldWithWrongType_ReportsError()
        {
            var errors = await Validate<FieldWrongType>();
            Assert.Single(errors);
            Assert.Contains("Only CancellationToken is supported", errors[0].Message);
        }

        public class FieldWrongType
        {
#pragma warning disable CS8618, BDN1600
            [BenchmarkCancellation]
            public string FieldName;
#pragma warning restore CS8618, BDN1600

            [Benchmark]
            public void Work() { }
        }

        [Fact]
        public async Task ReadonlyField_ReportsError()
        {
            var errors = await Validate<ReadonlyFieldType>();
            Assert.Single(errors);
            Assert.Contains("readonly", errors[0].Message);
        }

        public class ReadonlyFieldType
        {
#pragma warning disable BDN1603
            [BenchmarkCancellation]
            public readonly CancellationToken CancellationToken;
#pragma warning restore BDN1603

            [Benchmark]
            public void Work() { }
        }

        [Fact]
        public async Task NonPublicField_ReportsError()
        {
            var errors = await Validate<NonPublicFieldType>();
            Assert.Single(errors);
            Assert.Contains("not public", errors[0].Message);
        }

        public class NonPublicFieldType
        {
#pragma warning disable CS0649, BDN1601
            [BenchmarkCancellation]
            internal CancellationToken CancellationToken;
#pragma warning restore CS0649, BDN1601

            [Benchmark]
            public void Work() { }
        }

        [Fact]
        public async Task NonPublicProperty_ReportsError()
        {
            var errors = await Validate<NonPublicPropertyType>();
            Assert.Single(errors);
            Assert.Contains("not public", errors[0].Message);
        }

        public class NonPublicPropertyType
        {
#pragma warning disable BDN1602, BDN1604
            [BenchmarkCancellation]
            private CancellationToken CancellationToken { get; set; }
#pragma warning restore BDN1602, BDN1604

            [Benchmark]
            public void Work() { }
        }

        [Fact]
        public async Task PropertyWithNoSetter_ReportsError()
        {
            var errors = await Validate<PropertyNoSetter>();
            Assert.Single(errors);
            Assert.Contains("no setter", errors[0].Message);
        }

        public class PropertyNoSetter
        {
            [BenchmarkCancellation]
#pragma warning disable BDN1604
            public CancellationToken CancellationToken { get; }
#pragma warning restore BDN1604

            [Benchmark]
            public void Work() { }
        }

        [Fact]
        public async Task PropertyWithPrivateSetter_ReportsError()
        {
            var errors = await Validate<PropertyPrivateSetter>();
            Assert.Single(errors);
            Assert.Contains("setter is not public", errors[0].Message);
        }

        public class PropertyPrivateSetter
        {
            [BenchmarkCancellation]
#pragma warning disable BDN1604
            public CancellationToken CancellationToken { get; private set; }
#pragma warning restore BDN1604

            [Benchmark]
            public void Work() { }
        }

        [Fact]
        public async Task NoBenchmarkCancellation_NoErrors()
        {
            var errors = await Validate<NoCancellation>();
            Assert.Empty(errors);
        }

        public class NoCancellation
        {
            [Benchmark]
            public void Work() { }
        }

        [Fact]
        public async Task InheritedBenchmarkCancellation_NoErrors()
        {
            var errors = await Validate<DerivedClass>();
            Assert.Empty(errors);
        }

        public class BaseClassWithCancellation
        {
            [BenchmarkCancellation]
            public CancellationToken CancellationToken { get; set; }
        }

        public class DerivedClass : BaseClassWithCancellation
        {
            [Benchmark]
            public void Work() { }
        }
    }
}
