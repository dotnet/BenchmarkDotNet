using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Tests.Jobs;

public class MsBuildArgumentTests
{
    [Fact]
    public void EscapeSpecialCharactersIsOptIn()
    {
        var argument = new MsBuildArgument("/p:DefineConstants=TEST1;TEST2");

        Assert.Equal("/p:DefineConstants=TEST1;TEST2", argument.TextRepresentation);
        Assert.Equal("/p:DefineConstants=TEST1;TEST2", argument.ToString());
    }

    [Fact]
    public void EscapeSpecialCharactersEscapesAllMsBuildSpecialCharactersForCommandLine()
    {
        var argument = new MsBuildArgument("%$@();?*", escapeSpecialCharacters: true);

        Assert.Equal("%25%24%40%28%29%3B%3F%2A", argument.TextRepresentation);
        Assert.Equal("%$@();?*", argument.ToString());
    }

    [Fact]
    public void MsBuildPropertyJoinsValuesWithSemicolonsAndEscapesThem()
    {
        var argument = new MsBuildProperty("DefineConstants", "TEST1", "TEST2");

        Assert.Equal("/p:DefineConstants=TEST1%3BTEST2", argument.TextRepresentation);
        Assert.Equal("/p:DefineConstants=TEST1;TEST2", argument.ToString());
    }
}

