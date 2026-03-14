using BenchmarkDotNet.Analyzers.Attributes;
using BenchmarkDotNet.Analyzers.Tests.Fixtures;
using System.Threading.Tasks;
using Xunit;

namespace BenchmarkDotNet.Analyzers.Tests.AnalyzerTests.Attributes;

public class BenchmarkCancellationAttributeAnalyzerTests
{
    public class MustBeCancellationTokenType : AnalyzerTestFixture<BenchmarkCancellationAttributeAnalyzer>
    {
        public MustBeCancellationTokenType() : base(BenchmarkCancellationAttributeAnalyzer.MustBeCancellationTokenTypeRule) { }

        [Fact]
        public async Task Field_with_wrong_type_triggers_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using System.Threading;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [BenchmarkCancellation]
                    public string {|BDN1600:MyField|};

                    [Benchmark]
                    public void MyBenchmark() { }
                }
                """;

            TestCode = testCode;
            await RunAsync();
        }

        [Fact]
        public async Task Property_with_wrong_type_triggers_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using System.Threading;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [BenchmarkCancellation]
                    public int {|BDN1600:MyProperty|} { get; set; }

                    [Benchmark]
                    public void MyBenchmark() { }
                }
                """;

            TestCode = testCode;
            await RunAsync();
        }

        [Fact]
        public async Task CancellationToken_type_does_not_trigger_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using System.Threading;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [BenchmarkCancellation]
                    public CancellationToken MyToken { get; set; }

                    [Benchmark]
                    public void MyBenchmark() { }
                }
                """;

            TestCode = testCode;
            await RunAsync();
        }
    }

    public class FieldMustBePublic : AnalyzerTestFixture<BenchmarkCancellationAttributeAnalyzer>
    {
        public FieldMustBePublic() : base(BenchmarkCancellationAttributeAnalyzer.FieldMustBePublicRule) { }

        [Fact]
        public async Task Private_field_triggers_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using System.Threading;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [BenchmarkCancellation]
                    private CancellationToken {|BDN1601:myToken|};

                    [Benchmark]
                    public void MyBenchmark() { }
                }
                """;

            TestCode = testCode;
            await RunAsync();
        }

        [Fact]
        public async Task Public_field_does_not_trigger_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using System.Threading;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [BenchmarkCancellation]
                    public CancellationToken MyToken;

                    [Benchmark]
                    public void MyBenchmark() { }
                }
                """;

            TestCode = testCode;
            await RunAsync();
        }
    }

    public class PropertyMustBePublic : AnalyzerTestFixture<BenchmarkCancellationAttributeAnalyzer>
    {
        public PropertyMustBePublic() : base(BenchmarkCancellationAttributeAnalyzer.PropertyMustBePublicRule) { }

        [Fact]
        public async Task Private_property_triggers_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using System.Threading;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [BenchmarkCancellation]
                    private CancellationToken {|BDN1602:MyToken|} { get; set; }

                    [Benchmark]
                    public void MyBenchmark() { }
                }
                """;

            TestCode = testCode;
            await RunAsync();
        }

        [Fact]
        public async Task Public_property_does_not_trigger_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using System.Threading;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [BenchmarkCancellation]
                    public CancellationToken MyToken { get; set; }

                    [Benchmark]
                    public void MyBenchmark() { }
                }
                """;

            TestCode = testCode;
            await RunAsync();
        }
    }

    public class NotValidOnReadonlyField : AnalyzerTestFixture<BenchmarkCancellationAttributeAnalyzer>
    {
        public NotValidOnReadonlyField() : base(BenchmarkCancellationAttributeAnalyzer.NotValidOnReadonlyFieldRule) { }

        [Fact]
        public async Task Readonly_field_triggers_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using System.Threading;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [BenchmarkCancellation]
                    public {|BDN1603:readonly|} CancellationToken MyToken;

                    [Benchmark]
                    public void MyBenchmark() { }
                }
                """;

            TestCode = testCode;
            await RunAsync();
        }

        [Fact]
        public async Task Non_readonly_field_does_not_trigger_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using System.Threading;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [BenchmarkCancellation]
                    public CancellationToken MyToken;

                    [Benchmark]
                    public void MyBenchmark() { }
                }
                """;

            TestCode = testCode;
            await RunAsync();
        }
    }

    public class PropertyMustHavePublicSetter : AnalyzerTestFixture<BenchmarkCancellationAttributeAnalyzer>
    {
        public PropertyMustHavePublicSetter() : base(BenchmarkCancellationAttributeAnalyzer.PropertyMustHavePublicSetterRule) { }

        [Fact]
        public async Task Property_without_setter_triggers_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using System.Threading;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [BenchmarkCancellation]
                    public CancellationToken {|BDN1604:MyToken|} { get; }

                    [Benchmark]
                    public void MyBenchmark() { }
                }
                """;

            TestCode = testCode;
            await RunAsync();
        }

        [Fact]
        public async Task Property_with_private_setter_triggers_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using System.Threading;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [BenchmarkCancellation]
                    public CancellationToken {|BDN1604:MyToken|} { get; private set; }

                    [Benchmark]
                    public void MyBenchmark() { }
                }
                """;

            TestCode = testCode;
            await RunAsync();
        }

        [Fact]
        public async Task Property_with_public_setter_does_not_trigger_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using System.Threading;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [BenchmarkCancellation]
                    public CancellationToken MyToken { get; set; }

                    [Benchmark]
                    public void MyBenchmark() { }
                }
                """;

            TestCode = testCode;
            await RunAsync();
        }

        [Fact]
        public async Task Property_with_init_setter_does_not_trigger_diagnostic()
        {
            var testCode = /* lang=c#-test */ """
                using System.Threading;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [BenchmarkCancellation]
                    public CancellationToken MyToken { get; init; }

                    [Benchmark]
                    public void MyBenchmark() { }
                }
                """;

            DisableCompilerDiagnostics();
            TestCode = testCode;
            await RunAsync();
        }
    }

    public class StaticMembers : AnalyzerTestFixture<BenchmarkCancellationAttributeAnalyzer>
    {
        [Fact]
        public async Task Static_field_is_allowed()
        {
            var testCode = /* lang=c#-test */ """
                using System.Threading;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [BenchmarkCancellation]
                    public static CancellationToken MyToken;

                    [Benchmark]
                    public void MyBenchmark() { }
                }
                """;

            TestCode = testCode;
            await RunAsync();
        }

        [Fact]
        public async Task Static_property_is_allowed()
        {
            var testCode = /* lang=c#-test */ """
                using System.Threading;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [BenchmarkCancellation]
                    public static CancellationToken MyToken { get; set; }

                    [Benchmark]
                    public void MyBenchmark() { }
                }
                """;

            TestCode = testCode;
            await RunAsync();
        }
    }
}
