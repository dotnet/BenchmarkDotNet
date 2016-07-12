using System.Text;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples.Framework
{
    public class Framework_StringConcatVsStringBuilder
    {
        [Params(1, 2, 3, 4, 5, 10, 100, 1000)]
        public int Loops;

        [Benchmark]
        public string StringConcat()
        {
            string result = string.Empty;
            for (int i = 0; i < Loops; ++i)
                result = string.Concat(result, i.ToString());
            return result;
        }

        [Benchmark]
        public string StringBuilder()
        {
            StringBuilder sb = new StringBuilder(string.Empty);
            for (int i = 0; i < Loops; ++i)
                sb.Append(i.ToString());
            return sb.ToString();
        }
    }
}
