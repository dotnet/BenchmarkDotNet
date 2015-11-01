using System.Linq;

namespace BenchmarkDotNet.Tasks
{
    public class BenchmarkParameters
    {
        public int IntParam { get; }

        public BenchmarkParameters(int intParam)
        {
            IntParam = intParam;
        }

        public string ToArgs()
        {
            return "-p=" + IntParam;
        }

        public static BenchmarkParameters ParseArgs(string[] args)
        {
            int intValue = 0;
            foreach (var arg in args.Where(arg => arg.StartsWith("-p=")))
                int.TryParse(arg.Substring(3), out intValue);
            return new BenchmarkParameters(intValue);
        }

        public string ToInfo()
        {
            return $"IntParam={IntParam}";
        }
    }
}