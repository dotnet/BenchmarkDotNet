namespace BenchmarkDotNet.Tests
{
    public class KnownIssue
    {
        public static KnownIssue Issue2299 => new (2299, "Non-supported Mono on Linux", false);

        public int Number { get; }
        public string Description { get; }
        public bool IsFixed { get; }

        public KnownIssue(int number, string description, bool isFixed)
        {
            Number = number;
            Description = description;
            IsFixed = isFixed;
        }

        public string IgnoreMessage => $"This test is ignored because of the issue #{Number} '{Description}'";
    }
}