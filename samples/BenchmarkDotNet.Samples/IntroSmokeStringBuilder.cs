using BenchmarkDotNet.Attributes;
using System.Text;

namespace BenchmarkDotNet.Samples
{
    [MemoryDiagnoser(false)]
    public class IntroSmokeStringBuilder
    {
        [Benchmark]
        [Arguments(1)]
        [Arguments(1_000)]
        public StringBuilder Append_Strings(int repeat)
        {
            StringBuilder builder = new StringBuilder();

            // strings are not sorted by length to mimic real input
            for (int i = 0; i < repeat; i++)
            {
                builder.Append("12345");
                builder.Append("1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMN");
                builder.Append("1234567890abcdefghijklmnopqrstuvwxy");
                builder.Append("1234567890");
                builder.Append("1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHI");
                builder.Append("1234567890abcde");
                builder.Append("1234567890abcdefghijklmnopqrstuvwxyzABCD");
                builder.Append("1234567890abcdefghijklmnopqrst");
                builder.Append("1234567890abcdefghij");
                builder.Append("1234567890abcdefghijklmno");
            }

            return builder;
        }
    }
}