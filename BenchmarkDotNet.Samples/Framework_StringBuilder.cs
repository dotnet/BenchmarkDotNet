using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Samples
{
    [Task(framework: BenchmarkFramework.V35)]
    [Task(framework: BenchmarkFramework.V40)]
    public class Framework_StringBuilder
    {
        private const int Length = 100001;

        [Benchmark]
        public string Insert()
        {
            var builder = new StringBuilder();
            for (int i = 0; i < Length; i++)
                builder.Insert(0, 'a');
            return builder.ToString();
        }
    }
}