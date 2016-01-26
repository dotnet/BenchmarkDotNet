namespace BenchmarkDotNet.Jobs
{
    public class Count
    {
        public static readonly Count Auto = new Count(-1);

        public int Value { get; }
        public bool IsAuto => Value < 0;

        public Count(int value)
        {
            Value = value;
        }

        public static implicit operator int(Count count) => count.Value;
        public static implicit operator Count(int value) => new Count(value);

        public override string ToString() => IsAuto ? "Auto" : Value.ToString();
    }
}