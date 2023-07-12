namespace BenchmarkDotNet.Build.Options;

public class StringOption : Option<string>
{
    public StringOption(string commandLineName) : base(commandLineName)
    {
    }


    public override string Resolve(BuildContext context)
    {
        if (!HasArgument(context))
            return "";
        var value = GetArgument(context);
        if (value == null || string.IsNullOrWhiteSpace(value))
            return "";
        return value.Trim();
    }
}