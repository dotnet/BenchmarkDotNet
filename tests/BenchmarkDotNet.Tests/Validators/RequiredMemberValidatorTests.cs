using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using System.Diagnostics.CodeAnalysis;

namespace BenchmarkDotNet.Tests.Validators;

public class RequiredMemberValidatorTests
{
    private static async ValueTask<string[]> Validate<T>()
    {
        var benchmarks = BenchmarkConverter.TypeToBenchmarks(typeof(T));
        // Without a case to validate, every "is not reported" assertion below would hold for the wrong reason.
        Assert.NotEmpty(benchmarks.BenchmarksCases);

        var errors = await RequiredMemberValidator.FailOnError.ValidateAsync(benchmarks).ToArrayAsync();
        return errors.Select(error => error.Message).ToArray();
    }

    [Fact]
    public async Task RequiredMemberBenchmarkDotNetCannotSetIsReported()
    {
        var messages = await Validate<RequiredMemberWithoutAttribute>();

        Assert.Contains(messages, message => message.Contains(nameof(RequiredMemberWithoutAttribute.Text)) && message.Contains("required member"));
    }

    [Fact]
    public async Task RequiredParamsMemberIsNotReported()
    {
        var messages = await Validate<RequiredParamsMember>();

        Assert.DoesNotContain(messages, message => message.Contains("required member"));
    }

    [Fact]
    public async Task SetsRequiredMembersConstructorIsReported()
    {
        var messages = await Validate<WithSetsRequiredMembersCtor>();

        Assert.Contains(messages, message => message.Contains("[SetsRequiredMembers]"));
    }

    [Fact]
    public async Task PlainBenchmarkIsNotReported()
    {
        var messages = await Validate<RequiredParamsMember>();

        Assert.DoesNotContain(messages, message => message.Contains("[SetsRequiredMembers]"));
    }

    [Fact]
    public async Task NestedTypeDeclaringRequiredMembersIsNotReported()
    {
        // The compiler stamps [RequiredMember] on any type declaring required members, so a nested type must
        // not be mistaken for a required member of the benchmark type.
        var messages = await Validate<WithNestedTypeWithRequiredMember>();

        Assert.Empty(messages);
    }

    public class WithNestedTypeWithRequiredMember
    {
        public class Nested
        {
            public required int Value { get; set; }
        }

        [Benchmark]
        public void Foo() { }
    }

#pragma warning disable BDN1109
    public class RequiredMemberWithoutAttribute
    {
        public required string Text { get; set; }

        [Benchmark]
        public void Foo() { }
    }
#pragma warning restore BDN1109

    public class RequiredParamsMember
    {
        [Params(1)]
        public required int Value { get; set; }

        [Benchmark]
        public void Foo() { }
    }

#pragma warning disable BDN1110
    public class WithSetsRequiredMembersCtor
    {
        [Params(1)]
        public required int Value { get; set; }

        [SetsRequiredMembers]
        public WithSetsRequiredMembersCtor() => Value = 1;

        [Benchmark]
        public void Foo() { }
    }
#pragma warning restore BDN1110
}
