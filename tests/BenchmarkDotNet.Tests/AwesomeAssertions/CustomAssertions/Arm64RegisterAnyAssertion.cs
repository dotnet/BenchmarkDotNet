namespace BenchmarkDotNet.Tests;

using AsmArm64;
using AwesomeAssertions;
using AwesomeAssertions.Execution;

/// <summary>
/// Custom assertions for Arm64RegisterAny.
/// </summary>
[CustomAssertions]
public sealed class Arm64RegisterAnyAssertions
{
    private readonly Arm64RegisterAny subject;
    private readonly AssertionChain assertionChain;

    public Arm64RegisterAnyAssertions(
        Arm64RegisterAny subject,
        AssertionChain assertionChain)
    {
        this.subject = subject;
        this.assertionChain = assertionChain;
    }

    [CustomAssertion]
    public AndConstraint<Arm64RegisterAnyAssertions> Be(
        Arm64RegisterAny expected,
        string because = "",
        params object[] becauseArgs)
    {
        assertionChain
            .BecauseOf(because, becauseArgs)
            .ForCondition(EqualityComparer<Arm64RegisterAny>.Default.Equals(subject, expected))
            .FailWith("Expected register to be {0}, but found {1}.", expected.ToText(), subject.ToText());

        return new AndConstraint<Arm64RegisterAnyAssertions>(this);
    }

    //[CustomAssertion]
    //public AndConstraint<Arm64RegisterAnyAssertions> NotBe(
    //    Arm64RegisterAny unexpected,
    //    string because = "",
    //    params object[] becauseArgs)
    //{
    //    assertionChain
    //        .BecauseOf(because, becauseArgs)
    //        .ForCondition(!EqualityComparer<Arm64RegisterAny>.Default.Equals(
    //            subject, unexpected))
    //        .FailWith(
    //            "Did not expect {context:Arm64 register} to be {0}{reason}.",
    //            unexpected);

    //    return new AndConstraint<Arm64RegisterAnyAssertions>(this);
    //}
}

public static class Arm64RegisterAnyAssertionsExtensions
{
    public static Arm64RegisterAnyAssertions Should(this Arm64RegisterAny subject)
    {
        return new Arm64RegisterAnyAssertions(subject, AssertionChain.GetOrCreate());
    }
}